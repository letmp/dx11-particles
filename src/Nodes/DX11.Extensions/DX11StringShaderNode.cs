using System;
using System.IO;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FeralTic.DX11;
using FeralTic.DX11.Queries;
using FeralTic.DX11.Resources;

using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D11.Device;

using VVVV.DX11;
using VVVV.DX11.Lib.Effects;
using VVVV.DX11.Lib.Rendering;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Core.Logging;

namespace VVVV.DX11.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Shader", Category = "DX11.Effect", Version="String", Help = "Build a shader with a string input", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public unsafe class DX11StringShaderNode : IPluginEvaluate, IDX11LayerProvider, IPartImportsSatisfiedNotification, IDisposable
    {

        #region fields & pins
        [Input("Layer In")]
        protected Pin<DX11Resource<DX11Layer>> FInLayer;

        [Input("Render State", CheckIfChanged = true)]
        protected Pin<DX11RenderState> FInState;

        [Input("Iterastion Count", Order = 10000, DefaultValue = 1, MinValue = 1, Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<int> FIter;

        [Input("Geometry", CheckIfChanged = true)]
        protected Pin<DX11Resource<IDX11Geometry>> FGeometry;

        [Input("ShaderCode", DefaultString = "", IsSingle=true)]
        public IDiffSpread<string> FShaderCode;

        [Input("IncludePath", StringType = StringType.Filename, DefaultString = "", FileMask = "", IsSingle = true)]
        public IDiffSpread<string> FFileName;

        [Input("Defines", Order = 20000, Visibility = PinVisibility.OnlyInspector)]
        protected IDiffSpread<string> FInDefines;

        [Input("Enabled", Order = 10000, DefaultValue = 1)]
        protected ISpread<bool> FInEnabled;

        [Input("Apply Only", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<bool> FInApplyOnly;

        [Output("Layer")]
        protected ISpread<DX11Resource<DX11Layer>> FOutLayer;

        [Output("Layout Valid")]
        protected ISpread<bool> FOutLayoutValid;

        [Output("Layout Message")]
        protected ISpread<string> FOutLayoutMsg;

        [Output("Technique Valid")]
        protected ISpread<bool> FOutTechniqueValid;

        [Output("Compiled")]
        protected ISpread<bool> FOutCompiled;

        /*[Output("Shader Signature", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11Resource<DX11Shader>> FOutShader;*/

        [Output("Custom Semantics", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<string> FoutCS;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        protected bool FInvalidate;

        protected IPluginHost FHost;
        protected IIOFactory FFactory;

        protected ITransformIn FInWorld;
        protected Matrix* mworld;
        protected int mworldcount;

        private DX11ObjectRenderSettings objectsettings = new DX11ObjectRenderSettings();
        private DX11ShaderVariableManager varmanager;
        private Dictionary<DX11RenderContext, DX11ShaderData> deviceshaderdata = new Dictionary<DX11RenderContext, DX11ShaderData>();
        private DX11Effect FShader;
        private bool init = true;
        private bool shaderupdated;
        private int spmax = 0;
        private bool geomconnected;
        private bool stateconnected;

        private List<DX11ObjectRenderSettings> orderedObjectSettings = new List<DX11ObjectRenderSettings>();

        public event DX11QueryableDelegate BeginQuery;
        public event DX11QueryableDelegate EndQuery;

        protected IDiffSpread<EnumEntry> FInTechnique;
        
        protected string TechniqueEnumId;

        private int techniqueindex;
        protected bool techniquechanged;

        protected void OnBeginQuery(DX11RenderContext context)
        {
            if (this.BeginQuery != null)
            {
                this.BeginQuery(context);
            }
        }

        protected void OnEndQuery(DX11RenderContext context)
        {
            if (this.EndQuery != null)
            {
                this.EndQuery(context);
            }
        }

        #region Set the shader instance

        public void SetShader(DX11Effect shader, bool isnew)
        {
            this.FShader = shader;

            this.varmanager.SetShader(shader);
            
            //Only set technique if new, otherwise do it on update/evaluate
            if (isnew)
            {
                string defaultenum;
                if (shader.IsCompiled)
                {
                    defaultenum = shader.TechniqueNames[0];
                    this.FHost.UpdateEnum(this.TechniqueEnumId, shader.TechniqueNames[0], shader.TechniqueNames);
                    this.varmanager.CreateShaderPins();
                }
                else
                {
                    defaultenum = "";
                    this.FHost.UpdateEnum(this.TechniqueEnumId, "", new string[0]);
                }

                if (shader.TechniqueNames != null)
                {
                    this.techniqueindex = Array.IndexOf(shader.TechniqueNames, FInTechnique[0].Name);
                    this.techniquechanged = true;
                }
                
                //this.FoutCS.AssignFrom(this.varmanager.GetCustomData());
            }
            else
            {
                if (shader.IsCompiled)
                {
                    this.FHost.UpdateEnum(this.TechniqueEnumId, shader.TechniqueNames[0], shader.TechniqueNames);
                    this.varmanager.UpdateShaderPins();
                    this.FoutCS.AssignFrom(this.varmanager.GetCustomData());
                }
            }

            this.shaderupdated = true;
            this.FInvalidate = true;
        }
        #endregion

        #region Constructor
        [ImportingConstructor()]
        public DX11StringShaderNode(IPluginHost host, IIOFactory factory)
        {
            this.FHost = host;
            this.FFactory = factory;
            this.TechniqueEnumId = Guid.NewGuid().ToString();

            this.varmanager = new DX11ShaderVariableManager(host, factory);

            this.FHost.CreateTransformInput("Transform In", TSliceMode.Dynamic, TPinVisibility.True, out this.FInWorld);
        }
        #endregion

        #region Evaluate
        public void Evaluate(int SpreadMax)
        {

            this.shaderupdated = false;
            this.spmax = this.CalculateSpreadMax();

            if (FShaderCode.IsChanged || FFileName.IsChanged || FInDefines.IsChanged)
            {
                List<ShaderMacro> sms = new List<ShaderMacro>();
                for (int i = 0; i < this.FInDefines.SliceCount; i++)
                {
                    try
                    {
                        string[] s = this.FInDefines[i].Split("=".ToCharArray());

                        if (s.Length == 2)
                        {
                            ShaderMacro sm = new ShaderMacro();
                            sm.Name = s[0];
                            sm.Value = s[1];
                            sms.Add(sm);
                        }

                    }
                    catch
                    {

                    }
                }

                DX11ShaderInclude FIncludeHandler = new DX11ShaderInclude();
                FIncludeHandler.ParentPath = Path.GetDirectoryName(FFileName[0]);

                FShader = DX11Effect.FromString(FShaderCode[0], FIncludeHandler, sms.ToArray());
                if (init)
                {
                    this.SetShader(FShader, true);
                    init = false;
                }
                else this.SetShader(FShader, false);
                
            }
            

            if (FShader.TechniqueNames != null && this.FInTechnique.IsChanged)
            {
                this.techniqueindex = Array.IndexOf(FShader.TechniqueNames, FInTechnique[0].Name);
                this.techniquechanged = true;
            }

            float* src;

            //Cache world pointer
            this.FInWorld.GetMatrixPointer(out this.mworldcount, out src);
            this.mworld = (Matrix*)src;

            this.FOutLayer.SliceCount = 1;
            if (this.FOutLayer[0] == null)
            {
                this.FOutLayer[0] = new DX11Resource<DX11Layer>();
            }

            if (this.FInvalidate)
            {
                if (this.FShader.IsCompiled)
                {
                    this.FOutCompiled[0] = true;
                    this.FOutTechniqueValid.SliceCount = this.FShader.TechniqueValids.Length;

                    for (int i = 0; i < this.FShader.TechniqueValids.Length; i++)
                    {
                        this.FOutTechniqueValid[i] = this.FShader.TechniqueValids[i];
                    }
                }
                else
                {
                    this.FOutCompiled[0] = false;
                    this.FOutTechniqueValid.SliceCount = 0;
                }
                this.FInvalidate = false;
            }

        }
        #endregion

        #region Calculate Spread Max
        private int CalculateSpreadMax()
        {
            int max = this.varmanager.CalculateSpreadMax();

            if (max == 0 || this.FInWorld.SliceCount == 0 || this.FGeometry.SliceCount == 0)
            {
                return 0;
            }
            else
            {
                max = Math.Max(this.FInWorld.SliceCount, max);
                max = Math.Max(this.FGeometry.SliceCount, max);
                return max;
            }
        }
        #endregion

        #region Update
        public void Update(IPluginIO pin, DX11RenderContext context)
        {
            if (!this.FOutLayer[0].Data.ContainsKey(context))
            {
                this.FOutLayer[0][context] = new DX11Layer();
                this.FOutLayer[0][context].Render = this.Render;
            }

            if (!this.deviceshaderdata.ContainsKey(context))
            {
                this.deviceshaderdata.Add(context, new DX11ShaderData(context));
            }
            //Update shader
            this.deviceshaderdata[context].SetEffect(this.FShader);

            DX11ShaderData shaderdata = this.deviceshaderdata[context];
            if (this.shaderupdated)
            {
                shaderdata.SetEffect(this.FShader);
                //this.FOutShader[0][context] = new DX11Shader(shaderdata.ShaderInstance);
            }
            /*else
            {
                if (!this.FOutShader[0].Contains(context))
                {
                    this.FOutShader[0][context] = new DX11Shader(shaderdata.ShaderInstance);
                }
            }*/
        }
        #endregion

        #region Destroy
        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
            this.FOutLayer[0].Dispose(context);

            if (this.deviceshaderdata.ContainsKey(context))
            {
                this.deviceshaderdata[context].Dispose();
                this.deviceshaderdata.Remove(context);
            }
        }
        #endregion

        #region Render
        public void Render(IPluginIO pin, DX11RenderContext context, DX11RenderSettings settings)
        {
            Device device = context.Device;
            DeviceContext ctx = context.CurrentDeviceContext;

            bool popstate = false;

            bool multistate = this.FInState.IsConnected && this.FInState.SliceCount > 1;

            if (this.FInEnabled[0])
            {
               
                DX11ShaderData shaderdata = this.deviceshaderdata[context];
                if ((shaderdata.IsValid &&
                    (this.geomconnected || settings.Geometry != null)
                    && this.spmax > 0 && this.varmanager.SetGlobalSettings(shaderdata.ShaderInstance, settings))
                    || this.FInApplyOnly[0])
                {
                    this.OnBeginQuery(context);

                    //Select preferred technique if available
                    if (settings.PreferredTechniques.Count == 0 && this.techniqueindex != Array.IndexOf(FShader.TechniqueNames, FInTechnique[0].Name))
                    {
                        this.techniqueindex = Array.IndexOf(FShader.TechniqueNames, FInTechnique[0].Name);
                        this.techniquechanged = true;
                    }
                    else if (settings.PreferredTechniques.Count > 0)
                    {
                        int i = settings.GetPreferredTechnique(this.FShader);
                        if (i == -1)
                        {
                            i = 0;
                        }
                        if (i != this.techniqueindex)
                        {
                            this.techniqueindex = i;
                            this.techniquechanged = true;
                        }
                    }

                    //Need to build input layout
                    if (this.FGeometry.IsChanged || this.techniquechanged || shaderdata.LayoutValid.Count == 0)
                    {
                        shaderdata.Update(this.techniqueindex, 0, this.FGeometry);
                        this.FOutLayoutValid.AssignFrom(shaderdata.LayoutValid);
                        this.FOutLayoutMsg.AssignFrom(shaderdata.LayoutMsg);

                        int errorCount = 0;
                        StringBuilder sbMsg = new StringBuilder();
                        sbMsg.Append("Invalid layout detected for slices:");
                        for (int i = 0; i < shaderdata.LayoutValid.Count; i++)
                        {
                            if (shaderdata.LayoutValid[i] == false)
                            {
                                errorCount++;
                                sbMsg.Append(i + ",");
                            }
                        }

                        /*if (errorCount > 0)
                        {
                            this.FHost.Log(TLogType.Warning, sbMsg.ToString());
                        }*/

                        this.techniquechanged = false;
                    }

                    if (this.stateconnected && !multistate)
                    {
                        context.RenderStateStack.Push(this.FInState[0]);
                        popstate = true;
                    }

                    ShaderPipelineState pipelineState = null;
                    if (!settings.PreserveShaderStages)
                    {
                        shaderdata.ResetShaderStages(ctx);
                    }
                    else
                    {
                        pipelineState = new ShaderPipelineState(context);
                    }

                    settings.DrawCallCount = spmax; //Set number of draw calls

                    this.varmanager.ApplyGlobal(shaderdata.ShaderInstance);

                    //IDX11Geometry drawgeom = null;
                    objectsettings.Geometry = null;
                    DX11Resource<IDX11Geometry> pg = null;
                    bool doOrder = false;
                    List<int> orderedSlices = null;
                    if (settings.LayerOrder != null && settings.LayerOrder.Enabled)
                    {
                        this.orderedObjectSettings.Clear();
                        for (int i = 0; i < this.spmax; i++)
                        {
                            DX11ObjectRenderSettings objSettings = new DX11ObjectRenderSettings();
                            objSettings.DrawCallIndex = i;
                            objSettings.Geometry = null;
                            objSettings.IterationCount = 1;
                            objSettings.IterationIndex = 0;
                            objSettings.WorldTransform = this.mworld[i % this.mworldcount];

                            this.orderedObjectSettings.Add(objSettings);
                        }

                        orderedSlices = settings.LayerOrder.Reorder(settings, orderedObjectSettings);
                        doOrder = true;
                    }

                    int drawCount = doOrder ? orderedSlices.Count : this.spmax;

                    if (this.spmax == 0)
                    {
                        drawCount = 0;
                    }

                    for (int i = 0; i < drawCount; i++)
                    {
                        int idx = doOrder ? orderedSlices[i] : i;
                        if (multistate)
                        {
                            context.RenderStateStack.Push(this.FInState[idx]);
                        }

                        if (shaderdata.IsLayoutValid(idx) || settings.Geometry != null)
                        {
                            objectsettings.IterationCount = this.FIter[idx];

                            for (int k = 0; k < objectsettings.IterationCount; k++)
                            {
                                objectsettings.IterationIndex = k;
                                if (settings.Geometry == null)
                                {
                                    if (this.FGeometry[idx] != pg)
                                    {
                                        pg = this.FGeometry[idx];

                                        objectsettings.Geometry = pg[context];

                                        shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, idx);
                                    }
                                }
                                else
                                {
                                    objectsettings.Geometry = settings.Geometry;
                                    shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, idx);
                                }

                                //Prepare settings
                                objectsettings.DrawCallIndex = idx;
                                objectsettings.WorldTransform = this.mworld[idx % this.mworldcount];

                                if (settings.ValidateObject(objectsettings))
                                {
                                    this.varmanager.ApplyPerObject(context, shaderdata.ShaderInstance, this.objectsettings, idx);

                                    shaderdata.ApplyPass(ctx);

                                    if (settings.DepthOnly) { ctx.PixelShader.Set(null); }

                                    if (settings.PostPassAction != null) { settings.PostPassAction(context); }


                                    objectsettings.Geometry.Draw();
                                    shaderdata.ShaderInstance.CleanUp();
                                }
                            }
                        }

                        if (multistate)
                        {
                            context.RenderStateStack.Pop();
                        }

                        if (settings.PostShaderAction != null)
                        {
                            settings.PostShaderAction(context);
                        }
                    }

                    if (pipelineState != null)
                    {
                        pipelineState.Restore(context);
                    }


                    this.OnEndQuery(context);
                }
                //this.query.End();
            }

            if (popstate)
            {
                context.RenderStateStack.Pop();
            }
            else
            {
                //Since shaders can define their own states, reapply top of the stack
                context.RenderStateStack.Apply();
            }

            if (this.FInLayer.IsConnected && this.FInEnabled[0])
            {
                this.FInLayer[0][context].Render(this.FInLayer.PluginIO, context, settings);
            }

        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            foreach (DX11ShaderData sd in this.deviceshaderdata.Values)
            {
                sd.Dispose();
            }
        }
        #endregion

        public void OnImportsSatisfied()
        {
            this.FGeometry.Connected += new PinConnectionEventHandler(FGeometry_Connected);
            this.FGeometry.Disconnected += new PinConnectionEventHandler(FGeometry_Disconnected);
            this.FInState.Connected += new PinConnectionEventHandler(FInState_Connected);
            this.FInState.Disconnected += new PinConnectionEventHandler(FInState_Disconnected);

            //string[] entries = FConfig[0].Split(":".ToCharArray());
            this.FHost.UpdateEnum(this.TechniqueEnumId, "", new string[0]);
            //Create Technique enum pin
            InputAttribute inAttr = new InputAttribute("Technique");
            inAttr.EnumName = this.TechniqueEnumId;
            inAttr.DefaultEnumEntry = "";
            inAttr.Order = 1000;
            this.FInTechnique = this.FFactory.CreateDiffSpread<EnumEntry>(inAttr);

        }

        void FInState_Disconnected(object sender, PinConnectionEventArgs args)
        {
            this.stateconnected = false;

        }

        void FInState_Connected(object sender, PinConnectionEventArgs args)
        {
            this.stateconnected = true;
        }

        void FGeometry_Disconnected(object sender, PinConnectionEventArgs args)
        {
            this.geomconnected = false;
        }


        void FGeometry_Connected(object sender, PinConnectionEventArgs args)
        {
            this.geomconnected = true;
        }
    }
}
