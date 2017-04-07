
using DX11.Particles.IO.Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.VMath;
using System.IO;
using System.Collections.Generic;
using DX11.Particles.IO.Chunks.IO;

namespace DX11.Particles.IO
{
    public interface IChunkWriter
    {
        string Directory { get; set; }
        Dictionary<int, IIOTaskBase> WriteOperations { get; set; }

        void CreateDirectory();
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

        public ChunkWriterBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        public string Directory
        {
            get { return Directory; }
            set { this.Directory = value; }
        }

        public Dictionary<int, IIOTaskBase> WriteOperations
        {
            get { return WriteOperations; }
            set { WriteOperations = value; }
        }

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
            sbInfo.AppendLine("FIELDCOUNTCOLOR: " + _chunkManager.FieldCountColor);
            sbInfo.AppendLine("ELEMENTCOUNT: " + _chunkManager.GetElementCount());
            
            string infofile = Path.Combine(Directory, "_chunkinfo");
            StreamWriter swInfo = new StreamWriter(infofile, false);
            swInfo.WriteLine(sbInfo.ToString());
            swInfo.Close();
        }

        public abstract void Write(Chunk chunk);
        public abstract void WriteAll();

        public double Progress
        {
            get
            {
                if (Progress < 1) // calculate progress only 
                {
                    int chunkCount = _chunkManager.ChunkCount.x * _chunkManager.ChunkCount.y * _chunkManager.ChunkCount.z;
                    int chunkCachedCount = WriteOperations.Where(kvp => kvp.Value.IsCompleted).Count();
                    Progress = chunkCachedCount / chunkCount;
                }
                return Progress;

            }
            set { Progress = value; }
        }
    }

}
