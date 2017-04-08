using DX11.Particles.IO.Utils;
using System;
using VVVV.Utils.VMath;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DX11.Particles.IO.Chunks.IO;

namespace DX11.Particles.IO
{
    public interface IChunkReader
    {
        string Directory { get; set; }

        void ReadProjectInfo();
        void Read(Chunk chunk);
        void ReadAll();

        double Progress { get; set; }
    }

    public abstract class ChunkReaderBase : GenericConstructor<ChunkManager>, IChunkReader
    {
        ChunkManager _chunkManager;
        public const int DefaultBufferSize = 4096;
        public const FileOptions DefaultFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        public Dictionary<int, IChunkIOTaskBase> ReadOperations;

        public ChunkReaderBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        public string Directory
        {
            get { return Directory; }
            set { this.Directory = value; }
        }

        public void ReadProjectInfo()
        {
            string filename = Path.Combine(Directory, "_chunkinfo");
            if (File.Exists(filename))
            {
                StreamReader sr = new StreamReader(filename);
                Char delimiter = ' ';

                String[] substrings = sr.ReadLine().Split(delimiter);
                _chunkManager.ChunkSize = new Vector3D(double.Parse(substrings[1]), double.Parse(substrings[2]), double.Parse(substrings[3]));

                substrings = sr.ReadLine().Split(delimiter);
                Triple<int, int, int> chunkCount = new Triple<int, int, int>();
                chunkCount.x = Int32.Parse(substrings[1]);
                chunkCount.y = Int32.Parse(substrings[2]);
                chunkCount.z = Int32.Parse(substrings[3]);
                _chunkManager.ChunkCount = chunkCount;

                substrings = sr.ReadLine().Split(delimiter);
                _chunkManager.BoundsMin = new Vector3D(double.Parse(substrings[1]), double.Parse(substrings[2]), double.Parse(substrings[3]));

                substrings = sr.ReadLine().Split(delimiter);
                _chunkManager.BoundsMax = new Vector3D(double.Parse(substrings[1]), double.Parse(substrings[2]), double.Parse(substrings[3]));

                substrings = sr.ReadLine().Split(delimiter);
                _chunkManager.FieldCountColor = Int32.Parse(substrings[1]);
            }
        }

        public abstract void Read(Chunk chunk);
        public abstract void ReadAll();

        public double Progress
        {
            get
            {
                if (Progress < 1) 
                {
                    int chunkCount = _chunkManager.ChunkCount.x * _chunkManager.ChunkCount.y * _chunkManager.ChunkCount.z;
                    int chunkCachedCount = ReadOperations.Where(kvp => kvp.Value.IsCompleted).Count();
                    Progress = Convert.ToDouble(chunkCachedCount) / Convert.ToDouble(chunkCount);
                }
                return Progress;

            }
            set { Progress = value; }
        }


    }

}
