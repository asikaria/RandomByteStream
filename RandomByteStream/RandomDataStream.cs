using System;
using System.Threading;
using System.IO;
using System.Security.Cryptography;

namespace RandomByteStream
{
    /// <summary>
    /// Generates random numbers for creating a high-performance test stream.
    /// This stream pre-generates generates a few megabytes of random data, and uses that same data repeatedly. 
    /// This is not suitable for security purposes. However, for testing data processing pipelines,
    /// this provides just what is needed - random (non-compressible) bytes of arbitrary length.
    /// 
    /// The stream can be given a length at creation time, in which case it will only serve that many bytes before 
    /// emitting EOF.
    /// 
    /// Note that the random-number buffer is shared among all instances of this class, so the same random numbers 
    /// will be emitted by every RandomDataStream instance.
    /// 
    /// Thread safety: It is safe to call all methods of this class concurrently.
    /// </summary>
    public class RandomDataStream : Stream
    {
        private const int Size = 1024 * 1024 * 16;
        private static readonly byte[] _InternalBuffer = new byte[Size * 2];
        private long _cursor = 0;
        private long _streamLength;

        static RandomDataStream()
        {
            var prng = RandomNumberGenerator.Create();
            byte[] random = new byte[Size];
            prng.GetBytes(random);
            Buffer.BlockCopy(random, 0, _InternalBuffer, 0, Size);
            Buffer.BlockCopy(random, 0, _InternalBuffer, Size, Size);
        }

        /// <summary>
        /// Create a random data stream
        /// </summary>
        public RandomDataStream()
            : this(Int64.MaxValue)
        {
        }

        /// <summary>
        /// Create a Random data stream of specified length.
        /// </summary>
        /// <param name="length"></param>
        public RandomDataStream(long length)
        {
            _streamLength = length;
        }

        public byte[] InternalBuffer => _InternalBuffer;

        /// <summary>
        /// 
        /// </summary>
        public int InternalBufferSize => Size;


        /// <summary>Gets a value indicating whether the current stream supports reading (true always).</summary>
        /// <returns>true always,</returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanRead { get; } = true;

        /// <summary>Gets a value indicating whether the current stream supports seeking (true always).</summary>
        /// <returns>true</returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanSeek { get; } = true;

        /// <summary>Gets a value indicating whether the current stream supports writing (false always).</summary>
        /// <returns>false</returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanWrite { get; } = false;

        /// <inheritdoc />
        public override void Flush() => throw new NotImplementedException();

        /// <inheritdoc />
        public override long Length => _streamLength;


        /// <inheritdoc />
        public override long Position
        {
            get => Interlocked.Read(ref _cursor);
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > Size) count = Size;
            long tCursor;
            int tCount;
            do
            {
                tCursor = Interlocked.Read(ref _cursor);
                tCount = (int)Math.Min(count, _streamLength - tCursor);
                if (tCount < 1) return 0;
            } while (Interlocked.CompareExchange(ref _cursor, tCursor + tCount, tCursor) != tCursor);
            Buffer.BlockCopy(_InternalBuffer, (int)tCursor % Size, buffer, offset, tCount);
            return tCount;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newCursor = _cursor;

            if (origin == SeekOrigin.Begin)
            {
                newCursor = offset;
                if (newCursor <= 0) newCursor = 0;
                if (newCursor > _streamLength) newCursor = _streamLength;
                Interlocked.Exchange(ref _cursor, newCursor);
            }

            if (origin == SeekOrigin.Current)
            {
                long tCursor;
                do
                {
                    tCursor = Interlocked.Read(ref _cursor);
                    newCursor = tCursor + offset;
                    if (newCursor <= 0) newCursor = 0;
                    if (newCursor > _streamLength) newCursor = _streamLength;
                } while (Interlocked.CompareExchange(ref _cursor, newCursor, tCursor) != tCursor);
            }

            if (origin == SeekOrigin.End)
            {
                newCursor = _streamLength + offset;
                if (newCursor <= 0) newCursor = 0;
                if (newCursor > _streamLength) newCursor = _streamLength;
                Interlocked.Exchange(ref _cursor, newCursor);
            }
            return newCursor;
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            if (value > 0) _streamLength = value;
            if (_cursor > _streamLength) _cursor = Interlocked.Read(ref _streamLength);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}

