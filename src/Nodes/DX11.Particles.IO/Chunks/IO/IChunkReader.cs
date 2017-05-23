using DX11.Particles.IO.Utils;
using System;
using VVVV.Utils.VMath;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DX11.Particles.IO.Chunks.IO;
using VVVV.Core.Logging;

namespace DX11.Particles.IO
{
    public interface IChunkReader
    {
        string Directory { get; set; }

        void InitChunks();
        void Read(Chunk chunk);
        void ReadAll();

        double Progress { get; set; }
    }

    public abstract class ChunkReaderBase : GenericConstructor<ChunkManager>, IChunkReader
    {
        ChunkManager _chunkManager;
        public const int DefaultBufferSize = 4096;
        public const FileOptions DefaultFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        public Dictionary<int, IChunkIOTaskBase> ReadOperations = new Dictionary<int, IChunkIOTaskBase>();

        double _progress = 0;

        public ILogger FLogger;
        public IOMessages IOMessages;

        public ChunkReaderBase(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        public string Directory { get; set; }

        public void InitChunks()
        {
            FLogger.Log(LogType.Message, "ChunkReader: Reading ChunkInfo File");
            IOMessages.CurrentState = "Reading ChunkInfo File";

            try
            {
                string filePath = Path.Combine(Directory, "_chunkinfo");
                if (File.Exists(filePath))
                {
                    StreamReader sr = new StreamReader(filePath);
                    Char delimiter = ' ';

                    String[] lineStrings = sr.ReadLine().Split(delimiter);
                    _chunkManager.ChunkSize = new Vector3D(double.Parse(lineStrings[1]), double.Parse(lineStrings[2]), double.Parse(lineStrings[3]));

                    lineStrings = sr.ReadLine().Split(delimiter);
                    Triple<int, int, int> chunkCount = new Triple<int, int, int>();
                    chunkCount.x = Int32.Parse(lineStrings[1]);
                    chunkCount.y = Int32.Parse(lineStrings[2]);
                    chunkCount.z = Int32.Parse(lineStrings[3]);
                    _chunkManager.ChunkCount = chunkCount;

                    lineStrings = sr.ReadLine().Split(delimiter);
                    _chunkManager.BoundsMin = new Vector3D(double.Parse(lineStrings[1]), double.Parse(lineStrings[2]), double.Parse(lineStrings[3]));

                    lineStrings = sr.ReadLine().Split(delimiter);
                    _chunkManager.BoundsMax = new Vector3D(double.Parse(lineStrings[1]), double.Parse(lineStrings[2]), double.Parse(lineStrings[3]));

                    lineStrings = sr.ReadLine().Split(delimiter);
                    _chunkManager.DataStructure = lineStrings[1];

                    lineStrings = sr.ReadLine().Split(delimiter);
                    _chunkManager.ElementCount = Convert.ToInt32(lineStrings[1]);

                    _chunkManager.InitChunkList();
                }
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Error, e.ToString());
                IOMessages.CurrentState = e.ToString();
            }
            
        }

        public abstract void Read(Chunk chunk);
        public abstract void ReadAll();

        
        private void UpdateProgress()
        {
            if (_progress < 1)
            {
                int chunkCount = _chunkManager.ChunkCount.x * _chunkManager.ChunkCount.y * _chunkManager.ChunkCount.z;
                int chunkCachedCount = ReadOperations.Where(kvp => kvp.Value.IsCompleted).Count();
                _progress = Convert.ToDouble(chunkCachedCount) / Convert.ToDouble(chunkCount);
            }
        }
        public double Progress { get { UpdateProgress(); return _progress; }  set { _progress = value; } }
        
    }

}
