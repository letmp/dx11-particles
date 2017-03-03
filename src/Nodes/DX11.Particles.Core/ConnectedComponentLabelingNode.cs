#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;

using FeralTic.DX11;
using FeralTic.DX11.Resources;

using SlimDX;
using VVVV.DX11;
#endregion usings

namespace DX11.Particles.Core
{
    #region PluginInfo
    [PluginInfo(Name = "CCL", Category = "DX11.Texture", Author = "tmp", Help = "Tries to find connected pixels in a binary image in R8G8B8A8 format and outputs a label for each pixel", Tags = "")]
    #endregion PluginInfo
    public class ConnectedComponentLabelingNode : IPluginEvaluate, IDX11ResourceDataRetriever
    {
        #region fields & pins
        [Input("Texture In")]
        protected Pin<DX11Resource<DX11Texture2D>> FTextureIn;

        [Input("Enabled", IsSingle = true)]
        public ISpread<bool> FEnabled;

        [Output("Output")]
        public ISpread<int> FOutput;

        [Output("Labels")]
        public ISpread<int> FLabels;

        [Output("Group Count", IsSingle = true)]
        public ISpread<int> FComponentsCount;

        [Output("Bin Sizes")]
        public ISpread<int> FComponentsBins;

        [Output("Centroids")]
        public ISpread<int> FCentroids;

        [Import()]
        public ILogger FLogger;
        [Import()]
        protected IPluginHost FHost;


        // private

        private int[,] arr;
        private int cols, rows;

        private static int[,] SearchDirection = { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 } };

        private Dictionary<int, int> objectCounts = new Dictionary<int, int>();

        private Dictionary<int, int> xMin = new Dictionary<int, int>();
        private Dictionary<int, int> xMax = new Dictionary<int, int>();
        private Dictionary<int, int> yMin = new Dictionary<int, int>();
        private Dictionary<int, int> yMax = new Dictionary<int, int>();

        public DX11RenderContext AssignedContext
        {
            get;
            set;
        }
        public event DX11RenderRequestDelegate RenderRequest;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {

            if (this.FTextureIn.PluginIO.IsConnected && FEnabled[0] == true)
            {

                if (this.RenderRequest != null) { this.RenderRequest(this, this.FHost); }
                if (this.AssignedContext == null) { this.SetNull(); return; }
                DX11RenderContext context = this.AssignedContext;

                try
                {
                    if (this.FTextureIn[0].Contains(context))
                    {

                        DX11StagingTexture2D sTex = new DX11StagingTexture2D(context, this.FTextureIn[0][context].Width, this.FTextureIn[0][context].Height, this.FTextureIn[0][context].Format);
                        sTex.CopyFrom(FTextureIn[0][context]);

                        DataBox db = sTex.LockForRead();
                        FOutput.SliceCount = Convert.ToInt32(sTex.Width * sTex.Height);

                        // our array we want to fill with texture data
                        arr = new int[sTex.Height, sTex.Width];

                        // we need these pitch values to correctly fill our array
                        int pitchedWidth = db.RowPitch / 4;
                        int pitchDiff = pitchedWidth - sTex.Width;

                        // go through the texture data
                        while (db.Data.Position < db.Data.Length)
                        {

                            int arrPos = Convert.ToInt32(db.Data.Position) / 4; // this is the position in our array
                            int intCol = db.Data.Read<int>(); // read the color

                            // the texture data comes from a pitched resource! so we have to be aware of longer row lengths!
                            int y = arrPos / pitchedWidth; // this is the actual rowNumber in our pitched texture
                                                           // now check if the current arrayPosition lies inside the actual texture width
                            if (arrPos < y * pitchedWidth + sTex.Width)
                            {
                                arrPos = arrPos - y * pitchDiff; // we have to subtract rowNumber * pitchDiff to compensate the pitch
                                arr[(arrPos / sTex.Width) % sTex.Height, arrPos % sTex.Width] = intCol; // now we add the packed integer color
                            }
                        }

                        cols = sTex.Width;
                        rows = sTex.Height;

                        for (int j = 0; j < rows; j++)
                        {
                            for (int i = 0; i < cols; i++)
                            {
                                FOutput[cols * j + i] = arr[j, i];
                            }
                        }

                        // unlock and dispose the resource
                        sTex.UnLock();
                        sTex.Dispose();


                        // INIT LABELS
                        FLabels.SliceCount = Convert.ToInt32(sTex.Width * sTex.Height);
                        for (int i = 0; i < FLabels.SliceCount; i++)
                        {
                            FLabels[i] = 0;
                        }

                        // INIT COMPONENT COUNTER
                        this.FComponentsCount.SliceCount = 1;
                        this.objectCounts.Clear();

                        // DO THE COMPONENT LABELING
                        ConnectedComponentLabeling();

                        // OUTPUT THE LABELS
                        for (int i = 0; i < FLabels.SliceCount; i++)
                        {

                            if (FLabels[i] == -1) FLabels[i] = 0;
                        }

                        // SET BIN SIZES 
                        this.FComponentsBins.SliceCount = this.objectCounts.Count;
                        this.FCentroids.SliceCount = this.objectCounts.Count * 2;

                        // fill binsize and centroid spreads
                        for (int i = 0; i < this.objectCounts.Count; i++)
                        {
                            // binsize
                            FComponentsBins[i] = this.objectCounts[i + 1];
                            // centroid XY
                            FCentroids[i * 2] = (this.xMin[i + 1] + this.xMax[i + 1]) / 2;
                            FCentroids[i * 2 + 1] = (this.yMin[i + 1] + this.yMax[i + 1]) / 2;
                        }

                    }
                }
                catch (Exception ex)
                {
                    FLogger.Log(LogType.Error, ex.Message);
                }
            }
        }

        private void SetNull()
        {
            this.FOutput.SliceCount = 0;
            this.FLabels.SliceCount = 0;
            this.FComponentsCount.SliceCount = 0;
            this.FComponentsBins.SliceCount = 0;
        }

        private void Tracer(ref int cy, ref int cx, ref int tracingdirection)
        {
            int x, y;
            int nbr_count = SearchDirection.GetLength(0);

            for (int i = 0; i < nbr_count; i++)
            {
                y = cy + SearchDirection[tracingdirection, 0];
                x = cx + SearchDirection[tracingdirection, 1];

                if (x >= 0 && x < cols && y >= 0 && y < rows)
                {
                    //FLogger.Log(LogType.Message, "tracing dir: " + tracingdirection + " x: " + SearchDirection[tracingdirection, 1] + " y: " + SearchDirection[tracingdirection, 0]);	            	            	

                    // background?
                    if (arr[y, x] == 0)
                    {
                        // mark sorrounding pixels as 'checked'
                        FLabels[cols * y + x] = -1;
                        tracingdirection = (tracingdirection + 1) % nbr_count;
                        // foreground?
                    }
                    else
                    {
                        // take coordinates as next contour point and return
                        cy = y;
                        cx = x;
                        break;
                    }
                }
                else
                {
                    tracingdirection = (tracingdirection + 1) % nbr_count;
                }
            }
        }

        private void ContourTracing(int cy, int cx, int labelindex, int tracingdirection)
        {
            //FLogger.Log(LogType.Message, "Tracing contour");        	
            bool tracingstopflag = false;
            bool SearchAgain = true;
            int fx, fy;
            int start_x = cx;
            int start_y = cy;
            //int count = 0;

            // determine next contour point
            Tracer(ref cy, ref cx, ref tracingdirection);

            if (cx != start_x || cy != start_y) // contour has more than 1 point!
            {
                fx = cx; // save second point x
                fy = cy; // save second point y

                while (SearchAgain)
                {
                    //count++;                	
                    tracingdirection = (tracingdirection + 6) % SearchDirection.GetLength(0);
                    FLabels[cols * cy + cx] = labelindex;

                    if (cx < xMin[labelindex]) xMin[labelindex] = cx;
                    if (cx > xMax[labelindex]) xMax[labelindex] = cx;
                    if (cy < yMin[labelindex]) yMin[labelindex] = cy;
                    if (cy > yMax[labelindex]) yMax[labelindex] = cy;

                    if (!tracingstopflag)
                        this.objectCounts[labelindex]++;

                    // determine next contour point                			
                    Tracer(ref cy, ref cx, ref tracingdirection);

                    if (cx == start_x && cy == start_y)
                    {
                        tracingstopflag = true;
                    }
                    else if (tracingstopflag)
                    {
                        if (cx == fx && cy == fy)
                        {
                            // Contour end
                            SearchAgain = false;
                        }
                        else
                        {
                            tracingstopflag = false;
                        }
                    }
                }
                //FLogger.Log(LogType.Message, "#pxs in contour: " + count);
            }
        }

        private void ConnectedComponentLabeling()
        {
            int ConnectedComponentsCount = 0;
            int labelindex = 0;

            int cx, cy;
            for (cy = 0; cy < rows; cy++)
            {
                for (cx = 0, labelindex = 0; cx < cols; cx++)
                {
                    // FLogger.Log(LogType.Debug, "--> x = " + cx + " y = " + cy);
                    if (arr[cy, cx] != 0) // foreground pixel?
                    {
                        // pre-pixel already labeled? 
                        if (labelindex != 0 && FLabels[cols * cy + cx] == 0)
                        {

                            // use label
                            FLabels[cols * cy + cx] = labelindex;
                            this.objectCounts[labelindex]++;

                        }
                        else
                        {

                            // assign label if already labeled in some tracing step before
                            labelindex = FLabels[cols * cy + cx];

                            // not previously labeled
                            if (labelindex == 0)
                            {

                                labelindex = ++ConnectedComponentsCount;
                                this.objectCounts[labelindex] = 0;

                                // init x & y min/max dictionaries
                                xMin[labelindex] = cx;
                                xMax[labelindex] = cx;
                                yMin[labelindex] = cy;
                                yMax[labelindex] = cy;

                                ContourTracing(cy, cx, labelindex, 0); // external contour

                                FLabels[cols * cy + cx] = labelindex;
                                this.objectCounts[labelindex]++;
                            }
                        }

                        // background pixel & pre-pixel has been labeled?
                    }
                    else if (labelindex != 0)
                    {
                        if (FLabels[cols * cy + cx] == 0)
                        {
                            ContourTracing(cy, cx - 1, labelindex, 1); // internal contour
                        }
                        labelindex = 0;
                    }
                }
            }

            this.FComponentsCount[0] = ConnectedComponentsCount;
        }
    }

}


