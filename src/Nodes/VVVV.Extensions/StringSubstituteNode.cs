#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Substitute", Category = "String", Version = "bin", Help = "Substitute strings.", Tags = "replace", Author = "tmp")]
    #endregion PluginInfo
    public class StringSubstituteNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", IsSingle = true)]
        public IDiffSpread<string> FIn;

        [Input("From")]
        public IDiffSpread<string> FFrom;

        [Input("To")]
        public IDiffSpread<ISpread<string>> FTo;

        [Output("Output")]
        public ISpread<string> FOut;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if ( ! (FIn.IsChanged || FFrom.IsChanged || FTo.IsChanged )) return;

            FOut.SliceCount = 1;
            FOut[0] = FIn[0];
            for (int i = 0; i < FFrom.SliceCount; i++)
            {
                if (FFrom[i].Length > 0)
                    FOut[0] = FOut[0].Replace(FFrom[i], string.Join(System.Environment.NewLine, FTo[i]));
            }
        }
    }

}
