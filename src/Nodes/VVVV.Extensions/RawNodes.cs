using System;
using System.ComponentModel.Composition;
using System.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Splitter", Category = "Raw", Help = "Splits a raw stream in spreads with a given size.", Tags = "")]
    #endregion PluginInfo
    public class RawSplitterNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Input")]
        public ISpread<Stream> FInput;

        [Input("Count", MinValue = 0, DefaultValue = 768)]
        public IDiffSpread<int> FCount;

        [Output("Output")]
        public ISpread<ISpread<Stream>> FOutput;

        //when dealing with byte streams (what we call Raw in the GUI) it's always
        //good to have a byte buffer around. we'll use it when copying the data.
        protected byte[] FBuffer;
        #endregion fields & pins

        //called when all inputs and outputs defined above are assigned from the host
        public void OnImportsSatisfied()
        {
            //start with an empty stream output
            FOutput.SliceCount = 0;
            FCount.Changed += ResizeBuffer;
        }

        protected void ResizeBuffer(IDiffSpread<int> spread)
        {
            FBuffer = new byte[FCount[0]];
        }


        //called when data for any output pin is requested
        public void Evaluate(int spreadMax)
        {
            spreadMax = FInput.SliceCount;
            FOutput.SliceCount = spreadMax;

            for (int i = 0; i < spreadMax; i++)
            {
                var input = FInput[i];
                int count = (int)Math.Ceiling((double)input.Length / FCount[0]);

                input.Position = 0;
                FOutput[i].ResizeAndDispose(count, () => new MemoryStream(FCount[0]));

                int length = (int)input.Length;
                for (int j = 0; j < count; j++)
                {
                    var output = FOutput[i][j];
                    output.Position = 0;

                    var numBytesToCopy = Math.Min(length, FCount[0]);
                    output.SetLength(numBytesToCopy);

                    input.Read(FBuffer, 0, numBytesToCopy);
                    output.Write(FBuffer, 0, numBytesToCopy);
                    
                    length -= numBytesToCopy;
                }

            }
        }
    }

    
}
