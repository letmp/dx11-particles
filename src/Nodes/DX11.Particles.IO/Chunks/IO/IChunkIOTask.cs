using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DX11.Particles.IO.Chunks.IO
{
    public interface IChunkIOTask
    {
        
        void Run(Chunk chunk, string directory);
        bool IsRunning { get; }
        bool IsCompleted { get; }
    }

    public abstract class IChunkIOTaskBase : IChunkIOTask, IDisposable
    {
        public Task Task;
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public abstract void Run(Chunk chunk, string directory);

        public bool IsCompleted
        {
            get { return Task != null; }
        }

        public bool IsRunning
        {
            get { return Task != null && Task.IsCompleted; }
        }
        
        public void Dispose()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
            }
            if (Task != null)
            {
                try
                {
                    Task.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Handle(e => e is OperationCanceledException);
                }
                finally
                {
                    Task.Dispose();
                    Task = null;
                }
            }
        }
    }

}
