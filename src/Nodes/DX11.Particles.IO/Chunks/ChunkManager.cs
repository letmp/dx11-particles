using DX11.Particles.IO.Utils;
using System;
using System.Collections.Generic;
using VVVV.Utils.VMath;

namespace DX11.Particles.IO
{
    #region ChunkManager
    public class ChunkManager
    {
        public string ProjectName;

        public Vector3D ChunkSize;
        public Triple<int, int, int> ChunkCount;
        public Vector3D BoundsMin;
        public Vector3D BoundsMax;
        
        public static int Bytes = sizeof(Single); // all data is stored as single
        public string DataStructure; // f.e. "xyzrgb"
        public int BytesPerElement;

        public ChunkImporterBase ChunkImporter;
        public ChunkReaderBase ChunkReader;
        public ChunkWriterBase ChunkWriter;

        public List<Chunk> ChunkList;
        public int ElementCount;
        
        public ChunkManager() { }

        private int GetLeadingZeroes()
        {
            return Math.Max(Math.Max(ChunkCount.x.ToString().Length, ChunkCount.y.ToString().Length), ChunkCount.z.ToString().Length);
        }

        public void UpdateElementCount()
        {
            ElementCount = 0;
            foreach (Chunk chunk in ChunkList)
            {
                ElementCount += chunk.ElementCount;
            }
        }

        public Chunk GetChunk(int chunkId)
        {
            return ChunkList[chunkId];
        }

        public void InitChunkList()
        {
            List<Chunk> chunkList = new List<Chunk>();
            int leadingZeroes = GetLeadingZeroes();
            BytesPerElement = Bytes * (DataStructure.Length);

            for (int z = 0; z < ChunkCount.z; z++)
            {
                for (int y = 0; y < ChunkCount.y; y++)
                {
                    for (int x = 0; x < ChunkCount.x; x++)
                    {
                        int id = x +
                                        y * ChunkCount.x +
                                        z * ChunkCount.x * ChunkCount.y;

                        string fileName = x.ToString("D" + leadingZeroes) + "_" + y.ToString("D" + leadingZeroes) + "_" + z.ToString("D" + leadingZeroes) + ".bin";

                        Chunk chunk = new Chunk(id, fileName, BytesPerElement);
                        chunkList.Add(chunk);
                    }
                }
            }
            ChunkList = chunkList;
        }

    }
    #endregion ChunkManager

    
}
