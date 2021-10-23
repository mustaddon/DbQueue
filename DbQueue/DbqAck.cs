using System;
using System.Threading.Tasks;

namespace DbQueue
{
    public class DbqAck<T> : IDbqAcknowledgement<T>
    {
        public DbqAck(T data, Func<Task> commit, Func<ValueTask>? dispose = null)
        {
            Data = data;
            _commit = commit;
            _dispose = dispose;
        }

        private readonly Func<Task> _commit;
        private readonly Func<ValueTask>? _dispose;
        private bool _commited = false;
        private bool _disposed = false;

        public T Data { get; }

        public async Task Commit()
        {
            if (_commited)
                return;

            if (_disposed)
                throw new ObjectDisposedException(this.GetType().Name);

            await _commit();
            _commited = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed || _dispose == null)
                return;

            await _dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            if (!_disposed) DisposeAsync().AsTask().Wait();
        }
    }
}
