using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DirectPackageInstaller.IO
{
    public class SegmentedStreamReader : Stream
    {
        // Multiple readers may wrap the same segmented stream. Position changes and
        // reads must be atomic together, otherwise one reader can move another reader.
        private static readonly ConditionalWeakTable<Stream, object> StreamLocks = new();
        private readonly Stream _baseStream;
        private readonly object _streamLock;
        private long _position;

        public SegmentedStreamReader(Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _streamLock = StreamLocks.GetValue(_baseStream, _ => new object());
            _position = 0;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get { lock (_streamLock) return _position; }
            set
            {
                lock (_streamLock)
                {
                    if (value < 0 || value > Length)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _position = value;
                }
            }
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            lock (_streamLock)
            {
                if (_position >= Length || count == 0)
                    return 0;

                _baseStream.Position = _position;
                int read = _baseStream.Read(buffer, offset, (int)Math.Min(count, Length - _position));
                _position += read;
                return read;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (_streamLock)
            {
                long target = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => _position + offset,
                    SeekOrigin.End => Length + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin))
                };

                if (target < 0 || target > Length)
                    throw new IOException("Invalid stream position");

                _position = target;
                return _position;
            }
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // The wrapped stream is shared by other readers; do not dispose it here.
            base.Dispose(disposing);
        }
    }
}