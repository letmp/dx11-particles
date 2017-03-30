using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.VMath;

namespace DX11.Particles.IO
{
    public interface IChunkDataImporter
    {
        Vector3D BoundsMin { get; set; }
        Vector3D BoundsMax { get; set; }
        int ElementCount { get; set; }
        int MaxElementCount { get; set; }
        Vector3D ChunkSize { get; set; }
        List<int> ChunkCount { get; set; }
        double CachingProgress { get; set; }

        List<ChunkData> ChunkDataList { get; set; }

        void Import( string filePath );
    }

    public abstract class ChunkDataImporterBase : IChunkDataImporter
    {
        public abstract void Import(string filePath);
        
        public double CachingProgress
        {
            get {
                int count = ChunkCount[0] * ChunkCount[1] * ChunkCount[2];
                return ChunkDataList.Count / count;
            }
            set { CachingProgress = value; }
        }

        public List<ChunkData> ChunkDataList
        {
            get { return ChunkDataList; }
            set { ChunkDataList = value; }
        }

        public Vector3D BoundsMin
        {
            get { return BoundsMin; }
            set { BoundsMin = value; }
        }

        public Vector3D BoundsMax
        {
            get{ return BoundsMax;}
            set { BoundsMax = value; }
        }
        
        public List<int> ChunkCount
        {
            get { return ChunkCount; }
            set { ChunkCount = value; }
        }
        
        public Vector3D ChunkSize
        {
            get { return ChunkSize; }
            set { ChunkSize = value; }
        }

        public int ElementCount
        {
            get { return ElementCount; }
            set { ElementCount = value; }
        }

        public int MaxElementCount
        {
            get { return MaxElementCount; }
            set { MaxElementCount = value; }
        }
    }
    
}
