#region usings
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using DX11.Particles.IO.Utils;
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
    public class ChunkImporterNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("File", IsSingle = true, StringType = StringType.Filename)]
        public ISpread<string> FFile;

        [Input("Data Structure", DefaultString = "xyzrgb", IsSingle = true)]
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
        public ISpread<ChunkManager> FChunkManager;

        [Output("Progress")]
        public ISpread<double> FProgress;
        
        [Import()]
        public ILogger FLogger;

        ChunkManager _chunkManager = new ChunkManager();
        ChunkImporterASCII _chunkImporterASCII;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FChunkManager.SliceCount = 0;
            FProgress.SliceCount = 1;
            if (FImport[0])
            {
                _chunkManager = new ChunkManager();
                _chunkImporterASCII = new ChunkImporterASCII(_chunkManager);

                _chunkImporterASCII.FilePath = FFile[0];

                _chunkImporterASCII.AlignModeXYZ.x = (int)FAlignX[0];
                _chunkImporterASCII.AlignModeXYZ.y = (int)FAlignY[0];
                _chunkImporterASCII.AlignModeXYZ.z = (int)FAlignZ[0];

                _chunkImporterASCII.ScaleMode = (int)FScaleMode[0];
                _chunkImporterASCII.ScaleValue = FScaleValue[0];

                _chunkImporterASCII.ChunkCount.x = (int)FChunkCount[0].x;
                _chunkImporterASCII.ChunkCount.y = (int)FChunkCount[0].y;
                _chunkImporterASCII.ChunkCount.z = (int)FChunkCount[0].z;

                _chunkImporterASCII.SetDataStructure(FDataStructure[0]);

                _chunkManager.ChunkImporter = _chunkImporterASCII;

                _chunkImporterASCII.Import();
            }

            if (_chunkImporterASCII != null)
            {
                var progress = _chunkImporterASCII.Progress;
                FProgress[0] = progress;
                if (progress == 1) FChunkManager.Add(_chunkManager);
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

        [Output("Caching Progress")]
        public ISpread<double> FOutCachingProgress;

        [Import()]
        public ILogger FLogger;

        private ChunkManager _chunkManager = new ChunkManager();
        private ChunkReaderBinary _chunkReaderBinary;
        private bool changed = false;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutCachingProgress.SliceCount = 1;

            if (FOpen[0])
            {

                _chunkManager = new ChunkManager();
                _chunkReaderBinary = new ChunkReaderBinary(_chunkManager);
                _chunkReaderBinary.Directory = FDirectory[0];
                _chunkReaderBinary.InitChunks();

                if (FPrecache[0]) _chunkReaderBinary.ReadAll();

                changed = true;
            }

            if (_chunkReaderBinary != null)
            {
                if (FPrecache.IsChanged && FPrecache[0]) _chunkReaderBinary.ReadAll();
                FOutCachingProgress[0] = _chunkReaderBinary.Progress;
            }

            if (changed)
            {
                FOutChunkManager.SliceCount = FOutChunkSize.SliceCount = FOutChunkCount.SliceCount =
                FOutBoundsMin.SliceCount = FOutBoundsMax.SliceCount =
                FOutElementCount.SliceCount = 0;

                FOutChunkManager.Add(_chunkManager);
                FOutChunkSize.Add(_chunkManager.ChunkSize);
                FOutChunkCount.Add(new Vector3D((double)_chunkManager.ChunkCount.x, (double)_chunkManager.ChunkCount.y, (double)_chunkManager.ChunkCount.z));

                FOutBoundsMin.Add(_chunkManager.BoundsMin);
                FOutBoundsMax.Add(_chunkManager.BoundsMax);
                FOutElementCount.Add(_chunkManager.ParticleCount);
                changed = false;
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
        
        [Output("Write Progress")]
        public ISpread<double> FOutWriteProgress;
        
        [Import()]
        public ILogger FLogger;
        
        private ChunkWriterBinary _chunkWriterBinary;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutWriteProgress.SliceCount = 1;

            if (FWrite[0] && FChunkManager.IsConnected && FChunkManager != null)
            {
                _chunkWriterBinary = new ChunkWriterBinary(FChunkManager[0]);
                FChunkManager[0].ChunkWriter = _chunkWriterBinary;

                _chunkWriterBinary.Directory = FDirectory[0];
                _chunkWriterBinary.CreateDirectory();
                _chunkWriterBinary.WriteAll();
                _chunkWriterBinary.WriteProjectInfo();
            }
            if (_chunkWriterBinary != null)
            {
                FOutWriteProgress[0] = _chunkWriterBinary.Progress;
            }
        }
    }
}
