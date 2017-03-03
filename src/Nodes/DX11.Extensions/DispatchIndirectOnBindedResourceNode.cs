using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using FeralTic.Resources.Geometry;
using SlimDX.Direct3D11;
using VVVV.DX11.Lib.Rendering;


namespace VVVV.DX11.Nodes
{
    public enum DispatchIndirectMode
    {
        ArgumentBuffer,
        CopyCounter
    }
    [PluginInfo(Name = "DispatchIndirectOnBindedBuffer", Category = "DX11.Layer", Version = "", Author = "microdee")]
    public class DX11DispatchIndirectOnBindedBufferNode : IPluginEvaluate, IDX11LayerHost, IDX11UpdateBlocker
    {
        //[Input("Preliminary Modification", AutoValidate = false)]
        //protected Pin<DX11Resource<DX11Layer>> FPreModLayer;
        [Input("Layer In", AutoValidate = false)]
        protected Pin<DX11Resource<DX11Layer>> FLayerIn;

        [Input("RW Buffer Semantic")]
        protected ISpread<string> FSemantic;
        [Input("Indirect Mode")]
        protected ISpread<DispatchIndirectMode> FMode;
        [Input("Offset", MinValue = 0)]
        protected ISpread<int> FOffs;

        [Input("Enabled", DefaultValue = 1, Order = 100000)]
        protected IDiffSpread<bool> FEnabled;

        [Output("Layer Out")]
        protected ISpread<DX11Resource<DX11Layer>> FOutLayer;

        private DispatchIndirectBuffer dispatchBuffer;
        private DX11NullIndirectDispatcher indirectDispatch;
        private DX11Resource<DX11NullGeometry> indirectGeom;

        public void Evaluate(int SpreadMax)
        {
            if (this.FOutLayer[0] == null) { this.FOutLayer[0] = new DX11Resource<DX11Layer>(); }
            if(indirectGeom == null) { indirectGeom = new DX11Resource<DX11NullGeometry>(); }

            if (this.FEnabled[0])
            {
                this.FLayerIn.Sync();
            }
        }


        #region IDX11ResourceProvider Members

        public void Update( DX11RenderContext context)
        {
            if (this.dispatchBuffer == null)
            {
                this.dispatchBuffer = new DispatchIndirectBuffer(context);
            }
            if (!indirectGeom.Contains(context))
            {
                indirectDispatch = new DX11NullIndirectDispatcher();
                indirectDispatch.IndirectArgs = dispatchBuffer;

                DX11NullGeometry nullgeom = new DX11NullGeometry(context);
                nullgeom.AssignDrawer(this.indirectDispatch);
                indirectGeom[context] = nullgeom;
            }
            if (!this.FOutLayer[0].Contains(context))
            {
                this.FOutLayer[0][context] = new DX11Layer();
                this.FOutLayer[0][context].Render = this.Render;
            }
        }

        public void Destroy( DX11RenderContext context, bool force)
        {
            this.FOutLayer[0].Dispose(context);
        }

        public void Render( DX11RenderContext context, DX11RenderSettings settings)
        {
            IDX11Geometry g = settings.Geometry;

            if (this.FEnabled[0])
            {
                if (this.FLayerIn.IsConnected)
                {
                    RWStructuredBufferRenderSemantic ccrs = null;
                    foreach (var rsem in settings.CustomSemantics)
                    {
                        if (rsem.Semantic == FSemantic[0])
                        {
                            if (rsem is RWStructuredBufferRenderSemantic)
                            {
                                ccrs = rsem as RWStructuredBufferRenderSemantic;
                            }
                        }
                    }
                    if (ccrs != null)
                    {
                        var srcbuf = ccrs.Data as DX11RWStructuredBuffer;
                        if (srcbuf != null)
                        {
                            var argBuffer = dispatchBuffer.Buffer;
                            UnorderedAccessView uav = srcbuf.UAV;
                            if (FMode[0] == DispatchIndirectMode.CopyCounter)
                            {
                                context.CurrentDeviceContext.CopyStructureCount(uav, argBuffer, 0);
                            }
                            else
                            {
                                ResourceRegion rr = new ResourceRegion(FOffs[0] * 4, 0, 0, FOffs[0] * 4 + 4, 1, 1);
                                context.CurrentDeviceContext.CopySubresourceRegion(srcbuf.Buffer, 0, rr, argBuffer, 0, 0, 0, 0);
                            }

                            settings.Geometry = indirectGeom[context];
                        }
                    }
                    FLayerIn[0][context].Render( context, settings);
                }
            }
            settings.Geometry = g;
        }

        #endregion

        public bool Enabled
        {
            get { return this.FEnabled[0]; }
        }
    }
}
