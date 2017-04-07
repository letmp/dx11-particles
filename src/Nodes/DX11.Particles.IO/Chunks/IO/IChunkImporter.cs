using DX11.Particles.IO.Utils;
using System.Collections.Generic;
using System.IO;
using VVVV.Utils.VMath;

namespace DX11.Particles.IO
{
    public interface IChunkImporter
    {
        string FilePath { get; set; }

        Dictionary<string, int> DataStructure { get; set; }
        void SetDataStructure(string dataStructureString);
        void SetFieldCountColor();

        Vector3D BoundsMin { get; set; }
        Vector3D BoundsMax { get; set; }

        Triple<int, int, int> AlignModeXYZ { get; set; }
        Vector3D Offsets { get; set; }
        void UpdateOffsets();

        int ScaleMode { get; set; }
        double ScaleMultiplier { get; set; }
        void UpdateScaleMultiplier();

        void ParseBounds();
        void ImportChunks();

        int Lines { get; set; }
        int LinesProcessed { get; set; }
        double Progress { get; set; }
    }

    public abstract class ChunkImporterBase : GenericConstructor<ChunkManager>, IChunkImporter
    {
        ChunkManager _chunkManager;
        public const int DefaultBufferSize = 4096;
        public const FileOptions DefaultFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        public ChunkImporterBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }
        
        public string FilePath
        {
            get { return FilePath; }
            set { FilePath = value; }
        }
        
        public Vector3D BoundsMin
        {
            get { return BoundsMin; }
            set { BoundsMin = value; }
        }
        public Vector3D BoundsMax
        {
            get { return BoundsMax; }
            set { BoundsMax = value; }
        }
        
        public Triple<int, int, int> AlignModeXYZ
        {
            get { return AlignModeXYZ; }
            set { AlignModeXYZ = value; }
        }
        public Vector3D Offsets
        {
            get { return Offsets; }
            set { Offsets = value; }
        }
        public void UpdateOffsets()
        {
            Vector3D offsets = new Vector3D(0, 0, 0);
            
            if (AlignModeXYZ.x == Utils.AlignMode.MIN) offsets.x = 0 - BoundsMin.x;
            if (AlignModeXYZ.x == Utils.AlignMode.CENTER) offsets.x = 0 - BoundsMin.x - ((BoundsMax.x - BoundsMin.x) / 2);
            if (AlignModeXYZ.x == Utils.AlignMode.MAX) offsets.x = 0 - BoundsMax.x;
            
            if (AlignModeXYZ.y == Utils.AlignMode.MIN) offsets.y = 0 - BoundsMin.y;
            if (AlignModeXYZ.y == Utils.AlignMode.CENTER) offsets.y = 0 - BoundsMin.y - ((BoundsMax.y - BoundsMin.y) / 2);
            if (AlignModeXYZ.y == Utils.AlignMode.MAX) offsets.y = 0 - BoundsMax.y;
            
            if (AlignModeXYZ.z == Utils.AlignMode.MIN) offsets.z = 0 - BoundsMin.z;
            if (AlignModeXYZ.z == Utils.AlignMode.CENTER) offsets.z = 0 - BoundsMin.z - ((BoundsMax.z - BoundsMin.z) / 2);
            if (AlignModeXYZ.z == Utils.AlignMode.MAX) offsets.z = 0 - BoundsMax.z;

            Offsets = offsets;
        }

        public int ScaleMode
        {
            get { return ScaleMode; }
            set { ScaleMode = value; }
        }
        public double ScaleMultiplier
        {
            get { return ScaleMultiplier; }
            set { ScaleMultiplier = value; }
        }
        public void UpdateScaleMultiplier()
        {
            if (ScaleMode == Utils.ScaleMode.MULTIPLY) ScaleMultiplier = ScaleMultiplier;
            if (ScaleMode == Utils.ScaleMode.MAXX) ScaleMultiplier = ScaleMultiplier / (BoundsMax.x - BoundsMin.x);
            if (ScaleMode == Utils.ScaleMode.MAXY) ScaleMultiplier = ScaleMultiplier / (BoundsMax.y - BoundsMin.y);
            if (ScaleMode == Utils.ScaleMode.MAXZ) ScaleMultiplier = ScaleMultiplier / (BoundsMax.z - BoundsMin.z);
        }

        public Dictionary<string, int> DataStructure
        {
            get { return DataStructure; }
            set { DataStructure = value; }
        }
        public void SetDataStructure(string dataStructureString)
        {
            Dictionary<string, int> dataStructure = new Dictionary<string, int>();
            if (dataStructureString.Contains("x")) dataStructure.Add("x", dataStructureString.IndexOf("x"));
            if (dataStructureString.Contains("y")) dataStructure.Add("y", dataStructureString.IndexOf("y"));
            if (dataStructureString.Contains("z")) dataStructure.Add("z", dataStructureString.IndexOf("z"));
            if (dataStructureString.Contains("r")) dataStructure.Add("r", dataStructureString.IndexOf("r"));
            if (dataStructureString.Contains("g")) dataStructure.Add("g", dataStructureString.IndexOf("g"));
            if (dataStructureString.Contains("b")) dataStructure.Add("b", dataStructureString.IndexOf("b"));
            if (dataStructureString.Contains("a")) dataStructure.Add("a", dataStructureString.IndexOf("a"));
            this.DataStructure = dataStructure;
        }
        public void SetFieldCountColor()
        {
            int fieldCount = 0;
            if (DataStructure.ContainsKey("r")) fieldCount++;
            if (DataStructure.ContainsKey("g")) fieldCount++;
            if (DataStructure.ContainsKey("b")) fieldCount++;
            if (DataStructure.ContainsKey("a")) fieldCount++;
            _chunkManager.FieldCountColor = fieldCount;
        }
        
        public abstract void ParseBounds();
        public abstract void ImportChunks();

        public int Lines
        {
            get { return Lines; }
            set { Lines = value; }
        }
        public int LinesProcessed
        {
            get { return LinesProcessed; }
            set { LinesProcessed = value; }
        }
        public double Progress
        {
            get {
                double progress = 0;
                if(Lines > 0)
                {
                    progress = LinesProcessed / Lines;
                }
                return progress;
            }
            set { Progress = value; }
        }
    }
}
