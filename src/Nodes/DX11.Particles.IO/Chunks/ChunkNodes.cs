#region usings
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using DX11.Particles.IO.Utils;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using VVVV.Utils.Streams;
#endregion usings

namespace DX11.Particles.IO.Chunks
{

    #region PluginInfo
    [PluginInfo(Name = "Importer",
                Category = "DX11.Particles.IO.Chunks",
                Version = "ASCII",
                Help = "Imports pointcloud datasets in ascii format",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkImporterASCIINode : IPluginEvaluate
    {
        #region fields & pins
        [Input("File", IsSingle = true, StringType = StringType.Filename)]
        public ISpread<string> FFile;

        [Input("Data Structure", DefaultString = "xyzrgb", IsSingle = true)]
        public ISpread<string> FDataStructure;

        [Input("Chunk Count", MinValue = 1.0, DefaultValues = new double[] { 10.0, 10.0, 10.0 }, AsInt = true)]
        public ISpread<Vector3D> FChunkCount;

        [Input("Skip Lines Count", DefaultValue = 0, MinValue = 0, Visibility = PinVisibility.OnlyInspector, IsSingle = true)]
        public ISpread<int> FSkipLines;

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

        [Input("Import", IsSingle = true, IsBang = true)]
        public ISpread<bool> FImport;

        [Output("ChunkManager")]
        public ISpread<ChunkManager> FOutChunkManager;

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

        [Output("Data Structure")]
        public ISpread<string> FOutDataStructure;

        [Output("Bytes Per Element")]
        public ISpread<int> FOutBytesPerElement;

        [Output("Message")]
        public ISpread<string> FOutMessage;

        [Output("Progress")]
        public ISpread<double> FOutProgress;

        [Import()]
        public ILogger FLogger;

        public IOMessages IOMessages = new IOMessages();

        ChunkManager _chunkManager = new ChunkManager();
        ChunkImporterASCII _chunkImporterASCII;

        bool imported = false;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FOutProgress.SliceCount = FOutMessage.SliceCount = 1;
            FOutMessage[0] = IOMessages.CurrentState;

            if (imported == false)
            {
                FOutChunkManager.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
                FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
                FOutElementCount.SliceCount = FOutDataStructure.SliceCount = FOutBytesPerElement.SliceCount = 0;
            }

            if (FImport[0])
            {
                imported = false;

                _chunkManager = new ChunkManager();
                _chunkImporterASCII = new ChunkImporterASCII(_chunkManager);
                _chunkManager.ChunkImporter = _chunkImporterASCII;

                _chunkImporterASCII.FLogger = FLogger;
                _chunkImporterASCII.IOMessages = IOMessages;

                _chunkImporterASCII.FilePath = FFile[0];

                _chunkImporterASCII.skipLines = FSkipLines[0];

                _chunkImporterASCII.AlignModeXYZ.x = (int)FAlignX[0];
                _chunkImporterASCII.AlignModeXYZ.y = (int)FAlignY[0];
                _chunkImporterASCII.AlignModeXYZ.z = (int)FAlignZ[0];

                _chunkImporterASCII.ScaleMode = (int)FScaleMode[0];
                _chunkImporterASCII.ScaleValue = FScaleValue[0];

                _chunkImporterASCII.ChunkCount.x = (int)FChunkCount[0].x;
                _chunkImporterASCII.ChunkCount.y = (int)FChunkCount[0].y;
                _chunkImporterASCII.ChunkCount.z = (int)FChunkCount[0].z;

                _chunkImporterASCII.SetDataStructure(FDataStructure[0]);

                _chunkImporterASCII.Import();
            }

            if (_chunkImporterASCII != null)
            {
                var progress = _chunkImporterASCII.Progress;
                FOutProgress[0] = progress;
                if (progress == 1) imported = true;
            }

            if (imported && FOutChunkManager.SliceCount == 0)
            {
                FOutChunkManager.Add(_chunkManager);
                FOutChunkSize.Add(_chunkManager.ChunkSize);
                FOutChunkCount.Add(new Vector3D((double)_chunkManager.ChunkCount.x, (double)_chunkManager.ChunkCount.y, (double)_chunkManager.ChunkCount.z));

                FOutBoundsMin.Add(_chunkManager.BoundsMin);
                FOutBoundsMax.Add(_chunkManager.BoundsMax);
                FOutElementCount.Add(_chunkManager.ElementCount);

                FOutDataStructure.Add(_chunkManager.DataStructure);
                FOutBytesPerElement.Add(_chunkManager.BytesPerElement);
            }
            
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Importer",
                Category = "DX11.Particles.IO.Chunks",
                Version = "OBJ",
                Help = "Imports pointcloud datasets in OBJ format",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkImporterOBJNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("File", IsSingle = true, StringType = StringType.Filename)]
        public ISpread<string> FFile;

        [Input("Data Structure", DefaultString = "xyz", IsSingle = true)]
        public ISpread<string> FDataStructure;

        [Input("Chunk Count", MinValue = 1.0, DefaultValues = new double[] { 10.0, 10.0, 10.0 }, AsInt = true)]
        public ISpread<Vector3D> FChunkCount;
        
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

        [Input("Import", IsSingle = true, IsBang = true)]
        public ISpread<bool> FImport;

        [Output("ChunkManager")]
        public ISpread<ChunkManager> FOutChunkManager;

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

        [Output("Data Structure")]
        public ISpread<string> FOutDataStructure;

        [Output("Bytes Per Element")]
        public ISpread<int> FOutBytesPerElement;

        [Output("Message")]
        public ISpread<string> FOutMessage;

        [Output("Progress")]
        public ISpread<double> FOutProgress;

        [Import()]
        public ILogger FLogger;

        public IOMessages IOMessages = new IOMessages();

        ChunkManager _chunkManager = new ChunkManager();
        ChunkImporterOBJ _chunkImporterOBJ;

        bool imported = false;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FOutProgress.SliceCount = FOutMessage.SliceCount = 1;
            FOutMessage[0] = IOMessages.CurrentState;

            if (imported == false)
            {
                FOutChunkManager.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
                FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
                FOutElementCount.SliceCount = FOutDataStructure.SliceCount = FOutBytesPerElement.SliceCount = 0;
            }

            if (FImport[0])
            {
                imported = false;

                _chunkManager = new ChunkManager();
                _chunkImporterOBJ = new ChunkImporterOBJ(_chunkManager);
                _chunkManager.ChunkImporter = _chunkImporterOBJ;

                _chunkImporterOBJ.FLogger = FLogger;
                _chunkImporterOBJ.IOMessages = IOMessages;

                _chunkImporterOBJ.FilePath = FFile[0];
                
                _chunkImporterOBJ.AlignModeXYZ.x = (int)FAlignX[0];
                _chunkImporterOBJ.AlignModeXYZ.y = (int)FAlignY[0];
                _chunkImporterOBJ.AlignModeXYZ.z = (int)FAlignZ[0];

                _chunkImporterOBJ.ScaleMode = (int)FScaleMode[0];
                _chunkImporterOBJ.ScaleValue = FScaleValue[0];

                _chunkImporterOBJ.ChunkCount.x = (int)FChunkCount[0].x;
                _chunkImporterOBJ.ChunkCount.y = (int)FChunkCount[0].y;
                _chunkImporterOBJ.ChunkCount.z = (int)FChunkCount[0].z;

                _chunkImporterOBJ.SetDataStructure(FDataStructure[0]);

                _chunkImporterOBJ.Import();
            }

            if (_chunkImporterOBJ != null)
            {
                var progress = _chunkImporterOBJ.Progress;
                FOutProgress[0] = progress;
                if (progress == 1) imported = true;
            }

            if (imported && FOutChunkManager.SliceCount == 0)
            {
                FOutChunkManager.Add(_chunkManager);
                FOutChunkSize.Add(_chunkManager.ChunkSize);
                FOutChunkCount.Add(new Vector3D((double)_chunkManager.ChunkCount.x, (double)_chunkManager.ChunkCount.y, (double)_chunkManager.ChunkCount.z));

                FOutBoundsMin.Add(_chunkManager.BoundsMin);
                FOutBoundsMax.Add(_chunkManager.BoundsMax);
                FOutElementCount.Add(_chunkManager.ElementCount);

                FOutDataStructure.Add(_chunkManager.DataStructure);
                FOutBytesPerElement.Add(_chunkManager.BytesPerElement);
            }

        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Importer",
                Category = "DX11.Particles.IO.Chunks",
                Version = "PLY",
                Help = "Imports pointcloud datasets in PLY format",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkImporterPLYNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("File", IsSingle = true, StringType = StringType.Filename)]
        public ISpread<string> FFile;
        
        [Input("Chunk Count", MinValue = 1.0, DefaultValues = new double[] { 10.0, 10.0, 10.0 }, AsInt = true)]
        public ISpread<Vector3D> FChunkCount;
        
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

        [Input("Import", IsSingle = true, IsBang = true)]
        public ISpread<bool> FImport;

        [Output("ChunkManager")]
        public ISpread<ChunkManager> FOutChunkManager;

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

        [Output("Data Structure")]
        public ISpread<string> FOutDataStructure;

        [Output("Bytes Per Element")]
        public ISpread<int> FOutBytesPerElement;

        [Output("Message")]
        public ISpread<string> FOutMessage;

        [Output("Progress")]
        public ISpread<double> FOutProgress;

        [Import()]
        public ILogger FLogger;

        public IOMessages IOMessages = new IOMessages();

        ChunkManager _chunkManager = new ChunkManager();
        ChunkImporterPLY _chunkImporterPLY;

        bool imported = false;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FOutProgress.SliceCount = FOutMessage.SliceCount = 1;
            FOutMessage[0] = IOMessages.CurrentState;

            if (imported == false)
            {
                FOutChunkManager.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
                FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
                FOutElementCount.SliceCount = FOutDataStructure.SliceCount = FOutBytesPerElement.SliceCount = 0;
            }

            if (FImport[0])
            {
                imported = false;

                _chunkManager = new ChunkManager();
                _chunkImporterPLY = new ChunkImporterPLY(_chunkManager);
                _chunkManager.ChunkImporter = _chunkImporterPLY;

                _chunkImporterPLY.FLogger = FLogger;
                _chunkImporterPLY.IOMessages = IOMessages;

                _chunkImporterPLY.FilePath = FFile[0];

                _chunkImporterPLY.AlignModeXYZ.x = (int)FAlignX[0];
                _chunkImporterPLY.AlignModeXYZ.y = (int)FAlignY[0];
                _chunkImporterPLY.AlignModeXYZ.z = (int)FAlignZ[0];

                _chunkImporterPLY.ScaleMode = (int)FScaleMode[0];
                _chunkImporterPLY.ScaleValue = FScaleValue[0];

                _chunkImporterPLY.ChunkCount.x = (int)FChunkCount[0].x;
                _chunkImporterPLY.ChunkCount.y = (int)FChunkCount[0].y;
                _chunkImporterPLY.ChunkCount.z = (int)FChunkCount[0].z;

                _chunkImporterPLY.Import();
            }

            if (_chunkImporterPLY != null)
            {
                var progress = _chunkImporterPLY.Progress;
                FOutProgress[0] = progress;
                if (progress == 1) imported = true;
            }

            if (imported && FOutChunkManager.SliceCount == 0)
            {
                FOutChunkManager.Add(_chunkManager);
                FOutChunkSize.Add(_chunkManager.ChunkSize);
                FOutChunkCount.Add(new Vector3D((double)_chunkManager.ChunkCount.x, (double)_chunkManager.ChunkCount.y, (double)_chunkManager.ChunkCount.z));

                FOutBoundsMin.Add(_chunkManager.BoundsMin);
                FOutBoundsMax.Add(_chunkManager.BoundsMax);
                FOutElementCount.Add(_chunkManager.ElementCount);

                FOutDataStructure.Add(_chunkManager.DataStructure);
                FOutBytesPerElement.Add(_chunkManager.BytesPerElement);
            }

        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Reader",
                Category = "DX11.Particles.IO.Chunks",
                Version = "Binary",
                Help = "Opens a directory with binary chunk files for further streaming",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkReaderBinaryNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Chunk Directory", IsSingle = true, StringType = StringType.Directory)]
        public ISpread<string> FDirectory;

        [Input("Open", IsSingle = true, IsBang = true)]
        public ISpread<bool> FOpen;

        [Input("Precache", IsSingle = true, DefaultBoolean = true)]
        public IDiffSpread<bool> FPrecache;

        [Output("ChunkManager")]
        public ISpread<ChunkManager> FOutChunkManager;

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

        [Output("Data Structure")]
        public ISpread<string> FOutDataStructure;

        [Output("Bytes Per Element")]
        public ISpread<int> FOutBytesPerElement;

        [Output("Message")]
        public ISpread<string> FOutMessage;

        [Output("Cache Progress")]
        public ISpread<double> FOutProgress;

        [Import()]
        public ILogger FLogger;

        public IOMessages IOMessages = new IOMessages();

        private ChunkManager _chunkManager = new ChunkManager();
        private ChunkReaderBinary _chunkReaderBinary;
        private bool opened = false;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutProgress.SliceCount = FOutMessage.SliceCount = 1;

            FOutMessage[0] = IOMessages.CurrentState;

            if (FOpen[0] && opened == true) opened = false;

            if (opened == false)
            {
                FOutChunkManager.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
                FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
                FOutElementCount.SliceCount = FOutDataStructure.SliceCount = FOutBytesPerElement.SliceCount = 0;
            }
            
            if (FOpen[0])
            {
                opened = true;
                FOutChunkManager.SliceCount = 0;

                _chunkManager = new ChunkManager();
                _chunkReaderBinary = new ChunkReaderBinary(_chunkManager);
                _chunkManager.ChunkReader = _chunkReaderBinary;

                _chunkReaderBinary.FLogger = FLogger;
                _chunkReaderBinary.IOMessages = IOMessages;

                _chunkReaderBinary.Directory = FDirectory[0];
                _chunkReaderBinary.InitChunks();

                if (FPrecache[0]) _chunkReaderBinary.ReadAll();
            }

            if (_chunkReaderBinary != null)
            {
                if (FPrecache.IsChanged && FPrecache[0]) _chunkReaderBinary.ReadAll();
                FOutProgress[0] = _chunkReaderBinary.Progress;
            }

            if (opened && FOutChunkManager.SliceCount == 0)
            {
                FOutChunkManager.Add(_chunkManager);
                FOutChunkSize.Add(_chunkManager.ChunkSize);
                FOutChunkCount.Add(new Vector3D((double)_chunkManager.ChunkCount.x, (double)_chunkManager.ChunkCount.y, (double)_chunkManager.ChunkCount.z));

                FOutBoundsMin.Add(_chunkManager.BoundsMin);
                FOutBoundsMax.Add(_chunkManager.BoundsMax);
                FOutElementCount.Add(_chunkManager.ElementCount);

                FOutDataStructure.Add(_chunkManager.DataStructure);
                FOutBytesPerElement.Add(_chunkManager.BytesPerElement);
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Writer",
                Category = "DX11.Particles.IO.Chunks",
                Version = "Binary",
                Help = "Creates a directory and writes chunk files in binary format",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkWriterBinaryNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("ChunkManager", IsSingle = true)]
        public Pin<ChunkManager> FChunkManager;

        [Input("Chunk Directory", IsSingle = true, StringType = StringType.Directory)]
        public ISpread<string> FDirectory;

        [Input("Write", IsSingle = true, IsBang = true)]
        public ISpread<bool> FWrite;

        [Output("Message")]
        public ISpread<string> FOutMessage;

        [Output("Progress")]
        public ISpread<double> FOutProgress;

        [Import()]
        public ILogger FLogger;

        public IOMessages IOMessages = new IOMessages();

        private ChunkWriterBinary _chunkWriterBinary;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutProgress.SliceCount = FOutMessage.SliceCount = 1;

            FOutMessage[0] = IOMessages.CurrentState;

            if (FWrite[0] && FChunkManager.IsConnected && FChunkManager.SliceCount > 0 && FChunkManager != null)
            {
                _chunkWriterBinary = new ChunkWriterBinary(FChunkManager[0]);
                FChunkManager[0].ChunkWriter = _chunkWriterBinary;

                _chunkWriterBinary.FLogger = FLogger;
                _chunkWriterBinary.IOMessages = IOMessages;

                _chunkWriterBinary.Directory = FDirectory[0];
                _chunkWriterBinary.CreateDirectory();
                _chunkWriterBinary.WriteAll();
                _chunkWriterBinary.WriteProjectInfo();
            }
            if (_chunkWriterBinary != null)
            {
                FOutProgress[0] = _chunkWriterBinary.Progress;
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Stream",
                Category = "DX11.Particles.IO.Chunks",
                Version = "Binary",
                Help = "Streams chunks in Raw format",
                Author = "tmp")]
    #endregion PluginInfo
    public class ChunkStreamerBinaryNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("ChunkManager")]
        public Pin<ChunkManager> FChunkManager;

        [Input("Index", DefaultValue = 0.0, MinValue = 0.0, AsInt = true)]
        public IDiffSpread<int> FIndex;

        [Input("LockTime", DefaultValue = 0.0, MinValue = 0.0, AsInt = true)]
        public ISpread<double> FLockTime;

        [Input("Downsampling", DefaultValue = 1, MinValue = 1)]
        public IDiffSpread<int> FDownsampling;

        [Input("Downsampling Offset", DefaultValue = 0, MinValue = 0)]
        public IDiffSpread<int> FDownsamplingOffset;

        [Input("Element Limit Per Chunk", DefaultValue = -1, MinValue = -1)]
        public ISpread<int> FLimit;

        [Output("Raw Data")]
        public ISpread<Stream> FOutMemoryStream;

        [Output("Element Count")]
        public ISpread<int> FOutElementCount;
        
        [Output("Active Chunk Index")]
        public ISpread<int> FOutActiveChunkIndex;

        [Output("On Data")]
        public ISpread<bool> FOutOnData;

        [Import()]
        public ILogger FLogger;

        List<ChunkAccessData> ActiveChunks = new List<ChunkAccessData>();
        List<ChunkAccessData> LockedChunks = new List<ChunkAccessData>();

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutOnData.SliceCount = FOutElementCount.SliceCount = 1;
            FOutMemoryStream.SliceCount =  FOutActiveChunkIndex.SliceCount = 0;

            if (FChunkManager.IsConnected && FChunkManager.SliceCount > 0 && FChunkManager[0] != null)
            {
                ChunkManager chunkManager = FChunkManager[0];
                
                // create chunkaccessdata list for all requested chunks
                if (FIndex.IsChanged || FDownsampling.IsChanged || FDownsamplingOffset.IsChanged)
                {
                    IEnumerable<int> currentChunks = FIndex;
                    List<int> currentChunkList = FIndex.ToList();
                    List<ChunkAccessData> chunksToAdd = new List<ChunkAccessData>();
                    for (int i = 0; i < FIndex.SliceCount; i++)
                    {
                        int streamLimit = FLimit[i];
                        int streamDownsampling = FDownsampling[i];
                        int streamDownsamplingOffset = FDownsamplingOffset[i];
                        double streamLocktime = FLockTime[i];
                        chunksToAdd.Add(new ChunkAccessData(FIndex[i], streamLimit, streamDownsampling, streamDownsamplingOffset, streamLocktime));
                    }

                    // now remove chunks that are already active (with the same LOD)
                    chunksToAdd.RemoveAll(chunkToAdd => ActiveChunks.Exists(activeChunk =>  chunkToAdd.chunkId == activeChunk.chunkId &&
                                                                                            chunkToAdd.streamDownsampling == activeChunk.streamDownsampling &&
                                                                                            chunkToAdd.streamDownsamplingOffset == activeChunk.streamDownsamplingOffset
                                                                                            ));
                    // remove chunks that are locked
                    chunksToAdd.RemoveAll(chunkToAdd => LockedChunks.Exists(lockedChunk => chunkToAdd.chunkId == lockedChunk.chunkId));

                    // create list of chunks wo dont need anymore
                    var chunksToRemove = ActiveChunks.Select(activeChunk => activeChunk.chunkId).Except(currentChunks).ToList();

                    // update list of locked chunks
                    var chunksToLock = ActiveChunks.Where(activeChunk => chunksToRemove.Contains(activeChunk.chunkId) && activeChunk.streamFinished).ToList();
                    chunksToLock.ForEach(activeChunk => activeChunk.StartLock());
                    LockedChunks.AddRange(chunksToLock);
                    LockedChunks.RemoveAll(lockedChunk => lockedChunk.isLocked == false);

                    // finally add remaining chunkaccessdata items to active list ..
                    ActiveChunks.AddRange(chunksToAdd);
                    // and remove chunkaccessdata items we dont need anymore
                    ActiveChunks.RemoveAll(activeChunk => chunksToRemove.Contains(activeChunk.chunkId));

                    // start reading process for new chunks (if not already happened)
                    if (chunkManager.ChunkReader != null)
                    {
                        foreach (ChunkAccessData chunkAccessData in chunksToAdd)
                        {
                            Chunk chunk = chunkManager.GetChunk(chunkAccessData.chunkId);
                            chunkManager.ChunkReader.Read(chunk);
                        }
                    }
                }
                
                // add chunkdata streams to a list
                FOutElementCount[0] = 0;
                List<Stream> streams = new List<Stream>();
                foreach (ChunkAccessData activeChunk in ActiveChunks)
                {
                    Chunk chunk = chunkManager.GetChunk(activeChunk.chunkId);

                    if (!activeChunk.streamFinished && chunk.finishedLoading)
                    {
                        Stream stream = chunk.MemoryStream;
                        
                        // logic for limited stream output
                        if (activeChunk.streamLimit > -1)
                        {
                            int bytesToRead = chunkManager.BytesPerElement * activeChunk.streamLimit;
                            stream = new SkipStream(stream, activeChunk.streamPosition);
                            stream = new TakeStream(stream, bytesToRead * activeChunk.streamDownsampling);
                        }

                        // logic for picking every nth element
                        if(activeChunk.streamDownsampling > 1)
                            stream = new ModuloStream(stream, chunkManager.BytesPerElement, activeChunk.streamDownsampling, activeChunk.streamDownsamplingOffset);
                        
                        // calculate number of appended elements
                        int bytesRead = (int) stream.Length;
                        FOutElementCount[0] += (bytesRead / chunkManager.BytesPerElement);

                        // update streamposition and set flag if position is at end
                        activeChunk.streamPosition += bytesRead * activeChunk.streamDownsampling;
                        if (activeChunk.streamPosition >= chunk.MemoryStream.Length) activeChunk.streamFinished = true;

                        // add stream to list
                        streams.Add(stream);
                    }
                }

                // aggregate streams if needed
                if (streams.Count > 1) FOutMemoryStream.Add(new AggregatedStream(streams));
                else if (streams.Count == 1) FOutMemoryStream.Add(streams[0]);
                
                // bang on data
                FOutOnData[0] = (FOutElementCount[0] > 0) ? true : false;
                
                // output active chunk ids
                FOutActiveChunkIndex.AddRange(ActiveChunks.Select(activeChunk => activeChunk.chunkId));

            }
            else FOutMemoryStream.Add(new MemoryComStream(new byte[sizeof(Single)]));  // workaround to prevent DynamicBufer (raw) from throwing exceptions
        }

    }
}
