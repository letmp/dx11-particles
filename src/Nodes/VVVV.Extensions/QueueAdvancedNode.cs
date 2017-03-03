#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using System.Linq;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Queue", Category = "Transform", Version = "Advanced", Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO fashion", Author = "tmp")]
    #endregion PluginInfo
    public class AdvancedMatrix4x4QueueNode : AdvancedGenericSpreadsQueueNode<Matrix4x4> { }

    #region PluginInfo
    [PluginInfo(Name = "Queue", Category = "Vector4d", Version = "Advanced", Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO fashion", Author = "tmp")]
    #endregion PluginInfo
    public class AdvancedVector4dQueueNode : AdvancedGenericSpreadsQueueNode<Vector4D> { }

    #region PluginInfo
    [PluginInfo(Name = "Queue", Category = "Vector3d", Version = "Advanced", Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO fashion", Author = "tmp")]
    #endregion PluginInfo
    public class AdvancedVector3dQueueNode : AdvancedGenericSpreadsQueueNode<Vector3D> { }

    #region PluginInfo
    [PluginInfo(Name = "Queue", Category = "Vector2d", Version = "Advanced", Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO fashion", Author = "tmp")]
    #endregion PluginInfo
    public class AdvancedVector2dQueueNode : AdvancedGenericSpreadsQueueNode<Vector2D> { }

    #region PluginInfo
    [PluginInfo(Name = "Queue", Category = "String", Version = "Advanced", Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO fashion", Author = "tmp")]
    #endregion PluginInfo
    public class AdvancedStringQueueNode : AdvancedGenericSpreadsQueueNode<string> { }

    public class AdvancedGenericSpreadsQueueNode<T> : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", DefaultValue = 1.0)]
        public ISpread<T> FInput;

        [Input("Slot Count", DefaultValue = 1, MinValue = 1, IsSingle = true)]
        public IDiffSpread<int> FSlotCount;

        [Input("Frame Count", DefaultValue = 1, MinValue = -1)]
        public IDiffSpread<int> FFrameCount;

        [Input("Insert")]
        public ISpread<Boolean> FInsert;

        [Input("Reset", IsBang = true)]
        public ISpread<Boolean> FReset;

        [Output("Output")]
        public ISpread<ISpread<T>> FOutput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        List<Queue<T>> queueList;

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = FSlotCount[0];

            if (queueList == null || FSlotCount.IsChanged || FReset[0])
            {
                queueList = new List<Queue<T>>();

                for (int i = 0; i < FSlotCount[0]; i++)
                {
                    FOutput[i].SliceCount = 0;
                    queueList.Add(new Queue<T>());
                }
            }

            for (int i = 0; i < FSlotCount[0]; i++)
            {
                if (FInsert[i] == true)
                {
                    queueList[i].Enqueue(FInput[i]);
                    if (queueList[i].Count > FFrameCount[i]) queueList[i].Dequeue();
                    FOutput[i].AssignFrom(queueList[i].Reverse());
                }
            }

        }

    }
}
