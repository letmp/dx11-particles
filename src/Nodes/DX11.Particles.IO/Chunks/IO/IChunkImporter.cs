using DX11.Particles.IO.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VVVV.Utils.VMath;
using System;

namespace DX11.Particles.IO
{
    public interface IChunkImporter
    {
        string FilePath { get; set; }
        void Import();
        double Progress { get; set; }
    }

    public abstract class ChunkImporterBase : GenericConstructor<ChunkManager>, IChunkImporter
    {
        ChunkManager _chunkManager;

        public const int DefaultBufferSize = 4096;
        public const FileOptions DefaultFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        public Dictionary<string, int> DataStructure;

        public Vector3D ChunkSize;
        public Triple<int, int, int> ChunkCount;
        public Vector3D BoundsMin;
        public Vector3D BoundsMax;

        public Triple<int, int, int> AlignModeXYZ;
        public Vector3D Offsets = new Vector3D(0, 0, 0);

        public int ScaleMode;
        public double ScaleMultiplier;

        public int Lines = 0;
        public int LinesProcessed = 0;

        public ChunkImporterBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }
        
        public string FilePath
        {
            get { return FilePath; }
            set { FilePath = value; }
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

        private int GetFieldCountColor()
        {
            int fieldCount = 0;
            if (DataStructure.ContainsKey("r")) fieldCount++;
            if (DataStructure.ContainsKey("g")) fieldCount++;
            if (DataStructure.ContainsKey("b")) fieldCount++;
            if (DataStructure.ContainsKey("a")) fieldCount++;
            return fieldCount;
        }
        
        public void Import()
        {
            // the import consists of 3 steps
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task Task = Task.Run(
                () => ParseBounds() // parse the file to find the bounds of the dataset
                ).ContinueWith(
                tsk => UpdateChunkManager() // update chunkmanager with new data and init chunklist
                ).ContinueWith(
                tsk => ImportData() // parse the file again and write the data to the chunks
                );
        }
        
        public abstract Task ParseBounds();

        private async Task UpdateChunkManager()
        {
            await Task.Run(() =>
            {
                _chunkManager.ChunkCount = ChunkCount;
                _chunkManager.FieldCountColor = GetFieldCountColor();

                UpdateOffsets();
                UpdateScaleMultiplier();

                UpdateBounds();
                _chunkManager.BoundsMin = BoundsMin;
                _chunkManager.BoundsMin = BoundsMax;

                UpdateChunkSize();
                _chunkManager.ChunkSize = ChunkSize;

                _chunkManager.InitChunkList();

            });
        }

        public abstract Task ImportData();

        private void UpdateOffsets()
        {
            if (AlignModeXYZ.x == Utils.AlignMode.MIN) Offsets.x = 0 - BoundsMin.x;
            if (AlignModeXYZ.x == Utils.AlignMode.CENTER) Offsets.x = 0 - BoundsMin.x - ((BoundsMax.x - BoundsMin.x) / 2);
            if (AlignModeXYZ.x == Utils.AlignMode.MAX) Offsets.x = 0 - BoundsMax.x;

            if (AlignModeXYZ.y == Utils.AlignMode.MIN) Offsets.y = 0 - BoundsMin.y;
            if (AlignModeXYZ.y == Utils.AlignMode.CENTER) Offsets.y = 0 - BoundsMin.y - ((BoundsMax.y - BoundsMin.y) / 2);
            if (AlignModeXYZ.y == Utils.AlignMode.MAX) Offsets.y = 0 - BoundsMax.y;

            if (AlignModeXYZ.z == Utils.AlignMode.MIN) Offsets.z = 0 - BoundsMin.z;
            if (AlignModeXYZ.z == Utils.AlignMode.CENTER) Offsets.z = 0 - BoundsMin.z - ((BoundsMax.z - BoundsMin.z) / 2);
            if (AlignModeXYZ.z == Utils.AlignMode.MAX) Offsets.z = 0 - BoundsMax.z;
        }

        private void UpdateScaleMultiplier()
        {
            //if (ScaleMode == Utils.ScaleMode.MULTIPLY) ScaleMultiplier = ScaleMultiplier;
            if (ScaleMode == Utils.ScaleMode.MAXX) ScaleMultiplier = ScaleMultiplier / (BoundsMax.x - BoundsMin.x);
            if (ScaleMode == Utils.ScaleMode.MAXY) ScaleMultiplier = ScaleMultiplier / (BoundsMax.y - BoundsMin.y);
            if (ScaleMode == Utils.ScaleMode.MAXZ) ScaleMultiplier = ScaleMultiplier / (BoundsMax.z - BoundsMin.z);
        }

        private void UpdateBounds()
        {
            BoundsMin = (BoundsMin + Offsets) * ScaleMultiplier;
            BoundsMin = (BoundsMin + Offsets) * ScaleMultiplier;
        }

        private void UpdateChunkSize()
        {
            Vector3D size = BoundsMax - BoundsMin;
            ChunkSize = new Vector3D(size.x / ChunkCount.x, size.y / ChunkCount.y, size.z / ChunkCount.z);
        }

        public int GetChunkIndex(Vector3D position)
        {
            position = (position + Offsets) * ScaleMultiplier; // transform to new aligning and scaling first

            int chunkIdX = Convert.ToInt32(Math.Floor(position.x / ChunkSize.x));
            if (position.x == ChunkSize.x * ChunkCount.x) chunkIdX -= 1;
            int chunkIdY = Convert.ToInt32(Math.Floor(position.y / ChunkSize.y));
            if (position.y == ChunkSize.y * ChunkCount.y) chunkIdY -= 1;
            int chunkIdZ = Convert.ToInt32(Math.Floor(position.z / ChunkSize.z));
            if (position.z == ChunkSize.z * ChunkCount.z) chunkIdZ -= 1;

            return chunkIdX + chunkIdY * ChunkCount.x + chunkIdZ * ChunkCount.x * ChunkCount.y;
        }

        public double Progress
        {
            get
            {
                if (Progress < 1 && Lines > 0)
                {
                    Progress = Convert.ToDouble(LinesProcessed) / Convert.ToDouble(Lines);
                }
                return Progress;
            }
            set { Progress = value; }
        }
        
    }
}
