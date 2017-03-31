using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "ViewFrustumCulling", Category = "Vector3D", Version = "Transform", Help = "Outputs all given 3d Points that are within a ViewProjection", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class ViewFrustumCullingNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Position")]
        public ISpread<Vector3D> FPosition;

        [Input("ViewProjection", IsSingle = true)]
        public ISpread<Matrix4x4> FViewProjection;

        [Output("Position")]
        public ISpread<Vector3D> FOutput;

        [Output("Former Index")]
        public ISpread<int> FIndex;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = FIndex.SliceCount = 0;

            for (int i = 0; i < SpreadMax; i++)
            {
                Vector3D coord = FViewProjection[0] * FPosition[i];
                if (!(coord.x < -1 || coord.x > 1 || coord.y < -1 || coord.y > 1 || coord.z < -1 || coord.z > 1))
                {
                    FOutput.Add(FPosition[i]);
                    FIndex.Add(i);
                }
                    
            }
        }
    }
}
