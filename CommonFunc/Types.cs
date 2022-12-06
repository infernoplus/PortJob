using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public static Int2 operator +(Int2 a, Int2 b) {
            return a.Add(b);
        }

        public Int2 Add(Int2 b) {
            return new Int2(x + b.x, y + b.y);
        }

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

    public class UShort2 {
        public readonly ushort x, y;
        public UShort2(ushort x, ushort y) {
            this.x = x; this.y = y;
        }

        public static bool operator ==(UShort2 a, UShort2 b) {
            return a.Equals(b);
        }
        public static bool operator !=(UShort2 a, UShort2 b) => !(a == b);

        public bool Equals(UShort2 b) {
            return x == b.x && y == b.y;
        }
        public override bool Equals(object a) => Equals(a as UShort2);

        public override int GetHashCode() {
            unchecked {
                int hashCode = x.GetHashCode();
                hashCode = (hashCode * 397) ^ y.GetHashCode();
                return hashCode;
            }
        }

        public ushort[] Array() {
            ushort[] r = { x, y };
            return r;
        }
    }

    public class Byte4 {
        public readonly byte x, y, z, w;
        public Byte4(byte a) {
            x = a; y = a; z = a; w = a;
        }

        public Byte4(int x, int y, int z, int w) {
            
            this.x = (byte)Math.Max(0, Math.Min(Byte.MaxValue, x)); this.y = (byte)Math.Max(0, Math.Min(Byte.MaxValue, y)); this.z = (byte)Math.Max(0, Math.Min(Byte.MaxValue, z)); this.w = (byte)Math.Max(0, Math.Min(Byte.MaxValue, w));
        }

        public Byte4(byte x, byte y, byte z, byte w) {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
    }

    public class TextureKey {
        public string Key, Value;
        public Vector2 uv;
        public byte Unk10;
        public bool Unk11;
        public TextureKey(string k, string v, byte u, bool uu) {
            Key = k; Value = v; Unk10 = u; Unk11 = uu;
            uv = Vector2.One;
        }
        public TextureKey(string k, string v, byte u, bool uu, Vector2 uv) {
            Key = k; Value = v; Unk10 = u; Unk11 = uu;
            this.uv = uv;
        }
    }

    public class FBXConverterJob {
        public string OutputPath, MorrowindPath, TPFDir;
        public List<FBXInfo> FBXList;
        public FBXConverterJob(string OutputPath, string MorrowindPath, string TPFDir, List<FBXInfo> FBXList) {
            this.OutputPath = OutputPath;
            this.MorrowindPath = MorrowindPath;
            this.TPFDir = TPFDir;
            this.FBXList = FBXList;
        }
    }

    public class FBXInfo {
        public string FBXPath { get; }
        public string FlverPath { get; }
        public List<int> Scales;
        public FBXInfo(string fbxPath, string flverPath) {
            FBXPath = fbxPath;
            FlverPath = flverPath;
            Scales = new();
        }

        public void AddScale(float scale) {
            int stepScale = (int)(Math.Round((scale * 100f) / 10) * 10);
            if (Scales.Contains(stepScale)) { return; }
            Scales.Add(stepScale);
        }
    }
}
