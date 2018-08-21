using System;
using System.Threading;
using System.Threading.Tasks;

namespace N17Solutions.Microphobia.Utilities.Locking
{
    public class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly Task<Releaser> _releaser;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1);
            _releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> Lock()
        {
            var wait = _semaphore.WaitAsync();
            return wait.IsCompleted
                ? _releaser
                : wait.ContinueWith((_, state) => new Releaser((AsyncLock) state), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Releaser(AsyncLock toRelease) => _toRelease = toRelease;

            public void Dispose()
            {
                _toRelease?._semaphore.Release();
            }
        }
    }
    
    
}