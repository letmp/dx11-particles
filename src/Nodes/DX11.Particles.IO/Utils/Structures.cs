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
}
