using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DirectPackageInstaller.IO
{
    /// <summary>
    /// Parallel HTTP-range downloader backed only by bounded RAM.
    /// Completed segments may be evicted and re-fetched, so the stream never
    /// requires a full-size local file.
    /// </summary>
    public sealed class RamSegmentedStream : Stream
    {
        private sealed class Segment
        {
            public long Offset;
            public int Length;
            public byte[]? Data;
            public Task? Loading;
            public Exception? Error;
            public DateTime LastUse;
        }

        private readonly Func<Stream> _openSegment;
        private readonly int _segmentSize;
        private readonly int _maxCachedSegments;
        private readonly int _concurrency;
        private readonly object _lock = new();
        private readonly Dictionary<long, Segment> _segments = new();
        private readonly SemaphoreSlim _slots;
        private readonly CancellationTokenSource _cts = new();
        private long _position;

        public long TotalSize { get; }
        public int Concurrency => _concurrency;
        public int SegmentSize => _segmentSize;
        public int MaxCachedSegments => _maxCachedSegments;

        public long CachedBytes
        {
            get
            {
                lock (_lock)
                    return _segments.Values.Sum(x => (long)(x.Data?.Length ?? 0));
            }
        }

        public RamSegmentedStream(
            Func<Stream> openSegment,
            int segmentSize = 4 * 1024 * 1024,
            int maxCachedSegments = 8,
            int concurrency = 4)
        {
            _openSegment = openSegment ?? throw new ArgumentNullException(nameof(openSegment));
            _segmentSize = Math.Max(256 * 1024, segmentSize);
            _maxCachedSegments = Math.Max(2, maxCachedSegments);
            _concurrency = Math.Max(1, concurrency);
            _slots = new SemaphoreSlim(_concurrency, _concurrency);

            using var first = _openSegment();
            TotalSize = first.Length;

            if (TotalSize < 0)
                throw new IOException("Unable to determine remote file length");
        }

        private Segment GetOrCreate(long offset)
        {
            lock (_lock)
            {
                if (!_segments.TryGetValue(offset, out var segment))
                {
                    var length = (int)Math.Min(_segmentSize, TotalSize - offset);
                    segment = new Segment { Offset = offset, Length = length, LastUse = DateTime.UtcNow };
                    _segments[offset] = segment;
                }

                segment.LastUse = DateTime.UtcNow;

                if (segment.Data == null && segment.Loading == null)
                    segment.Loading = LoadSegmentAsync(segment);

                return segment;
            }
        }

        private async Task LoadSegmentAsync(Segment segment)
        {
            await _slots.WaitAsync(_cts.Token).ConfigureAwait(false);
            try
            {
                using var source = _openSegment();
                using var virtualStream = new VirtualStream(source, segment.Offset, segment.Length);

                var data = new byte[segment.Length];
                var readTotal = 0;

                while (readTotal < data.Length)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var read = await virtualStream.ReadAsync(
                        data, readTotal, data.Length - readTotal, _cts.Token).ConfigureAwait(false);

                    if (read <= 0)
                        throw new EndOfStreamException(
                            $"Remote stream ended at {segment.Offset + readTotal} of {segment.Offset + segment.Length}");

                    readTotal += read;
                }

                lock (_lock)
                {
                    segment.Data = data;
                    segment.Error = null;
                    segment.LastUse = DateTime.UtcNow;
                    EvictUnsafe(segment.Offset);
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                    segment.Error = ex;
            }
            finally
            {
                lock (_lock)
                    segment.Loading = null;

                _slots.Release();
            }
        }

        private void EvictUnsafe(long protectedOffset)
        {
            while (_segments.Values.Count(x => x.Data != null) > _maxCachedSegments)
            {
                var victim = _segments.Values
                    .Where(x => x.Data != null && x.Offset != protectedOffset && x.Loading == null)
                    .OrderBy(x => x.LastUse)
                    .FirstOrDefault();

                if (victim == null)
                    return;

                victim.Data = null;
            }
        }

        private byte[] GetSegmentData(long offset)
        {
            var segment = GetOrCreate(offset);

            while (true)
            {
                Task? loading;
                Exception? error;
                byte[]? data;

                lock (_lock)
                {
                    loading = segment.Loading;
                    error = segment.Error;
                    data = segment.Data;
                    segment.LastUse = DateTime.UtcNow;
                }

                if (data != null)
                    return data;

                if (error != null)
                    throw new IOException($"Failed to download range at {offset}", error);

                loading?.GetAwaiter().GetResult();

                if (loading == null)
                    Thread.Yield();
            }
        }

        private void Prefetch(long offset)
        {
            if (offset < 0 || offset >= TotalSize)
                return;

            lock (_lock)
            {
                if (_segments.TryGetValue(offset, out var existing) &&
                    (existing.Data != null || existing.Loading != null))
                    return;

                var segment = new Segment
                {
                    Offset = offset,
                    Length = (int)Math.Min(_segmentSize, TotalSize - offset),
                    LastUse = DateTime.UtcNow
                };

                _segments[offset] = segment;
                segment.Loading = LoadSegmentAsync(segment);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            if (count == 0 || _position >= TotalSize)
                return 0;

            count = (int)Math.Min(count, TotalSize - _position);
            var remaining = count;
            var destination = offset;

            while (remaining > 0)
            {
                var segmentOffset = (_position / _segmentSize) * _segmentSize;
                var data = GetSegmentData(segmentOffset);
                var within = (int)(_position - segmentOffset);
                var available = Math.Min(remaining, data.Length - within);

                Buffer.BlockCopy(data, within, buffer, destination, available);

                _position += available;
                destination += available;
                remaining -= available;

                Prefetch(segmentOffset + _segmentSize);
            }

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => TotalSize + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (target < 0 || target > TotalSize)
                throw new IOException("Invalid stream position");

            _position = target;
            return _position;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => TotalSize;
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Cancel();
                _slots.Dispose();
                _cts.Dispose();

                lock (_lock)
                    _segments.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
