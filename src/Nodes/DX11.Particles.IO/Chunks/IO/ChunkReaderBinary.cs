﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DX11.Particles.IO.Chunks.IO;

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
            }
        }

        public override void ReadAll()
        {
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
                                var buffer = new Byte[DefaultBufferSize];
                                int offset = 0;
                                int byteRead;

                                while ((byteRead = await file.ReadAsync(buffer, offset, buffer.Length)) != 0)
                                {
                                    chunk.MemoryStream.Write(buffer, offset, byteRead);
                                    offset += DefaultBufferSize;
                                    chunk.UpdateElementCount(); // update the elementcount after new data was inserted
                                }
                            }
                        },
                        CancellationTokenSource.Token
                        ).ContinueWith(tsk =>
                        {
                            // do something when finished
                        });
                }
            }
        }

    }
}