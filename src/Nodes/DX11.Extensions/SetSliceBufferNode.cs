using System.Collections.Generic;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using FeralTic.DX11.Resources;
using FeralTic.DX11;

namespace VVVV.DX11.Nodes
{

    #region PluginInfo
    [PluginInfo(Name = "SetSlice", Category = "IDX11RWStructureBuffer", Help = "Replace individual slices of the spread with the given input.", Author = "tmp")]
    #endregion PluginInfo
    public class SiftBufferNode : IPluginEvaluate, IDX11ResourceProvider
    {
        #region fields & pins
        [Input("Input Buffers")]
        protected IDiffSpread<DX11Resource<IDX11RWStructureBuffer>> FInBuffers;

        [Input("New Buffers")]
        public IDiffSpread<DX11Resource<IDX11RWStructureBuffer>> FInBuffersNew;

        [Input("Index", DefaultValue = 0)]
        public ISpread<int> FInIndex;

        [Output("Output")]
        public ISpread<DX11Resource<IDX11RWStructureBuffer>> FOutBuffers;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutBuffers.SliceCount = FInBuffers.SliceCount;
            FOutBuffers.AssignFrom(FInBuffers);

            for (int i = 0; i < FInIndex.SliceCount; i++)
            {
                FOutBuffers[FInIndex[i]] = FInBuffersNew[i];
            }
        }

        public void Update(IPluginIO pin, DX11RenderContext context)
        {
        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
        }

    }
}
