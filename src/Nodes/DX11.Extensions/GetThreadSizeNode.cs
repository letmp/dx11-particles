#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "GetThreadSize", Category = "Value", Help = "Calculates threadsizes to set up a dispatcher. The input-spread is distributed over threadX, threadY and threadZ.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class GetThreadSizeNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Element Count", DefaultValue = 1.0)]
        public IDiffSpread<int> FEleCount;

        [Input("Group Size", DefaultValue = 1.0)]
        public IDiffSpread<int> FGroup;

        [Output("ThreadX")]
        public ISpread<int> FThreadX;

        [Output("ThreadY")]
        public ISpread<int> FThreadY;

        [Output("ThreadZ")]
        public ISpread<int> FThreadZ;

        [Output("String")]
        public ISpread<string> FString;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (FEleCount.IsChanged || FGroup.IsChanged)
            {
                FThreadX.SliceCount = FThreadY.SliceCount = FThreadZ.SliceCount = FString.SliceCount = 0;

                for (int i = 0; i < SpreadMax; i++)
                {
                    int gSize = FGroup[i];
                    int threadsize = (FEleCount[i] + gSize - 1) / gSize;

                    if (i % 3 == 0) { FThreadX.Add(threadsize); FString.Add("XTHREADS=" + gSize); }
                    if (i % 3 == 1) { FThreadY.Add(threadsize); FString.Add("YTHREADS=" + gSize); }
                    if (i % 3 == 2) { FThreadZ.Add(threadsize); FString.Add("ZTHREADS=" + gSize); }

                }

                if (FThreadX.SliceCount == 0) { FThreadX.Add(1); FString.Add("XTHREADS=1"); }
                if (FThreadY.SliceCount == 0) { FThreadY.Add(1); FString.Add("YTHREADS=1"); }
                if (FThreadZ.SliceCount == 0) { FThreadZ.Add(1); FString.Add("ZTHREADS=1"); }

            }

            
        }
    }
}
