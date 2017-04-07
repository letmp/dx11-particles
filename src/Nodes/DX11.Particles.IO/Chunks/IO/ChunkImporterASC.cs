using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VVVV.Utils.VMath;

namespace DX11.Particles.IO.Chunks
{
    class ChunkImporterASC : ChunkImporterBase
    {
        ChunkManager _chunkManager;

        public ChunkImporterASC(ChunkManager chunkManager) : base(chunkManager)
        {
            this._chunkManager = chunkManager;
        }

        public override void ParseBounds()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task Task = Task.Run(
                    async () =>
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
                                    String[] substrings = line.Split(delimiter);
                                    double x = double.Parse(substrings[DataStructure["x"]], CultureInfo.InvariantCulture);
                                    double y = double.Parse(substrings[DataStructure["y"]], CultureInfo.InvariantCulture);
                                    double z = double.Parse(substrings[DataStructure["z"]], CultureInfo.InvariantCulture);

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
                    },
                    cancellationTokenSource.Token
                    ).ContinueWith(tsk =>
                    {
                        ImportChunks();

                        // TODO: http://stackoverflow.com/questions/34026109/run-a-function-when-a-task-finishes
                    });
        }

        public override void ImportChunks()
        {
            Progress = 0; // reset progress
            UpdateOffsets();
            UpdateScaleMultiplier();
            
            // update LinesProcessed
        }

    }
}
