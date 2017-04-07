using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DX11.Particles.IO.Chunks.IO
{
    public interface IIOTask
    {
        Task Task { get; set; }
        CancellationTokenSource CancellationTokenSource { get; set; }
        
        void Run(Chunk chunk, string directory);
        bool IsRunning { get; }
        bool IsCompleted { get; }
    }

    public abstract class IIOTaskBase : IIOTask, IDisposable
    {
        public bool IsCompleted
        {
            get { return Task != null; }
        }

        public bool IsRunning
        {
            get { return Task != null && Task.IsCompleted; }
        }

        public Task Task
        {
            get { return Task; }
            set { Task = value; }
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get { return CancellationTokenSource; }
            set { CancellationTokenSource = value; }
        }

        public abstract void Run(Chunk chunk, string directory);

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
