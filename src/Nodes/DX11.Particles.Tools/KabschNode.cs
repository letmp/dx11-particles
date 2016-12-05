using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using DotNetMatrix;

namespace DX11.Particles.Tools
{
    #region PluginInfo
    [PluginInfo(Name = "Kabsch", Category = "3d Vector", Help = "The kabsch algorithm is a method for calculating the optimal rotation matrix that minimizes the root mean squared deviation between two paired sets of points.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class ValueSVDNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("From ")]
        public ISpread<Vector3D> FInputQ;

        [Input("To ")]
        public ISpread<Vector3D> FInputP;

        [Input("Enabled")]
        public ISpread<bool> FInputEnabled;

        [Output("Transform Out")]
        public ISpread<Matrix4x4> FOutput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        // this is the straight forward implementation of the kabsch algorithm.
        // see http://en.wikipedia.org/wiki/Kabsch_algorithm for a detailed explanation.
        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = 1;
            Matrix4x4 mOut;

            if (FInputP.SliceCount > 0 && FInputQ.SliceCount > 0 && FInputEnabled[0])
            {

                // ======================== STEP 1 ========================
                // translate both sets so that their centroids coincides with the origin
                // of the coordinate system.

                double[] meanP = new double[3] { 0.0, 0.0, 0.0 }; // mean of first point set
                for (int i = 0; i < FInputP.SliceCount; i++)
                {
                    meanP[0] += FInputP[i].x;
                    meanP[1] += FInputP[i].y;
                    meanP[2] += FInputP[i].z;
                }
                meanP[0] /= FInputP.SliceCount;
                meanP[1] /= FInputP.SliceCount;
                meanP[2] /= FInputP.SliceCount;
                double[][] centroidP = new double[3][] { new double[] { meanP[0] }, new double[] { meanP[1] }, new double[] { meanP[2] } };
                GeneralMatrix mCentroidP = new GeneralMatrix(centroidP);

                double[][] arrayP = new double[FInputP.SliceCount][];
                for (int i = 0; i < FInputP.SliceCount; i++)
                {
                    arrayP[i] = new double[3];
                    arrayP[i][0] = FInputP[i].x - meanP[0]; // subtract the mean values from the incoming pointset
                    arrayP[i][1] = FInputP[i].y - meanP[1];
                    arrayP[i][2] = FInputP[i].z - meanP[2];
                }
                // this is the matrix of the first pointset translated to the origin of the coordinate system
                GeneralMatrix P = new GeneralMatrix(arrayP);

                double[] meanQ = new double[3] { 0.0, 0.0, 0.0 }; // mean of second point set
                for (int i = 0; i < FInputQ.SliceCount; i++)
                {
                    meanQ[0] += FInputQ[i].x;
                    meanQ[1] += FInputQ[i].y;
                    meanQ[2] += FInputQ[i].z;
                }
                meanQ[0] /= FInputQ.SliceCount;
                meanQ[1] /= FInputQ.SliceCount;
                meanQ[2] /= FInputQ.SliceCount;
                double[][] centroidQ = new double[3][] { new double[] { meanQ[0] }, new double[] { meanQ[1] }, new double[] { meanQ[2] } };
                GeneralMatrix mCentroidQ = new GeneralMatrix(centroidQ);

                double[][] arrayQ = new double[FInputQ.SliceCount][];
                for (int i = 0; i < FInputQ.SliceCount; i++)
                {
                    arrayQ[i] = new double[3];
                    arrayQ[i][0] = FInputQ[i].x - meanQ[0]; // subtract the mean values from the incoming pointset
                    arrayQ[i][1] = FInputQ[i].y - meanQ[1];
                    arrayQ[i][2] = FInputQ[i].z - meanQ[2];
                }
                // this is the matrix of the second pointset translated to the origin of the coordinate system
                GeneralMatrix Q = new GeneralMatrix(arrayQ);


                // ======================== STEP2 ========================
                // calculate a covariance matrix A and compute the optimal rotation matrix
                GeneralMatrix A = P.Transpose() * Q;

                SingularValueDecomposition svd = A.SVD();
                GeneralMatrix U = svd.GetU();
                GeneralMatrix V = svd.GetV();

                // calculate determinant for a special reflexion case.
                double det = (V * U.Transpose()).Determinant();
                double[][] arrayD = new double[3][]{ new double[]{1,0,0},
                                                    new double[] {0,1,0},
                                                    new double[] {0,0,1}
                };
                arrayD[2][2] = det < 0 ? -1 : 1; // multiply 3rd column with -1 if determinant is < 0
                GeneralMatrix D = new GeneralMatrix(arrayD);

                // now we can compute the rotation matrix:
                GeneralMatrix R = V * D * U.Transpose();

                // ======================== STEP3 ========================
                // calculate the translation:
                GeneralMatrix T = mCentroidP - R.Inverse() * mCentroidQ;

                // ================== OUTPUT TRANSFORM ===================

                mOut.m11 = (R.Array)[0][0];
                mOut.m12 = (R.Array)[0][1];
                mOut.m13 = (R.Array)[0][2];
                mOut.m14 = 0;
                mOut.m21 = (R.Array)[1][0];
                mOut.m22 = (R.Array)[1][1];
                mOut.m23 = (R.Array)[1][2];
                mOut.m24 = 0;
                mOut.m31 = (R.Array)[2][0];
                mOut.m32 = (R.Array)[2][1];
                mOut.m33 = (R.Array)[2][2];
                mOut.m34 = 0;
                mOut.m41 = (T.Array)[0][0];
                mOut.m42 = (T.Array)[1][0];
                mOut.m43 = (T.Array)[2][0];
                mOut.m44 = 1;
                FOutput[0] = mOut;
            }


            //FLogger.Log(LogType.Debug, T.Array[2][0].ToString());
        }
    }
}
