using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Indexer", Category = "String", AutoEvaluate = true, Help = "Session Management based on unique string-based Names. Outputs are useful for feeding a Buffer.", Tags = "Buffer", Author = "velcrome")]
    #endregion PluginInfo
    public class StringIndexerNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Name", DefaultString = "id")]
        public ISpread<string> FInput;

        [Input("Remove Name", DefaultString = "")]
        public ISpread<string> FRemove;

        [Input("Timeout", IsSingle = true)]
        public IDiffSpread<TimeSpan> FTimeOut;

        [Input("Index Count", DefaultValue = 256, IsSingle = true)]
        public IDiffSpread<int> FCount;

        [Output("Index")]
        public ISpread<int> FFormerId;

        [Output("Fresh")]
        public ISpread<int> FIdFresh;

        [Output("Removed")]
        public ISpread<int> FIdRemoved;

        [Output("Saved Index")]
        public ISpread<int> FId;

        [Output("Saved Name")]
        public ISpread<string> FOutput;

        protected Dictionary<string, int> Data = new Dictionary<string, int>();
        protected Dictionary<string, DateTime> Age = new Dictionary<string, DateTime>();
        protected HashSet<int> FreeIndices = new HashSet<int>();

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            FCount.Changed += CountChanged;
        }

        protected void CountChanged(IDiffSpread<int> spread)
        {
            Reset();
        }

        protected void Reset()
        {
            Data.Clear();
            Age.Clear();

            FreeIndices.Clear();
            for (int i = 0; i < FCount[0]; i++) FreeIndices.Add(i);
        }


        public void Evaluate(int SpreadMax)
        {
            FFormerId.SliceCount =
            FIdFresh.SliceCount =
            FIdRemoved.SliceCount = 0;

            for (int i = 0; i < FRemove.SliceCount; i++)
            {
                var remove = FRemove[i];
                remove = remove.Trim();
                if (remove == "") continue;

                if (Data.ContainsKey(remove))
                {
                    var index = Data[remove];
                    FreeIndices.Add(index);
                    Data.Remove(remove);
                    Age.Remove(remove);
                    FIdRemoved.Add(index);
                }
            }


            for (int i = 0; i < FInput.SliceCount; i++)
            {
                var input = FInput[i].Trim();
                if (input == "") continue;

                int index;
                if (!Data.ContainsKey(input))
                {
                    index = FreeIndices.DefaultIfEmpty(int.MinValue).First(); ;
                    if (index == int.MinValue)
                    {
                        index = RemoveOldest();
                        FIdRemoved.Add(index);
                    }

                    FreeIndices.Remove(index);
                    FIdFresh.Add(index);
                }
                else index = Data[input];

                Data[input] = index;
                Age[input] = DateTime.Now;
                FFormerId.Add(Data[input]);
            }

            var maxAge = FTimeOut[0];
            var sessionEnabled = false;
            if (maxAge != null && maxAge.Ticks > 0)
                sessionEnabled = true;

            foreach (var item in Age.Keys.ToArray())
            {
                var index = Data[item];
                if (sessionEnabled && Age[item] + maxAge < DateTime.Now)
                {
                    FreeIndices.Add(index);
                    Data.Remove(item);
                    Age.Remove(item);
                    FIdRemoved.Add(index);
                }
            }

            var sorted = Data.Values.ToList();
            sorted.Sort();
            FId.AssignFrom(sorted);

            FOutput.SliceCount = 0;
            SpreadMax = sorted.Count;

            FOutput.AssignFrom(
                    from key in sorted
                    from index in Data.Keys
                    where Data[index] == key
                    select index
                    );
        }

        private int RemoveOldest()
        {
            var oldest = (
                from id in Age.Keys
                orderby Age[id]
                select id
            ).First();

            var index = Data[oldest];
            Data.Remove(oldest);
            Age.Remove(oldest);
            FreeIndices.Add(index);

            return index;
        }
    }
}
