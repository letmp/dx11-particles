#region usings
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
#endregion usings

namespace DX11.Particles.IO
{

    public class Chunk
    {
        #region Fields
        private const int DefaultBufferSize = 4096;
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        
        public int Id;
        public string FileName;

        public int ElementCount; // number of elements in this chunk
        public int BytesPerElement; // one element consists of sizeof(single) * xyzrgb(a) bytes
        
        public MemoryComStream MemoryStream;
        public BinaryWriter BinaryWriter;
        #endregion Fields

        public Chunk(int id, string fileName, int bytesPerElement)
        {
            Id = id;
            FileName = fileName;
            BytesPerElement = bytesPerElement;
            MemoryStream = new MemoryComStream();
            BinaryWriter = new BinaryWriter(this.MemoryStream);
        }

        public void UpdateElementCount()
        {
            ElementCount = Convert.ToInt32(MemoryStream.Length / BytesPerElement);
        }
        
    }

}
