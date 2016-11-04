using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using FeralTic.DX11;
using FeralTic.DX11.Resources;
using SlimDX.Direct3D11;
using VVVV.DX11.Lib.Rendering;
using Buffer = SlimDX.Direct3D11.Buffer;


namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "CopyCounter", Category = "DX11.Layer", Version = "", Author = "microdee")]
    public class DX11LayerCopyCounterNode : IPluginEvaluate, IDX11LayerProvider, IDX11UpdateBlocker
    {
        [Input("Layer In", AutoValidate = false)]
        protected Pin<DX11Resource<DX11Layer>> FLayerIn;

        [Input("Source Buffer Semantic")]
        protected ISpread<string> FSrcSem;
        [Input("Destination Buffer Semantic")]
        protected ISpread<string> FDstSem;
        [Input("Offset in Destination", MinValue = 0)]
        protected ISpread<int> FOffs;

        [Input("Enabled", DefaultValue = 1, Order = 100000)]
        protected IDiffSpread<bool> FEnabled;

        [Output("Layer Out")]
        protected ISpread<DX11Resource<DX11Layer>> FOutLayer;

        public void Evaluate(int SpreadMax)
        {
            if (this.FOutLayer[0] == null) { this.FOutLayer[0] = new DX11Resource<DX11Layer>(); }

            if (this.FEnabled[0])
            {
                this.FLayerIn.Sync();
            }
        }


        #region IDX11ResourceProvider Members

        public void Update(IPluginIO pin, DX11RenderContext context)
        {
            if (!this.FOutLayer[0].Contains(context))
            {
                this.FOutLayer[0][context] = new DX11Layer();
                this.FOutLayer[0][context].Render = this.Render;
            }
        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
            this.FOutLayer[0].Dispose(context);
        }

        public void Render(IPluginIO pin, DX11RenderContext context, DX11RenderSettings settings)
        {
            if (this.FEnabled[0])
            {
                if (this.FLayerIn.IsConnected)
                {
                    var spreadmax = Math.Max(FSrcSem.SliceCount, FDstSem.SliceCount);
                    for (int i = 0; i < spreadmax; i++)
                    {
                        RWStructuredBufferRenderSemantic srcccrs = null;
                        foreach (var rsem in settings.CustomSemantics)
                        {
                            if (rsem.Semantic == FSrcSem[i])
                            {
                                if (rsem is RWStructuredBufferRenderSemantic)
                                {
                                    srcccrs = rsem as RWStructuredBufferRenderSemantic;
                                    break;
                                }
                            }
                        }
                        IDX11RWResource dstccrs = null;
                        foreach (var rsem in settings.CustomSemantics)
                        {
                            if (rsem.Semantic == FDstSem[i])
                            {
                                if (rsem is RWStructuredBufferRenderSemantic)
                                {
                                    var t = rsem as RWStructuredBufferRenderSemantic;
                                    dstccrs = t.Data;
                                    break;
                                }
                                if (rsem is RWBufferRenderSemantic)
                                {
                                    var t = rsem as RWBufferRenderSemantic;
                                    dstccrs = t.Data;
                                    break;
                                }
                            }
                        }
                        if ((srcccrs != null) && (dstccrs != null))
                        {
                            var srcbuf = srcccrs.Data as DX11RWStructuredBuffer;
                            if (srcbuf != null)
                            {
                                UnorderedAccessView uav = srcbuf.UAV;
                                Buffer dstbuf = null;
                                if (dstccrs is DX11RWStructuredBuffer)
                                {
                                    var t = dstccrs as DX11RWStructuredBuffer;
                                    dstbuf = t.Buffer;
                                }
                                if (dstccrs is DX11RawBuffer)
                                {
                                    var t = dstccrs as DX11RawBuffer;
                                    dstbuf = t.Buffer;
                                }
                                if (dstbuf != null)
                                    context.CurrentDeviceContext.CopyStructureCount(uav, dstbuf, FOffs[i]);
                            }
                        }
                    }
                    FLayerIn[0][context].Render(FLayerIn.PluginIO, context, settings);
                }
            }
        }

        #endregion

        public bool Enabled
        {
            get { return this.FEnabled[0]; }
        }
    }
}
