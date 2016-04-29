using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

using VVVV.DX11;
using FeralTic.DX11.Resources;

namespace DX11.Particles.Core
{
    #region PluginInfo
    [PluginInfo(Name = "Sift", Category = "DX11.Particles.Core", Version="Buffer", Help = "Sifts a spread of buffers by their semantics and splits them.", Author="tmp", AutoEvaluate=true)]
    #endregion PluginInfo
    public class SiftBufferNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Input", DefaultValue = 1.0)]
        public IDiffSpread<DX11Resource<IDX11RWStructureBuffer>> FInput;

        [Input("BufferSemantic")]
        public IDiffSpread<string> FBufferSemantic;

        [Input("BufferSemantic Filter")]
        public IDiffSpread<string> FBufferSemanticFilter;

        public Spread<IIOContainer<ISpread<DX11Resource<IDX11RWStructureBuffer>>>> FOutputs = new Spread<IIOContainer<ISpread<DX11Resource<IDX11RWStructureBuffer>>>>();

        [Import]
        public IIOFactory FIOFactory;

        List<int> indices = new List<int>();
        List<string> semantics = new List<string>();
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            //FInput.Changed += HandleBufferChange;
            //FBufferSemantic.Changed += HandleStringChange;
            FBufferSemanticFilter.Changed += HandleStringChange;
        }

        public void Evaluate(int SpreadMax)
        {
        }

        private void HandleBufferChange(IDiffSpread<DX11Resource<IDX11RWStructureBuffer>> spread)
        {
            Sift();
            if (indices.Count > 0) SetOutputPins();
        }

        private void HandleStringChange(IDiffSpread<string> spread)
        {
            
            Sift();
            if (indices.Count > 0) SetOutputPins();
        }

        private void Sift()
        {
                indices.Clear();
                semantics.Clear();
                for (int i = 0; i < FBufferSemanticFilter.SliceCount; i++)
                {
                    string semanticFilter = FBufferSemanticFilter[i];
                    for (int j = 0; j < FBufferSemantic.SliceCount; j++)
                    {
                        if (FBufferSemantic[j].Equals(semanticFilter))
                        {
                            indices.Add(j);
                            semantics.Add(semanticFilter);
                            break;
                        }
                    }
                }
        }

        private void SetOutputPins()
        {
            HandlePinCountChanged(indices.Count, FOutputs, (i) => new OutputAttribute(semantics[i - 1]));
            
            for (int i = 0; i < indices.Count; i++)
            {
                var outputSpread = FOutputs[i].IOObject;
                outputSpread[0] = FInput[indices[i]];
            }
        }

        private void HandlePinCountChanged<T>(int count, Spread<IIOContainer<T>> pinSpread, Func<int, IOAttribute> ioAttributeFactory) where T : class
        {
            pinSpread.ResizeAndDispose(
                count,
                (i) =>
                {
                    var ioAttribute = ioAttributeFactory(i + 1);
                    return FIOFactory.CreateIOContainer<T>(ioAttribute);
                }
            );
        }
    }
}
