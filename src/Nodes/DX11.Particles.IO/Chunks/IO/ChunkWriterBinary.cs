using DX11.Particles.IO.Chunks.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VVVV.Core.Logging;

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
                //FLogger.Log(LogType.Message, "ChunkWriter: Writing " + chunk.FileName);
                //IOMessages.CurrentState = "Writing" + chunk.FileName;
            }
            else
            {
                WriteOperation writeOperation = (WriteOperation) WriteOperations[chunkId];
                if (writeOperation.IsCompleted)
                {
                    writeOperation.Run(chunk, Directory);
                    //FLogger.Log(LogType.Message, "ChunkWriter: ReWriting " + chunk.FileName);
                    //IOMessages.CurrentState = "ReWriting" + chunk.FileName;
                }
            }
        }

        public override void WriteAll()
        {
            FLogger.Log(LogType.Message, "ChunkWriter: Started writing all files");
            IOMessages.CurrentState = "Started writing all files";
            foreach (Chunk chunk in _chunkManager.ChunkList) Write(chunk);
        }

        class WriteOperation : IChunkIOTaskBase
        {
            
            public override void Run(Chunk chunk, string directory)
            {
                CancellationTokenSource = new CancellationTokenSource();

                Task = Task.Run(
                    async () =>
                    {
                        using (var filestream = new FileStream(Path.Combine(directory, chunk.FileName), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
                        {
                            chunk.MemoryStream.Position = 0;
                            await chunk.MemoryStream.CopyToAsync(filestream);
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
