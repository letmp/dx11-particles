#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace DX11.Particles.IO
{
    public enum ScaleEnum
    {
        Multiply, MaxX, MaxY, MaxZ
    }
    public enum AlignEnum
    {
        None, Min, Center, Max
    }
    #region PluginInfo
    [PluginInfo(Name = "ChunkBuilder",
                Category = "DX11.Particles.IO",
                Version = "File",
                Help = "Parses a pointcloud dataset in ASCII format and creates chunkfiles",
                Tags = "ascii")]
    #endregion PluginInfo
    public class ChunkBuilderNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("File", IsSingle = true, StringType = StringType.Filename)]
        public ISpread<string> FFile;

        [Input("Output Directory", IsSingle = true, StringType = StringType.Directory)]
        public IDiffSpread<string> FDirectory;

        [Input("DataStructure", DefaultString = "xyzrgb", IsSingle = true)]
        public ISpread<string> FPattern;

        [Input("ChunkCount", MinValue = 1.0, DefaultValues = new double[] { 10.0, 10.0, 10.0 }, AsInt = true)]
        public ISpread<Vector3D> FChunkCount;

        [Input("NormalizeRGB", DefaultBoolean = true, IsSingle = true)]
        public ISpread<bool> FNormalizeRGB;

        [Input("Align X", DefaultEnumEntry = "None", Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        public ISpread<AlignEnum> FAlignX;

        [Input("Align Y", DefaultEnumEntry = "None", Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        public ISpread<AlignEnum> FAlignY;

        [Input("Align Z", DefaultEnumEntry = "None", Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        public ISpread<AlignEnum> FAlignZ;

        [Input("Scale Mode", DefaultEnumEntry = "Multiply", Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        public ISpread<ScaleEnum> FScaleMode;

        [Input("Scale Value", Visibility = PinVisibility.OnlyInspector, IsSingle = true, DefaultValue = 1.0)]
        public ISpread<double> FScaleValue;
        
        [Input("Create", IsSingle = true, IsBang = true)]
        public ISpread<bool> FRead;

        [Output("Success", IsBang = true)]
        public ISpread<bool> FSuccess;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        private ChunkDataManager cdm = new ChunkDataManager();
        private Dictionary<string, int> dataStructure = new Dictionary<string, int>();

        private StreamReader FReader;
        private List<StringBuilder> sbList = new List<StringBuilder>();
        private List<int> sbLineCount = new List<int>();

        private Vector3D min;
        private Vector3D max;

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FSuccess.SliceCount = 1;
            FSuccess[0] = false;

            if (FPattern.IsChanged)
            {
                cdm.SetDataStructure(FPattern[0]);
                this.dataStructure = cdm.dataStructure;
            }

            if (FRead[0])
            {

                sbList.Clear();
                sbLineCount.Clear();

                if (File.Exists(FFile[0]))
                {
                    FLogger.Log(LogType.Debug, "[ChunkConverter] start converting dataset");

                    this.FReader = new StreamReader(FFile[0]);

                    #region parse min and max coords
                    bool firstParse = true;
                    while (!this.FReader.EndOfStream)
                    {
                        string line = this.FReader.ReadLine();
                        Char delimiter = ' ';
                        String[] substrings = line.Split(delimiter);
                        double x = double.Parse(substrings[dataStructure["x"]], CultureInfo.InvariantCulture);
                        double y = double.Parse(substrings[dataStructure["y"]], CultureInfo.InvariantCulture);
                        double z = double.Parse(substrings[dataStructure["z"]], CultureInfo.InvariantCulture);

                        if (firstParse)
                        {
                            min = new Vector3D(x, y, z);
                            max = new Vector3D(x, y, z);
                        }
                        else
                        {

                            Vector3D newMinVec = min;
                            if (newMinVec.x > x) newMinVec.x = x;
                            if (newMinVec.y > y) newMinVec.y = y;
                            if (newMinVec.z > z) newMinVec.z = z;
                            min = newMinVec;

                            Vector3D newMaxVec = max;
                            if (newMaxVec.x < x) newMaxVec.x = x;
                            if (newMaxVec.y < y) newMaxVec.y = y;
                            if (newMaxVec.z < z) newMaxVec.z = z;
                            max = newMaxVec;
                        }
                        firstParse = false;
                    }
                    #endregion parse min and max coords

                    #region calculate aligning & scaling
                    double offsetX = 0;
                    if ((int)FAlignX[0] == 1) offsetX = 0 - min.x;
                    if ((int)FAlignX[0] == 2) offsetX = 0 - min.x - ((max.x - min.x) / 2);
                    if ((int)FAlignX[0] == 3) offsetX = 0 - max.x;
                    double offsetY = 0;
                    if ((int)FAlignY[0] == 1) offsetY = 0 - min.y;
                    if ((int)FAlignY[0] == 2) offsetY = 0 - min.y - ((max.y - min.y) / 2);
                    if ((int)FAlignY[0] == 3) offsetY = 0 - max.y;
                    double offsetZ = 0;
                    if ((int)FAlignZ[0] == 1) offsetZ = 0 - min.z;
                    if ((int)FAlignZ[0] == 2) offsetZ = 0 - min.z - ((max.z - min.z) / 2);
                    if ((int)FAlignZ[0] == 3) offsetZ = 0 - max.z;

                    double scalemultiplier = FScaleValue[0];
                    if ((int)FScaleMode[0] == 1) scalemultiplier = scalemultiplier / (max.x - min.x);
                    if ((int)FScaleMode[0] == 2) scalemultiplier = scalemultiplier / (max.y - min.y);
                    if ((int)FScaleMode[0] == 3) scalemultiplier = scalemultiplier / (max.z - min.z);
                    #endregion calculate aligning & scaling

                    #region calculate chunkinfos
                    Vector3D size = max - min;
                    double chunkSizeX = size.x / FChunkCount[0].x;
                    double chunkSizeY = size.y / FChunkCount[0].y;
                    double chunkSizeZ = size.z / FChunkCount[0].z;
                    int chunkCountX = Convert.ToInt32(FChunkCount[0].x);
                    int chunkCountY = Convert.ToInt32(FChunkCount[0].y);
                    int chunkCountZ = Convert.ToInt32(FChunkCount[0].z);
                    #endregion calculate chunkinfos

                    #region create stringbuilders
                    int fileCount = Convert.ToInt32(chunkCountX * chunkCountY * chunkCountZ);
                    for (int i = 0; i < fileCount; i++)
                    {
                        StringBuilder sb = new StringBuilder();
                        sbList.Add(sb);
                        sbLineCount.Add(0);
                    }
                    #endregion create stringbuilders

                    #region fill stringbuilders
                    this.FReader = new StreamReader(FFile[0]);
                    while (!this.FReader.EndOfStream)
                    {

                        // calculate chunk index in list
                        string line = this.FReader.ReadLine();
                        Char delimiter = ' ';
                        String[] substrings = line.Split(delimiter);
                        double x = double.Parse(substrings[dataStructure["x"]], CultureInfo.InvariantCulture) - min.x;
                        double y = double.Parse(substrings[dataStructure["y"]], CultureInfo.InvariantCulture) - min.y;
                        double z = double.Parse(substrings[dataStructure["z"]], CultureInfo.InvariantCulture) - min.z;
                        int chunkIdX = Convert.ToInt32(Math.Floor(x / chunkSizeX));
                        if (x == chunkSizeX * chunkCountX) chunkIdX -= 1;
                        int chunkIdY = Convert.ToInt32(Math.Floor(y / chunkSizeY));
                        if (y == chunkSizeY * chunkCountY) chunkIdY -= 1;
                        int chunkIdZ = Convert.ToInt32(Math.Floor(z / chunkSizeZ));
                        if (z == chunkSizeZ * chunkCountZ) chunkIdZ -= 1;

                        int sbListIndex = chunkIdX + chunkIdY * chunkCountX + chunkIdZ * chunkCountX * chunkCountY;

                        try
                        {
                            // build new string
                            string newLine = "";

                            if (dataStructure.ContainsKey("x"))
                            {
                                double value = double.Parse(substrings[dataStructure["x"]], CultureInfo.InvariantCulture);
                                value += offsetX;
                                value *= scalemultiplier;
                                newLine += value.ToString(new CultureInfo("en-US", false)) + " ";
                            } 
                            else newLine += " 0.0";

                            if (dataStructure.ContainsKey("y"))
                            {
                                double value = double.Parse(substrings[dataStructure["y"]], CultureInfo.InvariantCulture);
                                value += offsetY;
                                value *= scalemultiplier;
                                newLine += value.ToString(new CultureInfo("en-US", false)) + " ";
                            }
                            else newLine += " 0.0";

                            if (dataStructure.ContainsKey("z"))
                            {
                                double value = double.Parse(substrings[dataStructure["z"]], CultureInfo.InvariantCulture);
                                value += offsetZ;
                                value *= scalemultiplier;
                                newLine += value.ToString(new CultureInfo("en-US", false)) + " ";
                            }
                            else newLine += " 0.0";

                            if (dataStructure.ContainsKey("r"))
                            {
                                newLine += (FNormalizeRGB[0]) ? (double.Parse(substrings[dataStructure["r"]]) / 255).ToString(new CultureInfo("en-US", false)) : substrings[dataStructure["r"]];
                                newLine += " ";
                            }
                            if (dataStructure.ContainsKey("g"))
                            {
                                newLine += (FNormalizeRGB[0]) ? (double.Parse(substrings[dataStructure["g"]]) / 255).ToString(new CultureInfo("en-US", false)) : substrings[dataStructure["g"]];
                                newLine += " ";
                            }
                            if (dataStructure.ContainsKey("b"))
                            {
                                newLine += (FNormalizeRGB[0]) ? (double.Parse(substrings[dataStructure["b"]]) / 255).ToString(new CultureInfo("en-US", false)) : substrings[dataStructure["b"]];
                                newLine += " ";
                            }
                            if (dataStructure.ContainsKey("a"))
                            {
                                newLine += (FNormalizeRGB[0]) ? (double.Parse(substrings[dataStructure["a"]]) / 255).ToString(new CultureInfo("en-US", false)) : substrings[dataStructure["a"]];
                                newLine += " ";
                            }

                            sbList[sbListIndex].AppendLine(newLine);
                            sbLineCount[sbListIndex]++;
                        }
                        catch
                        {
                            FLogger.Log(LogType.Debug, "---- ERROR WRITING CHUNKDATA ----");
                            FLogger.Log(LogType.Debug, "chunkIdX: " + chunkIdX + " chunkIdY: " + chunkIdY + " chunkIdZ: " + chunkIdZ);
                            FLogger.Log(LogType.Debug, "sbListIndex: " + sbListIndex);
                        }
                    }
                    #endregion fill stringbuilders

                    #region write files
                    StringBuilder sbInfo = new StringBuilder();
                    sbInfo.AppendLine("Pattern: " + string.Join("", dataStructure.Keys));
                    sbInfo.AppendLine("ChunkSizeXYZ: " + chunkSizeX * scalemultiplier + " " + chunkSizeY * scalemultiplier + " " + chunkSizeZ * scalemultiplier);
                    sbInfo.AppendLine("ChunkCountXYZ: " + chunkCountX + " " + chunkCountY + " " + chunkCountZ);
                    sbInfo.AppendLine("BoundsMin: " + (min.x + offsetX) * scalemultiplier + " " + (min.y + offsetY) * scalemultiplier + " " + (min.z + offsetZ) * scalemultiplier + " ");
                    sbInfo.AppendLine("BoundsMax: " + (max.x + offsetX) * scalemultiplier + " " + (max.y + offsetY) * scalemultiplier + " " + (max.z + offsetZ) * scalemultiplier + " ");
                    sbInfo.AppendLine("ElementCount: " + sbLineCount.Sum());
                    sbInfo.AppendLine("MaxElementsPerChunk: " + sbLineCount.Max());

                    string filenameIn = Path.GetFileNameWithoutExtension(FFile[0]);
                    string dir = Path.Combine(FDirectory[0], filenameIn + "_" + chunkCountX + "_" + chunkCountY + "_" + chunkCountZ + "_" + string.Join("", dataStructure.Keys));
                    if (Directory.Exists(dir))
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        foreach (FileInfo file in di.GetFiles()) file.Delete();
                        foreach (DirectoryInfo subdir in di.GetDirectories()) subdir.Delete(true);
                    }
                    else
                    {
                        DirectoryInfo di = Directory.CreateDirectory(dir);
                    }

                    //calculate number of digits for padding in filename
                    int digits = Math.Max(chunkCountX.ToString().Length, chunkCountY.ToString().Length);
                    digits = Math.Max(digits, chunkCountZ.ToString().Length);

                    for (int x = 0; x < chunkCountX; x++)
                    {
                        for (int y = 0; y < chunkCountY; y++)
                        {
                            for (int z = 0; z < chunkCountZ; z++)
                            {

                                int sbListIndex = x + y * chunkCountX + z * chunkCountX * chunkCountY;
                                string filenameOut = x.ToString("D" + digits) + "_" + y.ToString("D" + digits) + "_" + z.ToString("D" + digits) + ".chunk";
                                string file = Path.Combine(dir, filenameOut);
                                StreamWriter sw = new StreamWriter(file, false);
                                StringBuilder sb = sbList[sbListIndex];
                                sw.WriteLine(sb.ToString());
                                sw.Close();
                                sbInfo.AppendLine(filenameOut + ": " + sbLineCount[sbListIndex]);
                            }
                        }
                    }

                    string infofile = Path.Combine(dir, "_chunkinfo");
                    StreamWriter swInfo = new StreamWriter(infofile, false);
                    swInfo.WriteLine(sbInfo.ToString());
                    swInfo.Close();
                    #endregion write files

                    FSuccess[0] = true;
                    FLogger.Log(LogType.Debug, "[ChunkConverter] finished");

                }
            }

        }

    }

    #region PluginInfo
    [PluginInfo(Name = "ChunkDataManager", Category = "DX11.Particles.IO", Version = "File", Help = "Loads chunks to the cache and provides the data for further usage", Tags = "")]
    #endregion PluginInfo
    public class ChunkDataManangerNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Chunk Directory", IsSingle = true, StringType = StringType.Directory)]
        public ISpread<string> FDirectory;

        [Input("Open", IsSingle = true, IsBang = true)]
        public ISpread<bool> FOpen;

        [Input("Precache", IsSingle = true, DefaultBoolean = true)]
        public IDiffSpread<bool> FPrecache;

        [Output("ChunkDataManager")]
        public ISpread<ChunkDataManager> FOutChunkData;

        [Output("Chunk Size")]
        public ISpread<Vector3D> FOutChunkSize;

        [Output("Chunk Count", AsInt = true)]
        public ISpread<Vector3D> FOutChunkCount;

        [Output("Bounds Min")]
        public ISpread<Vector3D> FOutBoundsMin;

        [Output("Bounds Max")]
        public ISpread<Vector3D> FOutBoundsMax;

        [Output("Element Count")]
        public ISpread<int> FOutElementCount;

        [Output("Max Elements per Chunk")]
        public ISpread<int> FOutMaxChunkEle;

        [Output("Caching Progress")]
        public ISpread<double> FOutCachingProgress;

        [Import()]
        public ILogger FLogger;

        private ChunkDataManager cdm = new ChunkDataManager();
        private Dictionary<string, int> chunkStructure = new Dictionary<string, int>();
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {

            FOutChunkData.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
            FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
            FOutElementCount.SliceCount = FOutMaxChunkEle.SliceCount =
            FOutCachingProgress.SliceCount = 0;

            FOutChunkData.Add(cdm);

            if (FOpen[0])
            {
                string filename = Path.Combine(FDirectory[0], "_chunkinfo");
                if (File.Exists(filename))
                {

                    StreamReader sr = new StreamReader(filename);
                    Char delimiter = ' ';

                    // init data structure
                    String[] substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetDataStructure(substrings[1]);

                    // init chunksize
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetChunkSize(substrings);

                    // init chunkcount
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetChunkCount(substrings);
                    
                    // init bounds
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetBounds(substrings, true);
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetBounds(substrings, false);

                    // init elementcount
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetElementCount(substrings);

                    // init maximum elements per chunk
                    substrings = sr.ReadLine().Split(delimiter);
                    cdm.SetMaxElementCount(substrings);

                    // unload previously loaded chunks
                    cdm.DisposeChunkDataList();

                    // init list of chunkdata
                    cdm.InitChunkDataList(FDirectory[0]);

                    // start precaching if necessary
                    if (FPrecache[0]) cdm.CacheAll();

                    cdm.fileOpened = true;
                }
                else
                {
                    cdm.fileOpened = false;
                }
            }

            if (cdm.fileOpened)
            {
                if (FPrecache.IsChanged && FPrecache[0])
                {
                    cdm.CacheAll();
                }

                FOutChunkSize.Add(cdm.chunkSize);
                FOutChunkCount.Add(new Vector3D(cdm.chunkCount[0], cdm.chunkCount[1], cdm.chunkCount[2]));
                FOutBoundsMin.Add(cdm.boundsMin);
                FOutBoundsMax.Add(cdm.boundsMax);
                FOutElementCount.Add(cdm.elementCount);
                FOutMaxChunkEle.Add(cdm.maxElementCount);
                FOutCachingProgress.Add(cdm.GetCachingProgress());
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "ChunkData", Category = "DX11.Particles.IO", Version = "Vector3D", Help = "Determines needed chunks by given positions and outputs the chunkdata", Tags = "")]
    #endregion PluginInfo
    public class ChunkStreamerVector3DNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("ChunkDataManager")]
        public Pin<ChunkDataManager> FChunkDataManager;

        [Input("Position")]
        public ISpread<Vector3D> FPosition;

        [Input("Data Limit", IsSingle = true, DefaultValue = -1, MinValue = -1)]
        public ISpread<int> FDataLimit;

        [Input("LockTime", IsSingle = true, DefaultValue = 0.0, MinValue = 0.0, AsInt = true)]
        public ISpread<double> FLockTime;

        [Input("Downsampling", IsSingle = true, DefaultValue = 1, MinValue = 1)]
        public ISpread<int> FDownsampling;

        [Output("Position")]
        public ISpread<Vector3D> FOutPosition;

        [Output("Colors")]
        public ISpread<RGBAColor> FOutColor;

        [Output("Loaded Elements")]
        public ISpread<int> FOutLoadedElements;

        [Output("Active Chunk Index")]
        public ISpread<int> FActiveChunkIndex;

        [Import()]
        public ILogger FLogger;

        List<ChunkAccessData> activeChunks = new List<ChunkAccessData>();
        List<ChunkAccessData> lockedChunks = new List<ChunkAccessData>();

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutPosition.SliceCount = FOutColor.SliceCount = FActiveChunkIndex.SliceCount = 0;
            FOutLoadedElements.SliceCount = 1;

            if (FChunkDataManager.IsConnected && FChunkDataManager[0] != null)
            {
                ChunkDataManager cdm = FChunkDataManager[0];
                 
                if (cdm.fileOpened)
                {
                    // get ids of chunks we want to display
                    var currentChunks = cdm.getChunkIds(FPosition.Select(pos => pos - cdm.boundsMin)).Distinct().ToList();
                    

                    // build lists of chunkIds we have to load/unload
                    var chunksToAdd = currentChunks.Except(activeChunks.Select(activeChunk => activeChunk.chunkId)).ToList();
                    chunksToAdd = chunksToAdd.Except(lockedChunks.Select(lockedChunk => lockedChunk.chunkId)).ToList(); // remove all chunks that are still locked
                    var chunksToRemove = activeChunks.Select(activeChunk => activeChunk.chunkId).Except(currentChunks).ToList();

                    // readd locked chunks that were not completely loaded
                    //var chunksToContinue = lockedChunks.Where(lockedChunk => currentChunks.Contains(lockedChunk.chunkId) && lockedChunk.dataOffset > 0 && lockedChunk.finishedLoading == false);
                    //lockedChunks.RemoveAll(lockedChunk => chunksToContinue.Any(chunkToContinue => chunkToContinue.chunkId == lockedChunk.chunkId));

                    // update list of locked chunks
                    var chunksToLock = activeChunks.Where(activeChunk => chunksToRemove.Contains(activeChunk.chunkId) && activeChunk.finishedLoading).ToList();
                    chunksToLock.ForEach(activeChunk => activeChunk.StartLock(FLockTime[0]));
                    lockedChunks.AddRange(chunksToLock);
                    lockedChunks.RemoveAll(lockedChunk => lockedChunk.isLocked == false);
                    
                    // update active chunks list
                    activeChunks.AddRange(chunksToAdd.Select(chunkId => new ChunkAccessData(chunkId)));
                    //activeChunks.AddRange(chunksToContinue);
                    activeChunks.RemoveAll(activeChunkId => chunksToRemove.Contains(activeChunkId.chunkId));
                    
                    // load caching data (if not already happened)
                    foreach (int chunkId in chunksToAdd) cdm.Cache(chunkId);
                    
                    // get all the data we need from active chunks
                    int limit = FDataLimit[0];
                    foreach (ChunkAccessData activeChunk in activeChunks)
                    {
                        ChunkData cd = cdm.GetChunkData(activeChunk.chunkId);
                        List<Tuple<Vector3D, RGBAColor>> chunkData = cd.GetChunkData(activeChunk.dataOffset, limit * FDownsampling[0], FDownsampling[0]);
                        activeChunk.dataOffset += chunkData.Count * FDownsampling[0];
                        if (activeChunk.dataOffset == cd.GetListSize()) activeChunk.finishedLoading = true;

                        FOutPosition.AddRange(chunkData.Select(tuple => tuple.Item1));
                        FOutColor.AddRange(chunkData.Select(tuple => tuple.Item2));

                        // stop outputting data if limit is set
                        if (limit != -1)
                        {
                            limit -= chunkData.Count;
                            if (limit <= 0) break;
                        }
                    }
                    FOutLoadedElements[0] = FOutPosition.SliceCount;
                    FActiveChunkIndex.AddRange(activeChunks.Select(activeChunk => activeChunk.chunkId));
                }
                
            }

        }
    }

    #region PluginInfo
    [PluginInfo(Name = "ChunkData", Category = "DX11.Particles.IO", Version = "ReadAll", Help = "Determines needed chunks by given positions and outputs the chunkdata", Tags = "")]
    #endregion PluginInfo
    public class ChunkStreamerCompleteNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("ChunkDataManager")]
        public Pin<ChunkDataManager> FChunkDataManager;

        [Input("ReadAll", IsSingle = true, IsBang = true)]
        public ISpread<bool> FReadAll;

        /*[Input("Position")]
        public ISpread<Vector3D> FPosition;*/
        [Input("Chunk Index")]
        public ISpread<int> FChunkIndex;

        [Input("Data Limit", IsSingle = true, DefaultValue = -1, MinValue = -1)]
        public ISpread<int> FDataLimit;

        [Input("LockTime", IsSingle = true, DefaultValue = 0.0, MinValue = 0.0, AsInt = true)]
        public ISpread<double> FLockTime;
        
        [Output("Position")]
        public ISpread<Vector3D> FOutPosition;

        [Output("Colors")]
        public ISpread<RGBAColor> FOutColor;

        [Output("ElementsToAdd Count")]
        public ISpread<int> FElementsToAddCount;

        [Output("Buffer Offset")]
        public ISpread<int> FBufferOffset;

        [Output("ChunkToAdd Index")]
        public ISpread<int> FChunkToAddIndex;

        [Output("Chunk BinSize")]
        public ISpread<int> FOutBinSize;

        [Output("Active Chunk Index")]
        public ISpread<int> FActiveChunkIndex;

        [Import()]
        public ILogger FLogger;

        List<ChunkAccessData> activeChunks = new List<ChunkAccessData>();
        List<ChunkAccessData> lockedChunks = new List<ChunkAccessData>();

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutPosition.SliceCount = FOutColor.SliceCount =
            FActiveChunkIndex.SliceCount = FChunkToAddIndex.SliceCount =
            FElementsToAddCount.SliceCount = FBufferOffset.SliceCount =0;

            if (FChunkDataManager.IsConnected && FChunkDataManager[0] != null)
            {
                ChunkDataManager cdm = FChunkDataManager[0];

                if (cdm.fileOpened)
                {

                    // this only works if progress of chunkdata caching is finished!
                    if (FReadAll[0])
                    {
                        FOutBinSize.SliceCount = 0;
                        foreach (ChunkData cd in cdm.GetChunkData())
                        {
                            List<Tuple<Vector3D, RGBAColor>> chunkData = cd.GetChunkData(0, -1);
                            FOutPosition.AddRange(chunkData.Select(tuple => tuple.Item1));
                            FOutColor.AddRange(chunkData.Select(tuple => tuple.Item2));
                            FOutBinSize.Add(cd.GetListSize());
                        }
                    }
                    
                    // get ids of chunks we want to display
                    //var currentChunks = cdm.getChunkIds(FPosition.Select(pos => pos - cdm.boundsMin)).Distinct().ToList();
                    var currentChunks = FChunkIndex;

                    // build lists of chunkIds we have to load/unload
                    var chunksToAdd = currentChunks.Except(activeChunks.Select(activeChunk => activeChunk.chunkId)).ToList();
                    chunksToAdd = chunksToAdd.Except(lockedChunks.Select(lockedChunk => lockedChunk.chunkId)).ToList(); // remove all chunks that are still locked
                    var chunksToRemove = activeChunks.Select(activeChunk => activeChunk.chunkId).Except(currentChunks).ToList();
                    
                    // update list of locked chunks
                    var chunksToLock = activeChunks.Where(activeChunk => chunksToRemove.Contains(activeChunk.chunkId) && activeChunk.finishedLoading).ToList();
                    chunksToLock.ForEach(activeChunk => activeChunk.StartLock(FLockTime[0]));
                    lockedChunks.AddRange(chunksToLock);
                    lockedChunks.RemoveAll(lockedChunk => lockedChunk.isLocked == false);

                    // update active chunks list
                    activeChunks.AddRange(chunksToAdd.Select(chunkId => new ChunkAccessData(chunkId)));
                    //activeChunks.AddRange(chunksToContinue);
                    activeChunks.RemoveAll(activeChunkId => chunksToRemove.Contains(activeChunkId.chunkId));

                    // load caching data (if not already happened)
                    foreach (int chunkId in chunksToAdd) cdm.Cache(chunkId);

                    // get all the data we need from active chunks
                    int limit = FDataLimit[0];
                    foreach (ChunkAccessData activeChunk in activeChunks)
                    {
                        if(activeChunk.finishedLoading == false)
                        {
                            FChunkToAddIndex.Add(activeChunk.chunkId);

                            ChunkData cd = cdm.GetChunkData(activeChunk.chunkId);
                            int range = cd.getRange(activeChunk.dataOffset, limit);
                            
                            FElementsToAddCount.Add(range);
                            FBufferOffset.Add(activeChunk.dataOffset);

                            activeChunk.dataOffset += range;
                            if (activeChunk.dataOffset == cd.GetListSize()) activeChunk.finishedLoading = true;
                            break;
                        }
                        
                    }
                    FActiveChunkIndex.AddRange(activeChunks.Select(activeChunk => activeChunk.chunkId));
                }

            }

        }
    }
}
