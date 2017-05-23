using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO
{
    public class AggregatedStreamAdvanced : ComStream
    {
        private readonly List<Tuple<long, Stream>> cache = new List<Tuple<long, Stream>>();
        private long length;
        private int BytesPerElement;
        private int EveryNth;

        public AggregatedStreamAdvanced(IEnumerable<Stream> streams, int bytesPerElement, int everyNth)
        {
            foreach (var s in streams)
            {
                var t = Tuple.Create(s.Length, s);
                this.length += s.Length;
                this.cache.Add(t);
            }

            //this.length = (long)Math.Round((double)(length / bytesPerElement) / everyNth , MidpointRounding.AwayFromZero) * bytesPerElement;
            this.length = (long)Math.Ceiling((double)(length / bytesPerElement) / everyNth) * bytesPerElement;

            BytesPerElement = bytesPerElement;
            EveryNth = everyNth;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { return this.length; }
        }

        public override long Position
        {
            get;
            set;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long streamOffset = 0;
            var totalBytesRead = 0;
            foreach (var tuple in this.cache)
            {
                var streamLength = tuple.Item1;
                var stream = tuple.Item2;

                if ((streamOffset <= (Position * EveryNth)) && ((Position * EveryNth) < streamOffset + streamLength))
                {
                    stream.Position = (Position * EveryNth) - streamOffset;
                    var bytesToRead = count - totalBytesRead;

                    while (bytesToRead > 0)
                    {
                        if (bytesToRead < BytesPerElement) break;

                        while (Math.Floor((double)stream.Position / (double)BytesPerElement) % EveryNth != 0) stream.Position++;
                        
                        var bytesRead = stream.Read(buffer, offset + totalBytesRead, BytesPerElement);
                        if (bytesRead == 0) break;
                        Position += bytesRead;
                        bytesToRead -= bytesRead;
                        totalBytesRead += bytesRead;

                    }
                }
                streamOffset += streamLength;
            }
            return totalBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}