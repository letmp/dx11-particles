using System;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Camera", Category = "Transform Basic", Help = "Does a Perspective Projection with Lens Shift", Tags = "Projector", Author = "velcrome")]
    #endregion PluginInfo
    public class CameraSimpleNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Translate", DefaultValues = new[] { 0.0, 0, -1 })]
        public ISpread<Vector3D> FTranslate;

        [Input("Rotate", DefaultValues = new[] { 0.0, 0, 0 })]
        public ISpread<Vector3D> FRotate;

        [Input("Shift", DefaultValues = new[] { 0.0, 0 })]
        public ISpread<Vector2D> FShift;

        [Input("FoV", DefaultValue = 0.2)]
        public ISpread<Vector2D> FFov;

        [Input("Near Plane", DefaultValue = 0.05, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<double> FNear;

        [Input("Far Plane", DefaultValue = 100, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<double> FFar;

        [Input("Aspect Ratio", DefaultValue = 0.75, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<double> FAspect;

        [Output("View")]
        public ISpread<Matrix4x4> FView;

        [Output("Projection")]
        public ISpread<Matrix4x4> FProjection;

        [Output("ViewProjection")]
        public ISpread<Matrix4x4> FViewProjection;

        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FView.SliceCount =
            FProjection.SliceCount =
            FViewProjection.SliceCount = SpreadMax;


            for (int i = 0; i < SpreadMax; i++)
            {
                var translate = FTranslate[i];
                var rotate = FRotate[i];
                var fov = FFov[i];
                var shift = FShift[i];

                var near = FNear[i];
                var far = FFar[i];
                var aspect = FAspect[i];

                var view = VMath.Inverse(VMath.Rotate(rotate * Math.PI * 2) * VMath.Translate(translate));

                double scaleX = 1.0 / Math.Tan(fov[0] * Math.PI);
                double scaleY = 1.0 / Math.Tan(fov[1] * Math.PI);
                double fn = far / (far - near);

                var proj = new Matrix4x4(scaleX, 0, 0, 0,
                                              0, scaleY, 0, 0,
                                          -2 * shift.x, -2 * shift.y, fn, 1,
                                              0, 0, -near * fn, 0);

                FView[i] = view;
                FProjection[i] = proj;
                FViewProjection[i] = view * proj;
            }
        }
    }
}
