using System;
using System.IO;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Text;

using FeralTic.DX11;
using FeralTic.DX11.Queries;
using FeralTic.DX11.Resources;

using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D11.Device;

using VVVV.DX11.Lib.Effects;
using VVVV.DX11.Lib.Rendering;
using VVVV.DX11.Internals.Effects;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Core.Logging;


namespace VVVV.DX11.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Shader", Category = "DX11.Effect", Version="String", Help = "Build a shader with a string input", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public unsafe class DX11StringShaderNode : IPluginEvaluate, IDX11LayerHost, IPartImportsSatisfiedNotification, IDisposable
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

        [Output("Custom Semantics", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<string> FoutCS;

        [Output("Shader Signature", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11Resource<DX11Shader>> FOutShader;

        [Output("Current Effect", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11Effect> currentEffectOut;

        /*[Output("Shader Signature", Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11Resource<DX11Shader>> FOutShader;*/
        
        [Config("ConfigShader", DefaultString = "")]
        public IDiffSpread<string> FConfigShader;
        [Config("ConfigDefines", DefaultString = "")]
        public IDiffSpread<string> FConfigDefines;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        protected bool FInvalidate;

        protected IPluginHost FHost;
        protected IIOFactory FFactory;

        protected ITransformIn FInWorld;
        protected Matrix* mworld;
        protected int mworldcount;

        //private DX11ObjectRenderSettings objectsettings = new DX11ObjectRenderSettings();
        private DX11ContextElement<DX11ObjectRenderSettings> objectSettings = new DX11ContextElement<DX11ObjectRenderSettings>();
        private DX11ContextElement<List<DX11ObjectRenderSettings>> orderedObjectSettings = new DX11ContextElement<List<DX11ObjectRenderSettings>>();
        private DX11ContextElement<DX11ShaderVariableCache> shaderVariableCache = new DX11ContextElement<DX11ShaderVariableCache>();

        private DX11ShaderVariableManager varmanager;
        private DX11Resource<DX11ShaderData> deviceshaderdata = new DX11Resource<DX11ShaderData>();
        private DX11Effect FShader;

        private bool init = true;
        private bool shaderupdated;
        private bool configWritten = false;

        private int spmax = 0;
        private bool geomconnected;
        private bool stateconnected;
        
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
            if (shader.IsCompiled)
            {
                this.FShader = shader;
                this.varmanager.SetShader(shader);
                this.shaderVariableCache.Clear();
                this.deviceshaderdata.Dispose();
                this.currentEffectOut[0] = shader;
            }
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

                //Create Technique enum pin
                /*InputAttribute inAttr = new InputAttribute("Technique");
                inAttr.EnumName = this.TechniqueEnumId;
                inAttr.DefaultEnumEntry = defaultenum;
                inAttr.Order = 1000;
                this.FInTechnique = this.FFactory.CreateDiffSpread<EnumEntry>(inAttr);

                this.FoutCS.AssignFrom(this.varmanager.GetCustomData());*/

                if (shader.TechniqueNames != null)
                {
                    this.techniqueindex = Array.IndexOf(shader.TechniqueNames, FInTechnique[0].Name);
                    this.techniquechanged = true;
                }
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

        
        public bool HasDynamicPins(DX11Effect shader)
        {
            
            for (int i = 0; i < shader.DefaultEffect.Description.GlobalVariableCount; i++)
            {
                EffectVariable var = shader.DefaultEffect.GetVariableByIndex(i);
                if (ShaderPinFactory.IsShaderPin(var)) return true;
            }

            return false;
        }


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
                if (init && !ShaderCreatedByConfig)
                {
                    this.SetShader(FShader, true);
                    init = false;
                }
                else this.SetShader(FShader, false);

                // Write Shadercode & Defines into config -> needed to restore dynamic pins
                if (HasDynamicPins(FShader))
                {
                    configWritten = true;
                    if (FConfigShader[0] != FShaderCode[0])
                    {
                        FConfigShader[0] = FShaderCode[0];
                        FConfigDefines[0] = "";

                        for (int i = 0; i < FInDefines.SliceCount; i++)
                        {
                            if (i != 0) FConfigDefines[0] += ",";
                            FConfigDefines[0] += this.FInDefines[i];
                        }
                    }
                }
                else
                {
                    if (FConfigShader[0] != "")
                    {
                        FConfigShader[0] = "";
                        FConfigDefines[0] = "";
                    }
                }
            }


            if (FShader.TechniqueNames != null && this.FInTechnique.IsChanged && FInTechnique.SliceCount > 0)
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
        public void Update(DX11RenderContext context)
        {
            if (!this.FOutLayer[0].Contains(context))
            {
                this.FOutLayer[0][context] = new DX11Layer();
                this.FOutLayer[0][context].Render = this.Render;
            }

            if (!this.objectSettings.Contains(context))
            {
                this.objectSettings[context] = new DX11ObjectRenderSettings();
            }
            if (!this.orderedObjectSettings.Contains(context))
            {
                this.orderedObjectSettings[context] = new List<DX11ObjectRenderSettings>();
            }


            if (!this.deviceshaderdata.Contains(context))
            {
                this.deviceshaderdata[context] = new DX11ShaderData(context, this.FShader);
            }
            if (!this.shaderVariableCache.Contains(context))
            {
                this.shaderVariableCache[context] = new DX11ShaderVariableCache(context, this.deviceshaderdata[context].ShaderInstance, this.varmanager);
            }

            //DX11ShaderData shaderdata = this.deviceshaderdata[context];
            if (this.shaderupdated)
            {
                this.deviceshaderdata[context] = new DX11ShaderData(context, this.FShader);
                //this.FOutShader[0][context] = new DX11Shader(shaderdata.ShaderInstance);
            }
            /*if (!this.FOutShader[0].Contains(context))
            {
                this.FOutShader[0][context] = new DX11Shader(shaderdata.ShaderInstance);
            }*/
        }
        #endregion

        #region Destroy
        public void Destroy( DX11RenderContext context, bool force)
        {
            if (force)
            {
                this.FOutLayer.SafeDisposeAll(context);
                this.deviceshaderdata.Dispose(context);
                this.shaderVariableCache.Dispose(context);
                this.objectSettings.Dispose(context);
                this.orderedObjectSettings.Dispose(context);
            }
        }
        #endregion

        #region Collect
        private void Collect(DX11RenderContext context, DX11RenderSettings settings)
        {
            if (settings.RenderHint == eRenderHint.Collector)
            {
                if (this.FGeometry.IsConnected)
                {
                    DX11ObjectGroup group = new DX11ObjectGroup();
                    //group.ShaderName = this.Source.Name;
                    group.ShaderName = "DynamicStringShader";
                    group.Semantics.AddRange(settings.CustomSemantics);

                    if (this.FGeometry.SliceCount == 1)
                    {
                        IDX11Geometry g = this.FGeometry[0][context];
                        if (g.Tag != null)
                        {
                            DX11RenderObject o = new DX11RenderObject();
                            o.ObjectType = g.PrimitiveType;
                            o.Descriptor = g.Tag;
                            o.Transforms = new Matrix[spmax];
                            for (int i = 0; i < this.spmax; i++)
                            {
                                o.Transforms[i] = this.mworld[i % this.mworldcount];
                            }
                            group.RenderObjects.Add(o);

                            settings.SceneDescriptor.Groups.Add(group);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < this.spmax; i++)
                        {
                            IDX11Geometry g = this.FGeometry[i][context];
                            if (g.Tag != null)
                            {
                                DX11RenderObject o = new DX11RenderObject();
                                o.ObjectType = g.PrimitiveType;
                                o.Descriptor = g.Tag;
                                o.Transforms = new Matrix[1];
                                o.Transforms[0] = this.mworld[i % this.mworldcount];
                                group.RenderObjects.Add(o);
                            }
                        }

                        settings.SceneDescriptor.Groups.Add(group);

                    }

                }
                return;
            }
        }
        #endregion

        private void ApplyOnly(DX11RenderContext context, DX11RenderSettings settings)
        {
            Device device = context.Device;
            DeviceContext ctx = context.CurrentDeviceContext;

            var variableCache = this.shaderVariableCache[context];
            DX11ShaderData sdata = this.deviceshaderdata[context];
            this.varmanager.SetGlobalSettings(sdata.ShaderInstance, settings);
            variableCache.ApplyGlobals(settings);

            DX11ObjectRenderSettings oset = new DX11ObjectRenderSettings();
            oset.DrawCallIndex = 0;
            oset.Geometry = null;
            oset.IterationCount = 1;
            oset.IterationIndex = 0;
            oset.WorldTransform = this.mworld[0 % this.mworldcount];
            variableCache.ApplySlice(oset, 0);
            sdata.ApplyPass(ctx);

            this.FInLayer.RenderAll(context, settings);

        }

        #region Render
        public void Render( DX11RenderContext context, DX11RenderSettings settings)
        {
            Device device = context.Device;
            DeviceContext ctx = context.CurrentDeviceContext;

            bool popstate = false;

            bool multistate = this.FInState.IsConnected && this.FInState.SliceCount > 1;
            bool stateConnected = this.FInState.IsConnected;

            if (this.FInEnabled[0])
            {
                //In that case we do not care about geometry, but only apply pass for globals
                if (settings.RenderHint == eRenderHint.ApplyOnly)
                {
                    this.ApplyOnly(context, settings);
                    return;
                }

                if (settings.RenderHint == eRenderHint.Collector)
                {
                    this.Collect(context, settings);
                    return;
                }

                DX11ShaderData shaderdata = this.deviceshaderdata[context];
                if ((shaderdata.IsValid &&
                    (this.geomconnected || settings.Geometry != null)
                    && this.spmax > 0 && this.varmanager.SetGlobalSettings(shaderdata.ShaderInstance, settings))
                    || this.FInApplyOnly[0])
                {
                    this.OnBeginQuery(context);

                    //Select preferred technique if available
                    if (settings.PreferredTechniques.Count == 0 && this.FInTechnique.SliceCount > 0 && this.techniqueindex != this.FInTechnique[0].Index)
                    {
                        this.techniqueindex = this.FInTechnique[0].Index;
                        this.techniquechanged = true;
                    }
                    else if (settings.PreferredTechniques.Count > 0)
                    {
                        int i = settings.GetPreferredTechnique(this.FShader);
                        if (i == -1)
                        {
                            i = this.FInTechnique[0].Index;
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

                        if (errorCount > 0)
                        {
                            this.FHost.Log(TLogType.Warning, sbMsg.ToString());
                        }

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

                    var objectsettings = this.objectSettings[context];
                    var orderedobjectsettings = this.orderedObjectSettings[context];
                    var variableCache = this.shaderVariableCache[context];
                    variableCache.ApplyGlobals(settings);

                    //IDX11Geometry drawgeom = null;
                    objectsettings.Geometry = null;
                    DX11Resource<IDX11Geometry> pg = null;
                    bool doOrder = false;
                    List<int> orderedSlices = null;
                    if (settings.LayerOrder != null && settings.LayerOrder.Enabled)
                    {
                        orderedobjectsettings.Clear();
                        for (int i = 0; i < this.spmax; i++)
                        {
                            DX11ObjectRenderSettings objSettings = new DX11ObjectRenderSettings();
                            objSettings.DrawCallIndex = i;
                            objSettings.Geometry = null;
                            objSettings.IterationCount = 1;
                            objSettings.IterationIndex = 0;
                            objSettings.WorldTransform = this.mworld[i % this.mworldcount];
                            objSettings.RenderStateTag = stateConnected ? this.FInState[i].Tag : null;

                            orderedobjectsettings.Add(objSettings);
                        }

                        orderedSlices = settings.LayerOrder.Reorder(settings, orderedobjectsettings);
                        doOrder = true;
                    }

                    int drawCount = doOrder ? orderedSlices.Count : this.spmax;

                    if (this.spmax == 0)
                    {
                        drawCount = 0;
                    }

                    bool singleGeometry = this.FGeometry.SliceCount == 1 || settings.Geometry != null;
                    if (settings.Geometry != null)
                    {
                        objectsettings.Geometry = settings.Geometry;
                        shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, 0);
                    }
                    else if (singleGeometry)
                    {
                        pg = this.FGeometry[0];
                        objectsettings.Geometry = pg[context];
                        if (objectsettings.Geometry == null)
                        {
                            objectsettings.Geometry = new DX11InvalidGeometry();
                        }
                        shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, 0);
                    }

                    if (!multistate)
                    {
                        objectsettings.RenderStateTag = this.FInState[0] != null ? this.FInState[0].Tag : null;
                    }

                    for (int i = 0; i < drawCount; i++)
                    {
                        int idx = doOrder ? orderedSlices[i] : i;
                        if (multistate)
                        {
                            context.RenderStateStack.Push(this.FInState[idx]);
                            objectsettings.RenderStateTag = this.FInState[idx] != null ? this.FInState[idx].Tag : null;
                        }

                        if (shaderdata.IsLayoutValid(idx) || settings.Geometry != null)
                        {
                            objectsettings.IterationCount = this.FIter[idx];

                            for (int k = 0; k < objectsettings.IterationCount; k++)
                            {
                                objectsettings.IterationIndex = k;

                                if (!singleGeometry)
                                {
                                    if (settings.Geometry == null)
                                    {
                                        if (this.FGeometry[idx] != pg)
                                        {
                                            pg = this.FGeometry[idx];

                                            objectsettings.Geometry = pg[context];
                                            if (objectsettings.Geometry == null)
                                            {
                                                objectsettings.Geometry = new DX11InvalidGeometry();
                                            }

                                            shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, idx);
                                        }
                                    }
                                    else
                                    {
                                        objectsettings.Geometry = settings.Geometry;
                                        shaderdata.SetInputAssembler(ctx, objectsettings.Geometry, idx);
                                    }
                                }


                                //Prepare settings
                                objectsettings.DrawCallIndex = idx;
                                objectsettings.WorldTransform = this.mworld[idx % this.mworldcount];

                                if (settings.ValidateObject(objectsettings))
                                {
                                    variableCache.ApplySlice(objectsettings, idx);

                                    for (int ip = 0; ip < shaderdata.PassCount; ip++)
                                    {
                                        shaderdata.ApplyPass(ctx, ip);

                                        if (settings.DepthOnly) { ctx.PixelShader.Set(null); }

                                        objectsettings.Geometry.Draw();

                                    }
                                }
                            }
                        }

                        if (multistate)
                        {
                            context.RenderStateStack.Pop();
                        }
                    }

                    shaderdata.ShaderInstance.CleanUp();

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
                this.FInLayer.RenderAll(context, settings);
            }

        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            this.deviceshaderdata.Dispose();
            this.shaderVariableCache.Dispose();
            this.FOutLayer.SafeDisposeAll();
            this.objectSettings.Dispose();
            this.orderedObjectSettings.Dispose();
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

            // load config to restore dynamic pins
            FConfigShader.Changed += HandleConfigShaderChangeOnStartup;
            FConfigDefines.Changed += HandleConfigDefinesChangeOnStartup;

        }

        bool ShaderCreatedByConfig = false;
        private void HandleConfigShaderChangeOnStartup(IDiffSpread<string> spread)
        {
            if (FConfigShader[0] != ""  && !configWritten)
            {
                DX11ShaderInclude FIncludeHandler = new DX11ShaderInclude();
                List<ShaderMacro> sms = new List<ShaderMacro>();
                
                FShader = DX11Effect.FromString(FConfigShader[0], FIncludeHandler, sms.ToArray());
                this.SetShader(FShader, true);
                ShaderCreatedByConfig = true;
            }
        }

        private void HandleConfigDefinesChangeOnStartup(IDiffSpread<string> spread)
        {
            if (FConfigShader[0] != "" && !configWritten)
            {

                DX11ShaderInclude FIncludeHandler = new DX11ShaderInclude();
                List<ShaderMacro> sms = new List<ShaderMacro>();

                if (FConfigDefines[0] != "")
                {
                    string[] defines = FConfigDefines[0].Split(",".ToCharArray());
                    for (int i = 0; i < defines.Length; i++)
                    {
                        try
                        {
                            string[] s = defines[i].Split("=".ToCharArray());

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
                }
                
                FShader = DX11Effect.FromString(FConfigShader[0], FIncludeHandler, sms.ToArray());
                this.SetShader(FShader, !ShaderCreatedByConfig);
                ShaderCreatedByConfig = true;
            }
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
