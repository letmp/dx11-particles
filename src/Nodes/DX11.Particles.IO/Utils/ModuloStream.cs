using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VVVV.Utils.VMath;

namespace System.IO
{
    public class ModuloStream : ComStream
    {
        private readonly Stream stream;
        private readonly int mod;
        private readonly int modOffset;
        private readonly int bytesPerElement;
        private long position;

        public ModuloStream(Stream stream, int bytesPerElement, int mod, int modOffset)
        {
            this.stream = stream;
            this.bytesPerElement = bytesPerElement;
            this.mod = mod;
            this.modOffset = modOffset;            
        }

        public override bool CanRead
        {
            get { return this.stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override long Length
        {
             
            get { return (long)Math.Ceiling((double)(this.stream.Length / bytesPerElement) / mod) * bytesPerElement; }
        }

        public override long Position
        {
            get { return this.position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            
            this.stream.Position = this.position * mod;

            var bytesToRead = count;
            while (bytesToRead > 0)
            {
                if (bytesToRead < bytesPerElement) break;

                while (Math.Floor((double)stream.Position / (double)bytesPerElement)% mod != modOffset && stream.Position < stream.Length) stream.Position += bytesPerElement;

                var bytesRead = stream.Read(buffer, offset + totalBytesRead, bytesPerElement);
                if (bytesRead == 0) break;

                this.Position += bytesRead;
                bytesToRead -= bytesRead;
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    this.position = VMath.Clamp(offset, 0, this.Length);
                    break;
                case SeekOrigin.Current:
                    this.position = VMath.Clamp(this.position + offset, 0, this.Length);
                    break;
                case SeekOrigin.End:
                    this.position = VMath.Clamp(this.Length - offset, 0, this.Length);
                    break;
            }
            return this.position;
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