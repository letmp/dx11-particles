#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using System.Timers;
#endregion usings

namespace DX11.Particles.IO
{

    #region ChunkDataManager
    public class ChunkDataManager
    {
        [Import()]
        public ILogger FLogger;
        
        public Dictionary<string, int> dataStructure = new Dictionary<string, int>();
        public Vector3D boundsMin;
        public Vector3D boundsMax;
        public int elementCount;
        public int maxElementCount;
        public Vector3D chunkSize;
        public List<int> chunkCount;
        int digits = 0;
        
        public bool fileOpened = false;
        private double cachingProgress = 0;

        private List<ChunkData> chunkDataList = new List<ChunkData>();
        
        public ChunkDataManager(){}

        public Dictionary<string, int> SetDataStructure(string dataStructureString)
        {
            Dictionary<string, int> dataStructure = new Dictionary<string, int>();
            if (dataStructureString.Contains("x")) dataStructure.Add("x", dataStructureString.IndexOf("x"));
            if (dataStructureString.Contains("y")) dataStructure.Add("y", dataStructureString.IndexOf("y"));
            if (dataStructureString.Contains("z")) dataStructure.Add("z", dataStructureString.IndexOf("z"));
            if (dataStructureString.Contains("r")) dataStructure.Add("r", dataStructureString.IndexOf("r"));
            if (dataStructureString.Contains("g")) dataStructure.Add("g", dataStructureString.IndexOf("g"));
            if (dataStructureString.Contains("b")) dataStructure.Add("b", dataStructureString.IndexOf("b"));
            if (dataStructureString.Contains("a")) dataStructure.Add("a", dataStructureString.IndexOf("a"));
            this.dataStructure = dataStructure;
            return dataStructure;
        }

        public Vector3D SetBounds( string[] boundsStrings, bool isMin)
        {
            double x = double.Parse(boundsStrings[1]);
            double y = double.Parse(boundsStrings[2]);
            double z = double.Parse(boundsStrings[3]);
            Vector3D vec = new Vector3D(x, y, z);
            if (isMin) this.boundsMin = vec;
            else this.boundsMax = vec;
            return vec;
        }

        public int SetElementCount(string[] elementCountStrings)
        {
            this.elementCount = int.Parse(elementCountStrings[1]);
            return this.elementCount;
        }

        public int SetMaxElementCount(string[] maxElementCountStrings)
        {
           this.maxElementCount = int.Parse(maxElementCountStrings[1]);
            return this.maxElementCount;
        }

        public Vector3D SetChunkSize( string[] chunkSizeString)
        {
            this.chunkSize = new Vector3D(double.Parse(chunkSizeString[1]), double.Parse(chunkSizeString[2]), double.Parse(chunkSizeString[3]));
            return this.chunkSize;
        }

        public List<int> SetChunkCount(string[] chunkCountStrings)
        {
            this.chunkCount = new List<int> {   Int32.Parse(chunkCountStrings[1], CultureInfo.InvariantCulture),
                                                Int32.Parse(chunkCountStrings[2], CultureInfo.InvariantCulture),
                                                Int32.Parse(chunkCountStrings[3], CultureInfo.InvariantCulture) };

            this.digits = Math.Max(Math.Max(chunkCount[0].ToString().Length, chunkCount[1].ToString().Length), chunkCount[2].ToString().Length);
            return this.chunkCount;
        }

        public IEnumerable<int> getChunkIds(IEnumerable<Vector3D> positions)
        {
            foreach (Vector3D position in positions)
            {
                int chunkIdX = Convert.ToInt32(Math.Floor(position.x / chunkSize.x));
                if (chunkIdX < 0 || chunkIdX > chunkCount[0] - 1) break;
                int chunkIdY = Convert.ToInt32(Math.Floor(position.y / chunkSize.y));
                if (chunkIdY < 0 || chunkIdY > chunkCount[1] - 1) break;
                int chunkIdZ = Convert.ToInt32(Math.Floor(position.z / chunkSize.z));
                if (chunkIdZ < 0 || chunkIdZ > chunkCount[2] - 1) break;

                int chunkId = chunkIdX +
                                chunkIdY * chunkCount[0] +
                                chunkIdZ * chunkCount[0] * chunkCount[1];
                yield return chunkId;
            }
        }

        public void InitChunkDataList( string directory)
        {
            List<ChunkData> chunkDataList = new List<ChunkData>();

            for (int z = 0; z < chunkCount[2]; z++)
            {
                for (int y = 0; y < chunkCount[1]; y++)
                {
                    for (int x = 0; x < chunkCount[0]; x++)
                    {
                        int chunkId = x +
                                        y * chunkCount[0] +
                                        z * chunkCount[0] * chunkCount[1];

                        string fileName = x.ToString("D" + digits) + "_" + y.ToString("D" + digits) + "_" + z.ToString("D" + digits) + ".chunk";
                        string filePath = Path.Combine(directory, fileName);

                        ChunkData cd = new ChunkData(filePath, chunkId, this.dataStructure);
                        chunkDataList.Add(cd);
                    }
                }
            }

            this.chunkDataList = chunkDataList;
            this.cachingProgress = 0;
        }

        public void DisposeChunkDataList()
        {
            foreach (var cd in this.chunkDataList)
                DisposeChunkData(cd);

            chunkDataList.Clear();
            this.cachingProgress = 0;
        }

        public void DisposeChunkData(ChunkData chunkData)
        {
            try
            {
                chunkData.Dispose();
            }
            catch (Exception e)
            {
                FLogger.Log(e);
            }
        }
        
        public void Cache(int chunkId)
        {
            this.chunkDataList[chunkId].Cache();
        }

        public void CacheAll()
        {
            foreach (ChunkData cd in this.chunkDataList) cd.Cache();
        }

        public double GetCachingProgress()
        {
            if ( this.cachingProgress == 1.0) return this.cachingProgress;
            int cachedCount = 0;
            foreach (ChunkData cd in this.chunkDataList)
            {
                if (cd.IsCompleted) cachedCount++;
            }
            return Convert.ToDouble(cachedCount) / Convert.ToDouble(chunkDataList.Count);
        }

        public ChunkData GetChunkData(int chunkId)
        {
            return this.chunkDataList[chunkId];
        }
    }
    #endregion ChunkDataManager

    #region ChunkData
    public class ChunkData : IDisposable
    {
        private const int DefaultBufferSize = 4096;
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        private int chunkId;
        private string filePath;
        private Dictionary<string, int> dataStructure = new Dictionary<string, int>();

        private Task ReadTask;
        private CancellationTokenSource CancellationTokenSource;
        private List<Tuple<Vector3D, RGBAColor>> chunkData = new List<Tuple<Vector3D, RGBAColor>>();
           	
        public ChunkData(string filePath, int chunkId, Dictionary<string, int> dataStructure)
        {
            this.chunkId = chunkId;
            this.filePath = filePath;
            this.dataStructure = dataStructure;
        }

        public int GetListSize()
        {
            return this.chunkData.Count;
        }

        public List<Tuple<Vector3D, RGBAColor>> GetChunkData()
        {
            return chunkData.ToList();
        }

        public List<Tuple<Vector3D, RGBAColor>> GetChunkData(int dataOffset, int limit)
        {
            int range = 0;
            if (limit == -1) range = chunkData.Count - dataOffset;
            else range = Math.Min(chunkData.Count - dataOffset, limit);

            List<Tuple<Vector3D, RGBAColor>> chunkDataCopy = chunkData.GetRange(dataOffset, range);
            return chunkDataCopy;
        }

        public List<Tuple<Vector3D, RGBAColor>> GetChunkData(int dataOffset, int limit, int nth)
        {
            int range = 0;
            dataOffset = Math.Min(chunkData.Count, dataOffset);
            
            if (limit == -1) range = chunkData.Count - dataOffset;
            else range = Math.Min(chunkData.Count - dataOffset, limit);

            List<Tuple<Vector3D, RGBAColor>> chunkDataCopy = chunkData.GetRange(dataOffset, range).Where((x, i) => i % nth == 0).ToList();
            return chunkDataCopy;
        }

        public void Cache()
        {
            if (!this.IsCompleted && !this.IsRunning)
            {
                CancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = CancellationTokenSource.Token;

                ReadTask = Task.Run(
                    async () =>
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
                        using (var reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                if (line.Length > 0)
                                {
                                    Char delimiter = ' ';
                                    String[] substrings = line.Split(delimiter);
                                    double x = 0; double y = 0; double z = 0;
                                    if (dataStructure.ContainsKey("x")) x = double.Parse(substrings[dataStructure["x"]], CultureInfo.InvariantCulture);
                                    if (dataStructure.ContainsKey("y")) y = double.Parse(substrings[dataStructure["y"]], CultureInfo.InvariantCulture);
                                    if (dataStructure.ContainsKey("z")) z = double.Parse(substrings[dataStructure["z"]], CultureInfo.InvariantCulture);
                                    double r = 1; double g = 1; double b = 1; double a = 1;
                                    if (dataStructure.ContainsKey("r")) r = double.Parse(substrings[dataStructure["r"]], CultureInfo.InvariantCulture);
                                    if (dataStructure.ContainsKey("g")) g = double.Parse(substrings[dataStructure["g"]], CultureInfo.InvariantCulture);
                                    if (dataStructure.ContainsKey("b")) b = double.Parse(substrings[dataStructure["b"]], CultureInfo.InvariantCulture);
                                    if (dataStructure.ContainsKey("a")) a = double.Parse(substrings[dataStructure["a"]], CultureInfo.InvariantCulture);
                                    Vector3D position = new Vector3D(x, y, z);
                                    RGBAColor color = new RGBAColor(r, g, b, a);
                                    chunkData.Add(new Tuple<Vector3D, RGBAColor>(position, color));
                                }
                            }
                        }
                    },
                    CancellationTokenSource.Token
                    ).ContinueWith(tsk => {
                    //this.isCached = true;
                });
            }
            
        }
        public bool IsRunning { get { return ReadTask != null; } }
        public bool IsCompleted { get { return ReadTask != null && ReadTask.IsCompleted; } }

        public void Dispose()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
            }
            if (ReadTask != null)
            {
                try
                {
                    ReadTask.Wait();
                }
                catch (AggregateException ae)
                {
                    ae.Handle(e => e is OperationCanceledException);
                }
                finally
                {
                    ReadTask.Dispose();
                    ReadTask = null;
                    chunkData.Clear();
                }
            }
        }
    }
    #endregion ChunkData

    #region ChunkAccessData
    public class ChunkAccessData
    {
        public int chunkId;
        public int dataOffset = 0;
        public bool isLocked = false;
        
        System.Timers.Timer timer;

        public ChunkAccessData(int chunkId) {
            this.chunkId = chunkId;
        }

        public void StartLock(double lockTime)
        {
            if (lockTime > 0)
            {
                timer = new System.Timers.Timer();
                timer.Interval = lockTime;
                timer.Elapsed += Unlock;
                timer.Enabled = true;
                this.isLocked = true;
            }
        }

        private void Unlock(object sender, ElapsedEventArgs e)
        {
            this.isLocked = false;
        }

    }
    #endregion ChunkAccessData
}
