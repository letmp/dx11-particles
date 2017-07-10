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

    class ChunkImporterPLY : ChunkImporterBase
    {

        ChunkManager _chunkManager;

        public int skipLines = 0;
        public const string PLY_FORMAT_ASCII = "PLY_FORMAT_ASCII";
        public const string PLY_FORMAT_BE = "PLY_FORMAT_BE";
        public const string PLY_FORMAT_LE = "PLY_FORMAT_LE";
        public string format;

        public ChunkImporterPLY(ChunkManager chunkManager) : base(chunkManager)
        {
            _chunkManager = chunkManager;
        }

        protected override async Task ParseFile()
        {

            FLogger.Log(LogType.Message, "ChunkImporter: Analyzing Data");
            IOMessages.CurrentState = "Analyzing Data";
            try
            {
                using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
                using (var reader = new StreamReader(stream))
                {
                    string line;

                    Vector3D boundsMin = new Vector3D();
                    Vector3D boundsMax = new Vector3D();

                    bool firstLine = true;
                    bool header = true;
                    
                    string dataStructureString = "";
                    int lineCounter = 0;
                    Char delimiter = ' ';

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        
                        String[] lineStrings = line.Split(delimiter);

                        if (header)
                        {
                            if (lineStrings[0] == "format") {
                                if(lineStrings[1] == "ascii") format = PLY_FORMAT_ASCII;
                                if (lineStrings[1] == "binary_little_endian") format = PLY_FORMAT_LE;
                                if (lineStrings[1] == "binary_big_endian ") format = PLY_FORMAT_BE;
                            }

                            if (lineStrings[0] == "element" && lineStrings[1] == "vertex") Lines = int.Parse(lineStrings[2]);
                            if(lineStrings[0] == "property")
                            {
                                if (lineStrings[2] == "x") dataStructureString += "x";
                                else if (lineStrings[2] == "y") dataStructureString += "y";
                                else if (lineStrings[2] == "z") dataStructureString += "z";
                                else if (lineStrings[2] == "red") dataStructureString += "r";
                                else if (lineStrings[2] == "green") dataStructureString += "g";
                                else if (lineStrings[2] == "blue") dataStructureString += "b";
                                else dataStructureString += "_";
                            }
                            
                            if (lineStrings[0] == "end_header")
                            {
                                header = false;
                                SetDataStructure(dataStructureString);
                            }
                        }
                        else
                        {

                            if (format != PLY_FORMAT_ASCII) throw new FormatException("PLY files in binary format are not supported.");

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

                            lineCounter++;
                            if (lineCounter == Lines) break; // all vertices are parsed now -> break
                        }
                    }
                    BoundsMax = boundsMax;
                    BoundsMin = boundsMin;
                }
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Error, e.ToString());
                IOMessages.CurrentState = e.ToString();
            }
            
        }

        protected override async Task ImportData()
        {
            
            FLogger.Log(LogType.Message, "ChunkImporter: Importing Data");
            IOMessages.CurrentState = "Importing Data";
            
            try
            {

                if (format != PLY_FORMAT_ASCII) throw new FormatException("PLY files in binary format are not supported.");

                using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultFileOptions))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    bool header = true;
                    Char delimiter = ' ';

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        
                        String[] lineStrings = line.Split(delimiter);

                        if (header)
                        {
                            if (lineStrings[0] == "end_header") header = false;
                        }
                        else
                        {
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


                            if (chunkIndex >= 0 && chunkIndex < _chunkManager.ChunkList.Count)
                            {
                                Chunk chunk = _chunkManager.ChunkList[chunkIndex];
                                chunk.BinaryWriter.Write(particleData.GetByteArray());
                                chunk.UpdateElementCount();
                            }

                            LinesProcessed++; // update count of processed lines -> needed to calculate progress
                            if (LinesProcessed == Lines) break; // all vertices are parsed now -> break
                        }
                    }

                    _chunkManager.UpdateElementCount();
                    IOMessages.CurrentState = "Finished";
                }
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Error, e.ToString());
                IOMessages.CurrentState = e.ToString();
            }

        }
    }
}
