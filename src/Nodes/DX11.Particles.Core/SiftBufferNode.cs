using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

using SlimDX.Direct3D11;
using VVVV.DX11;
using FeralTic.DX11;
using FeralTic.DX11.Resources;

namespace DX11.Particles.Core
{
    #region PluginInfo
    [PluginInfo(Name = "Sift", Category = "DX11.Particles.Core", Version="Buffer", Help = "Sifts a spread of buffers by their semantics and splits them.", Author="tmp", AutoEvaluate=true)]
    #endregion PluginInfo
    public class SiftBufferNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDX11ResourceProvider
    {
        #region fields & pins
        [Input("Input", DefaultValue = 1.0)]
        //public IDiffSpread<DX11Resource<IDX11RWStructureBuffer>> FInput;
        public Pin<DX11Resource<IDX11RWStructureBuffer>> FInput;

        [Input("BufferSemantic")]
        public IDiffSpread<string> FBufferSemantic;

        [Input("BufferSemantic Filter")]
        public IDiffSpread<string> FBufferSemanticFilter;

        [Config("Config", DefaultString = "")]
        public IDiffSpread<string> FConfig;

        public Spread<IIOContainer<ISpread<DX11Resource<IDX11RWStructureBuffer>>>> FOutputs = new Spread<IIOContainer<ISpread<DX11Resource<IDX11RWStructureBuffer>>>>();

        [Import]
        public IIOFactory FIOFactory;
        
        private bool wasConnected = false;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FBufferSemantic.Changed += HandleStringChange;
            FBufferSemanticFilter.Changed += HandleStringChange;
            FConfig.Changed += HandleConfigChange;
        }

        public void Evaluate(int SpreadMax)
        {
            // assign input buffers to output pins when the buffer spread gets (dis)connected
            if (FInput.IsConnected != wasConnected)
            {
                HandleConfigChange(null);
                wasConnected = FInput.IsConnected;
            }
        }

        public void Update(IPluginIO pin, DX11RenderContext context)
        {
        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
        }

        private void HandleStringChange(IDiffSpread<string> spread)
        {

            string configString = "";
            bool first = true;

            for (int i = 0; i < FBufferSemanticFilter.SliceCount; i++)
                {
                    string semanticFilter = FBufferSemanticFilter[i];
                    for (int j = 0; j < FBufferSemantic.SliceCount; j++)
                    {
                        if (FBufferSemantic[j].Equals(semanticFilter))
                        {
                            if (!first) configString += ",";
                            configString += j + ":" + semanticFilter;
                            first = false;
                            break;
                        }
                    }
                }
            if (!FConfig[0].Equals(configString) && configString != ""){
                FConfig[0] = configString;
            }
            
        }

        private void HandleConfigChange(IDiffSpread<string> spread)
        {
            string[] entries = FConfig[0].Split(",".ToCharArray());
            int length = entries.Length;

            if (entries[0] != "")
            {
                
                HandlePinCountChanged(length, FOutputs, (i) => new OutputAttribute(entries[i-1].Split(":".ToCharArray())[1]));

                int cnt = 0;
                foreach (string entry in entries)
                {
                    if (entry != "")
                    {
                        int slicenumber = Convert.ToInt32(entry.Split(":".ToCharArray())[0]);
                        var outputSpread = FOutputs[cnt].IOObject;
                        outputSpread[0] = FInput[slicenumber];
                        cnt++;
                    }
                }
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
