using DX11.Particles.IO.Utils;
using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using System.ComponentModel.Composition;

namespace DX11.Particles.IO.Chunks
{

    class ChunkImporterASCII : ChunkImporterBase
    {

        ChunkManager _chunkManager;

        public ChunkImporterASCII(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        protected override async Task ParseBounds()
        {
            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
            using (var reader = new StreamReader(stream))
            {
                string line;
                bool firstLine = true;
                Vector3D boundsMin = new Vector3D();
                Vector3D boundsMax = new Vector3D();
                Lines = 0; // needed to calculate progress
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Lines++; // update linecount

                    if (line.Length > 0)
                    {
                        Char delimiter = ' ';
                        String[] lineStrings = line.Split(delimiter);
                        double x = double.Parse(lineStrings[DataStructure["x"]], CultureInfo.InvariantCulture);
                        double y = double.Parse(lineStrings[DataStructure["y"]], CultureInfo.InvariantCulture);
                        double z = double.Parse(lineStrings[DataStructure["z"]], CultureInfo.InvariantCulture);

                        if (firstLine)
                        {
                            boundsMin = new Vector3D(x, y, z);
                            boundsMax = new Vector3D(x, y, z);
                        }
                        else
                        {
                            Vector3D newMinVec = boundsMin;
                            if (newMinVec.x > x) newMinVec.x = x;
                            if (newMinVec.y > y) newMinVec.y = y;
                            if (newMinVec.z > z) newMinVec.z = z;
                            boundsMin = newMinVec;

                            Vector3D newMaxVec = boundsMax;
                            if (newMaxVec.x < x) newMaxVec.x = x;
                            if (newMaxVec.y < y) newMaxVec.y = y;
                            if (newMaxVec.z < z) newMaxVec.z = z;
                            boundsMax = newMaxVec;
                        }
                        firstLine = false;
                    }
                }
                BoundsMax = boundsMax;
                BoundsMin = boundsMin;
            }
        }

        protected override async Task ImportData()
        {
            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    LinesProcessed++; // update count of processed lines -> needed to calculate progress

                    if (line.Length > 0)
                    {
                        Char delimiter = ' ';
                        String[] lineStrings = line.Split(delimiter);

                        ParticleData particleData = new ParticleData();
                        Triple<int, int, int> chunkId = new Triple<int, int, int>();
                        chunkId.x = 0; chunkId.y = 0; chunkId.z = 0;

                        if (DataStructure.ContainsKey("x"))
                        {
                            Single x = Single.Parse(lineStrings[DataStructure["x"]], CultureInfo.InvariantCulture);
                            x += (Single)Offsets.x;
                            x *= (Single)ScaleValue;
                            particleData.x = x;
                            chunkId.x = Convert.ToInt32(Math.Floor((x - BoundsMin.x) / ChunkSize.x));
                            if (chunkId.x < 0) chunkId.x = 0;
                            if (chunkId.x >= ChunkCount.x) chunkId.x = ChunkCount.x - 1;
                        }

                        if (DataStructure.ContainsKey("y"))
                        {
                            Single y = Single.Parse(lineStrings[DataStructure["y"]], CultureInfo.InvariantCulture);
                            y += (Single)Offsets.y;
                            y *= (Single)ScaleValue;
                            particleData.y = y;
                            chunkId.y = Convert.ToInt32(Math.Floor((y - BoundsMin.y) / ChunkSize.y));
                            if (chunkId.y < 0) chunkId.y = 0;
                            if (chunkId.y >= ChunkCount.y) chunkId.y = ChunkCount.y - 1;
                        }

                        if (DataStructure.ContainsKey("z"))
                        {
                            Single z = Single.Parse(lineStrings[DataStructure["z"]], CultureInfo.InvariantCulture);
                            z += (Single)Offsets.z;
                            z *= (Single)ScaleValue;
                            particleData.z = z;
                            chunkId.z = Convert.ToInt32(Math.Floor((z - BoundsMin.z) / ChunkSize.z));
                            if (chunkId.z < 0) chunkId.z = 0;
                            if (chunkId.z >= ChunkCount.z) chunkId.z = ChunkCount.z - 1;
                        }

                        if (DataStructure.ContainsKey("r"))
                        {
                            Single r = Single.Parse(lineStrings[DataStructure["r"]], CultureInfo.InvariantCulture) / 255;
                            particleData.r = r;
                        }

                        if (DataStructure.ContainsKey("g"))
                        {
                            Single g = Single.Parse(lineStrings[DataStructure["g"]], CultureInfo.InvariantCulture) / 255;
                            particleData.g = g;
                        }

                        if (DataStructure.ContainsKey("b"))
                        {
                            Single b = Single.Parse(lineStrings[DataStructure["b"]], CultureInfo.InvariantCulture) / 255;
                            particleData.b = b;
                        }

                        if (DataStructure.ContainsKey("a"))
                        {
                            Single a = Single.Parse(lineStrings[DataStructure["a"]], CultureInfo.InvariantCulture) / 255;
                            particleData.a = a;
                        }

                        int chunkIndex = chunkId.x +
                                            chunkId.y * ChunkCount.x +
                                            chunkId.z * ChunkCount.x * ChunkCount.y;

                        Chunk chunk = _chunkManager.ChunkList[chunkIndex];
                        chunk.BinaryWriter.Write(particleData.GetByteArray());
                        chunk.UpdateElementCount();

                    }

                }

            }
        }
    }
}
