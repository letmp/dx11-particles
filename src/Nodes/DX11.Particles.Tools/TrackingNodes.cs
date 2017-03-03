using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

namespace DX11.Particles.Tools
{
    #region PluginInfo
    [PluginInfo(Name = "NearestPairs", Category = "Value", Help = "Find closest direct matches", Tags = "ID, Tracking", Author = "velcrome")]
    #endregion PluginInfo
    public class NearestPairsDouble : NearestPairs<double>
    {

        public override double Distance(double input, double other)
        {
            return Math.Abs(input - other);
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "NearestPairs", Category = "Vector3D", Help = "Find closest direct matches", Tags = "ID, Tracking", Author = "velcrome")]
    #endregion PluginInfo
    public class NearestPairsVector3D : NearestPairs<Vector3D>
    {

        public override double Distance(Vector3D input, Vector3D other)
        {
            return (input - other).Length;
        }

        protected override int Index(ISpread<Vector3D> spread, Vector3D item)
        {
            int i = 0;
            foreach (var v in spread)
            {
                if (item.x == v.x && item.y == v.y && item.z == v.z) return i;
                i++;
            }
            return -1;
        }
    }

    public abstract class NearestPairs<T> : IPluginEvaluate// where T: IEquatable<T>
    {
        #region fields & pins
        [Input("Input")]
        public ISpread<T> FInput;

        [Input("Other")]
        public ISpread<T> FOther;

        [Input("MinDistance", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<double> FMin;

        [Input("MaxDistance", DefaultValue = 0.5)]
        public ISpread<double> FMax;

        [Output("Output")]
        public ISpread<T> FOutput;

        [Output("Former Slice")]
        public ISpread<int> FFormer;

        [Output("Other Slice")]
        public ISpread<int> FFormerOther;

        [Output("Unpaired Input")]
        public ISpread<T> FNew;

        [Output("Unpaired Input Former Slice")]
        public ISpread<int> FFormerNew;

        [Output("Unpaired Other")]
        public ISpread<T> FOld;

        [Output("Unpaired Other Slice")]
        public ISpread<int> FFormerOld;

        [Import()]
        public ILogger FLogger;

        protected List<T> FoundInputs = new List<T>();
        protected List<T> FoundOthers = new List<T>();

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount =
            FFormer.SliceCount =
            FFormerOther.SliceCount = 0;


            // 			remove redundancies
            var inputs = FInput;
            var others = FOther;

            var map =
                from input in inputs
                from other in others
                let distance = Distance(input, other)
                where distance <= FMax[0] && distance >= FMin[0]
                orderby distance ascending
                select new MapElement<T>(distance, input, other);

            FoundInputs.Clear();
            FoundOthers.Clear();

            for (int i = 0; i < FInput.SliceCount; i++)
            {
                var inputItem = map.FirstOrDefault();

                if (inputItem != null)
                {
                    FoundInputs.Add(inputItem.Input);
                    FoundOthers.Add(inputItem.Other);

                    var newMap =
                        from item in map
                        where (!item.Input.Equals(inputItem.Input) && !item.Other.Equals(inputItem.Other))
                        select item;

                    map = newMap;

                    FOutput.Add(inputItem.Input);

                    FFormer.Add(Index(FInput, inputItem.Input));
                    FFormerOther.Add(Index(FOther, inputItem.Other));
                }
            }

            FFormerNew.SliceCount =
            FNew.SliceCount =
            FFormerOld.SliceCount =
            FOld.SliceCount = 0;

            foreach (var input in inputs.Except(FoundInputs))
            {
                FNew.Add(input);
                FFormerNew.Add(Index(FInput, input));
            }

            foreach (var input in others.Except(FoundOthers))
            {
                FOld.Add(input);
                FFormerOld.Add(Index(FOther, input));
            }
        }

        protected virtual int Index(ISpread<T> spread, T item)
        {
            return spread.IndexOf(item);
        }

        public abstract double Distance(T a, T b);


    }

    public class MapElement<T>
    {
        public double Distance;
        public T Input;
        public T Other;

        public MapElement(double distance, T input, T other)
        {
            this.Distance = distance;
            this.Input = input;
            this.Other = other;
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Cache", Category = "Vector3D", Help = "Basic template with one value in/out", Tags = "")]
    #endregion PluginInfo
    public class Vector3DCacheNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        public ISpread<Vector3D> FInput;

        [Input("ID")]
        public ISpread<int> FIndex;

        [Input("Retain Time", IsSingle = true, DefaultValue = 1.0)]
        public ISpread<double> FTime;

        [Input("Reset", IsSingle = true)]
        public ISpread<bool> FReset;

        [Output("Cached Output")]
        public ISpread<Vector3D> FOutput;

        [Output("Cached ID")]
        public ISpread<int> FCachedID;

        [Import()]
        public ILogger FLogger;

        [Import()]
        protected IHDEHost FHDEHost;


        protected Dictionary<int, Vector3D> dict = new Dictionary<int, Vector3D>();
        protected Dictionary<int, double> mark = new Dictionary<int, double>();

        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FReset[0])
            {
                dict.Clear();
                mark.Clear();
            }

            SpreadMax = FInput.CombineWith(FIndex);

            if (FInput.SliceCount > 0 && FIndex.SliceCount > 0)
                for (int i = 0; i < SpreadMax; i++)
                {
                    dict[FIndex[i]] = FInput[i];
                    mark[FIndex[i]] = FHDEHost.FrameTime;
                }

            var validTime = FHDEHost.FrameTime - FTime[0];

            var clear = from id in mark.Keys
                        where mark[id] < validTime
                        select id;

            foreach (var id in clear.ToArray())
            {
                if (mark[id] < validTime)
                {
                    mark.Remove(id);
                    dict.Remove(id);
                }
            }

            FOutput.SliceCount =
            FCachedID.SliceCount = 0;
            FOutput.AssignFrom(dict.Values);
            FCachedID.AssignFrom(dict.Keys);

        }
    }
}
