using DX11.Particles.IO.Chunks;
using DX11.Particles.IO.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.VMath;

namespace DX11.Particles.IO
{
    public class ChunkManager
    {
        public string ProjectName;

        public Vector3D ChunkSize;
        public Triple<int, int, int> ChunkCount;
        public Vector3D BoundsMin;
        public Vector3D BoundsMax;

        public static int Bytes = sizeof(Single); // all data is stored as single
        public int FieldCountXYZ = 3; // xyz = 3
        public int FieldCountColor; // has to be set. in most cases rgb = 3 or rgba = 4

        public ChunkImporterBase ChunkImporter;
        public ChunkReaderBase ChunkReader;
        public ChunkWriterBase ChunkWriter;

        public List<Chunk> ChunkList;
        
        public ChunkManager() { }

        private int GetLeadingZeroes()
        {
            return Math.Max(Math.Max(ChunkCount.x.ToString().Length, ChunkCount.y.ToString().Length), ChunkCount.z.ToString().Length);
        }

        public int GetElementCount()
        {
            int count = 0;
            foreach (Chunk chunk in ChunkList)
            {
                count += chunk.ElementCount;
            }
            return count;
        }

        public void InitChunkList()
        {
            List<Chunk> chunkList = new List<Chunk>();
            int leadingZeroes = GetLeadingZeroes();
            int bytesPerElement = Bytes * (FieldCountXYZ + FieldCountColor);
            for (int z = 0; z < ChunkCount.z; z++)
            {
                for (int y = 0; y < ChunkCount.y; y++)
                {
                    for (int x = 0; x < ChunkCount.x; x++)
                    {
                        int id = x +
                                        y * ChunkCount.x +
                                        z * ChunkCount.x * ChunkCount.y;

                        string fileName = x.ToString("D" + leadingZeroes) + "_" + y.ToString("D" + leadingZeroes) + "_" + z.ToString("D" + leadingZeroes) + ".chunk";

                        Chunk chunk = new Chunk(id,fileName,bytesPerElement);
                        chunkList.Add(chunk);
                    }
                }
            }
            ChunkList = chunkList;
        }

    }
}
