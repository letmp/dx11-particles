using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DX11.Particles.IO.Utils
{
    public class Triple<X, Y, Z>
    {
        public X x { get; set; }
        public Y y { get; set; }
        public Z z { get; set; }
    }
    
    public static class AlignMode
    {
        public const int NONE = 0;
        public const int MIN = 1;
        public const int CENTER = 2;
        public const int MAX = 3;
    }

    public static class ScaleMode
    {
        public const int MULTIPLY = 0;
        public const int MAXX = 1;
        public const int MAXY = 2;
        public const int MAXZ = 3;
    }

    public enum AlignEnum
    {
        None, Min, Center, Max
    }

    public enum ScaleEnum
    {
        Multiply, MaxX, MaxY, MaxZ
    }

    class ParticleData
    {
        string pattern = "";
        Single _x,_y,_z,_r,_g,_b,_a;
        public Single x { get { return _x; } set { _x = value; pattern += "x"; } }
        public Single y { get { return _y; } set { _y = value; pattern += "y"; } }
        public Single z { get { return _z; } set { _z = value; pattern += "z"; } }
        public Single r { get { return _r; } set { _r = value; pattern += "r"; } }
        public Single g { get { return _g; } set { _g = value; pattern += "g"; } }
        public Single b { get { return _b; } set { _b = value; pattern += "b"; } }
        public Single a { get { return _a; } set { _a = value; pattern += "a"; } }

        public ParticleData() {}

        public Single[] GetValueArray()
        {
            List<Single> valueList = new List<Single>();
            if (pattern.Contains("x")) valueList.Add(x);
            if (pattern.Contains("y")) valueList.Add(y);
            if (pattern.Contains("z")) valueList.Add(z);
            if (pattern.Contains("r")) valueList.Add(r);
            if (pattern.Contains("g")) valueList.Add(g);
            if (pattern.Contains("b")) valueList.Add(b);
            if (pattern.Contains("a")) valueList.Add(a);
            return valueList.ToArray();
        }

        public byte[] GetByteArray()
        {
            Single[] valueArray = GetValueArray();
            byte[] byteArray = new byte[valueArray.Length * sizeof(Single)];
            Buffer.BlockCopy(valueArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
        
    }
}
