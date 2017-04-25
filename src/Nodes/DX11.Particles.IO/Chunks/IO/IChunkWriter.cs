
using DX11.Particles.IO.Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.VMath;
using System.IO;
using System.Collections.Generic;
using DX11.Particles.IO.Chunks.IO;
using System;

namespace DX11.Particles.IO
{
    public interface IChunkWriter
    {
        string Directory { get; set; }
        
        void WriteProjectInfo();
        void Write(Chunk chunk);
        void WriteAll();

        double Progress { get; set; }
    }

    public abstract class ChunkWriterBase : GenericConstructor<ChunkManager>, IChunkWriter
    {
        ChunkManager _chunkManager;
        public const int DefaultBufferSize = 4096;
        public const FileOptions DefaultFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        public Dictionary<int, IChunkIOTaskBase> WriteOperations = new Dictionary<int, IChunkIOTaskBase>();

        double _progress = 0;

        public ChunkWriterBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        public string Directory { get; set; }
        
        public void CreateDirectory()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                DirectoryInfo dir = new DirectoryInfo(Directory);
                foreach (FileInfo file in dir.GetFiles()) file.Delete();
                foreach (DirectoryInfo subdir in dir.GetDirectories()) subdir.Delete(true);
            }
            else
            {
                DirectoryInfo dir = System.IO.Directory.CreateDirectory(Directory);
            }
        }
        
        public void WriteProjectInfo()
        {
            StringBuilder sbInfo = new StringBuilder();

            sbInfo.AppendLine("CHUNKSIZEXYZ: " + _chunkManager.ChunkSize.x + " " + _chunkManager.ChunkSize.y + " " + _chunkManager.ChunkSize.z);
            sbInfo.AppendLine("CHUNKCOUNTXYZ: " + _chunkManager.ChunkCount.x + " " + _chunkManager.ChunkCount.y + " " + _chunkManager.ChunkCount.z);
            sbInfo.AppendLine("BOUNDSMIN: " + _chunkManager.BoundsMin.x + " " + _chunkManager.BoundsMin.y + " " + _chunkManager.BoundsMin.z);
            sbInfo.AppendLine("BOUNDSMAX: " + _chunkManager.BoundsMax.x + " " + _chunkManager.BoundsMax.y + " " + _chunkManager.BoundsMax.z);
            sbInfo.AppendLine("DATASTRUCTURE: " + _chunkManager.DataStructure);
            sbInfo.AppendLine("PARTICLECOUNT: " + _chunkManager.GetCachedParticleCount());
            
            string infofile = Path.Combine(Directory, "_chunkinfo");
            StreamWriter swInfo = new StreamWriter(infofile, false);
            swInfo.WriteLine(sbInfo.ToString());
            swInfo.Close();
        }

        public abstract void Write(Chunk chunk);
        public abstract void WriteAll();

        
        private void UpdateProgress()
        {
            if (_progress < 1)
            {
                int chunkCount = _chunkManager.ChunkCount.x * _chunkManager.ChunkCount.y * _chunkManager.ChunkCount.z;
                int chunkCachedCount = WriteOperations.Where(kvp => kvp.Value.IsCompleted).Count();
                _progress = Convert.ToDouble(chunkCachedCount) / Convert.ToDouble(chunkCount);
            }
        }

        public double Progress { get { UpdateProgress(); return _progress; } set { _progress = value; } }

    }

}
