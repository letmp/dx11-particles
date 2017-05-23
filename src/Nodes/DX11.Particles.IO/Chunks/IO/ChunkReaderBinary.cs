using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DX11.Particles.IO.Chunks.IO;
using VVVV.Core.Logging;

namespace DX11.Particles.IO
{
    class ChunkReaderBinary : ChunkReaderBase
    {
        ChunkManager _chunkManager;
        
        public ChunkReaderBinary(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
            ReadOperations = new Dictionary<int, IChunkIOTaskBase>();
        }
        
        public override void Read(Chunk chunk)
        {
            int chunkId = chunk.Id;
            if (!ReadOperations.ContainsKey(chunkId))
            {
                ReadOperation readOperation = new ReadOperation();
                ReadOperations.Add(chunkId, readOperation);

                readOperation.Run(chunk, Directory);

                //FLogger.Log(LogType.Message, "ChunkReader: Reading " + chunk.FileName);
                //IOMessages.CurrentState = "Reading " + chunk.FileName;
            }
        }

        public override void ReadAll()
        {
            FLogger.Log(LogType.Message, "ChunkReader: Started caching all files");
            IOMessages.CurrentState = "Started caching all files";

            foreach (Chunk chunk in _chunkManager.ChunkList) Read(chunk);
        }
        
        class ReadOperation : IChunkIOTaskBase
        {
            public override void Run(Chunk chunk, string directory)
            {
                if (!this.IsRunning && !this.IsCompleted)
                {
                    
                    CancellationTokenSource = new CancellationTokenSource();
                    Task = Task.Run(
                        async () =>
                        {
                            using (var file = new FileStream(Path.Combine(directory, chunk.FileName), FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
                            {
                                chunk.MemoryStream.Position = 0;
                                await file.CopyToAsync(chunk.MemoryStream);
                                chunk.UpdateElementCount();                                
                            }
                        },
                        CancellationTokenSource.Token
                        ).ContinueWith(tsk =>
                        {
                            // do something when finished
                            chunk.finishedLoading = true;
                        });
                }
            }
        }

    }
}
