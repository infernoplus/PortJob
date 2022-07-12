using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {
    public class Box {
        public int x1, y1, x2, y2;
        public Box(int x1, int y1, int x2, int y2) {
            this.x1 = x1; this.y1 = y1;
            this.x2 = x2; this.y2 = y2;
        }
    }

    public class Int2 {
        public readonly int x, y;
        public Int2(int x, int y) {
            this.x = x; this.y = y;
        }

        public static bool operator ==(Int2 a, Int2 b) {
            return a.Equals(b);
        }
        public static bool operator !=(Int2 a, Int2 b) => !(a == b);

        public bool Equals(Int2 b) {
            return x == b.x && y == b.y;
        }
        public override bool Equals(object a) => Equals(a as Int2);

        public override int GetHashCode() {
            unchecked {
                int hashCode = x.GetHashCode();
                hashCode = (hashCode * 397) ^ y.GetHashCode();
                return hashCode;
            }
        }

        public int[] Array() {
            int[] r = { x, y };
            return r;
        }
    }
}
