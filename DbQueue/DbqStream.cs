using System;
using System.Collections.Generic;
using System.IO;

namespace DbQueue
{
    public class DbqStream : Stream
    {
        public DbqStream(IAsyncEnumerator<byte[]> enumerator)
        {
            _enumerator = enumerator;
        }

        private readonly IAsyncEnumerator<byte[]> _enumerator;

        public override bool CanRead => _canRead && !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public EventHandler? OnComplete;
        public EventHandler? OnDispose;

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _enumerator.DisposeAsync().AsTask().Wait();
            OnDispose?.Invoke(this, EventArgs.Empty);
            base.Dispose(disposing);
            _disposed = true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var result = 0;

            if (_leftCount > 0)
            {
                result = Math.Min(_leftCount, count);
                Array.Copy(_enumerator.Current, _enumerator.Current.Length - _leftCount, buffer, offset, result);

                _leftCount -= result;
                _position += result;

                if (count == result)
                    return result;

                offset += result;
                count -= result;
            }

            var next = _enumerator.MoveNextAsync().AsTask().Result;

            if (!next)
            {
                _length = _position > 0 ? _position + 1 : 0;

                if (result == 0 && _canRead)
                {
                    OnComplete?.Invoke(this, EventArgs.Empty);
                    _canRead = false;
                }

                return result;
            }

            result = Math.Min(_enumerator.Current.Length, count);
            Array.Copy(_enumerator.Current, 0, buffer, offset, result);

            _leftCount = _enumerator.Current.Length - result;
            _position += result;

            return result;
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }



        private bool _canRead = true;
        private long _position = 0;
        private long _length = long.MaxValue;
        private bool _disposed = false;
        private int _leftCount = 0;
    }

}
