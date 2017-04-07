using DX11.Particles.IO.Chunks.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DX11.Particles.IO.Chunks
{
    class ChunkWriterBinary : ChunkWriterBase
    {
        ChunkManager _chunkManager;
        
        public ChunkWriterBinary(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        public override void Write(Chunk chunk)
        {
            int chunkId = chunk.Id;
            if (!WriteOperations.ContainsKey(chunkId))
            {
                WriteOperation writeOperation = new WriteOperation();
                WriteOperations.Add(chunkId, writeOperation);

                writeOperation.Run(chunk, Directory);
            }
            else
            {
                WriteOperation writeOperation = (WriteOperation) WriteOperations[chunkId];
                if (writeOperation.IsCompleted) writeOperation.Run(chunk, Directory);
            }
        }

        public override void WriteAll()
        {
            foreach (Chunk chunk in _chunkManager.ChunkList) Write(chunk);
        }

        class WriteOperation : IIOTaskBase
        {
            
            public override void Run(Chunk chunk, string directory)
            {
                CancellationTokenSource = new CancellationTokenSource();

                Task = Task.Run(
                    async () =>
                    {
                        using (var filestream = new FileStream(Path.Combine(directory, chunk.FileName), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
                        {
                            var buffer = new Byte[DefaultBufferSize];
                            int offset = 0;
                            int byteRead;

                            while ((byteRead = await chunk.MemoryStream.ReadAsync(buffer, offset, buffer.Length)) != 0)
                            {
                                filestream.Write(buffer, offset, byteRead);
                                offset += DefaultBufferSize;
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
