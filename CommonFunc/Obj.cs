using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {

    public class Obj {
        public List<Vector3> vs, vts, vns;
        public List<ObjG> gs;
        public Obj() {
            vs = new();
            vts = new();
            vns = new();
            gs = new();
        }

        /* Takes data in this class and writes an obj file of it to the path specified */
        public void write(string outPath) {
            StringBuilder sb = new();

            /* write vertices */
            sb.Append("## Vertices: "); sb.Append(vs.Count); sb.Append(" ##\r\n");
            foreach (Vector3 v in vs) {
                sb.Append("v  "); sb.Append(v.X); sb.Append(' '); sb.Append(v.Y); sb.Append(' '); sb.Append(v.Z); sb.Append("\r\n");
            }

            /* write texture coordinates */
            sb.Append("\r\n## Texture Coordinates: "); sb.Append(vts.Count); sb.Append(" ##\r\n");
            foreach (Vector3 vt in vts) {
                sb.Append("vt "); sb.Append(vt.X); sb.Append(' '); sb.Append(vt.Y); sb.Append(' '); sb.Append(vt.Z); sb.Append("\r\n");
            }

            /* write vertex normals */
            sb.Append("\r\n## Vertex Normals: "); sb.Append(vns.Count); sb.Append(" ##\r\n");
            foreach (Vector3 vn in vns) {
                sb.Append("vn "); sb.Append(vn.X); sb.Append(' '); sb.Append(vn.Y); sb.Append(' '); sb.Append(vn.Z); sb.Append("\r\n");
            }

            foreach (ObjG g in gs) {
                g.write(sb);
            }

            /* write to file */
            if (File.Exists(outPath)) { File.Delete(outPath); }
            File.WriteAllText(outPath, sb.ToString());
        }
    }

    public class ObjG {
        public string name, mtl;
        public List<ObjF> fs;
        public ObjG() {
            fs = new();
        }
        
        public void write(StringBuilder sb) {
            /* write object name */
            sb.Append("\r\n");
            sb.Append("g "); sb.Append(name);
            sb.Append("\r\n");

            /* write material */
            sb.Append("usemtl "); sb.Append(mtl);

            /* write triangles */
            sb.Append("\r\n## Triangles: "); sb.Append(fs.Count); sb.Append(" ##\r\n");
            foreach (ObjF f in fs) {
                f.write(sb);
            }
            sb.Append("\r\n");
        }
    }

    public class ObjF {
        public ObjV a, b, c;
        public ObjF(ObjV a, ObjV b, ObjV c) {
            this.a = a; this.b = b; this.c = c;
        }

        public void write(StringBuilder sb) {
            sb.Append("f ");
            a.write(sb);
            b.write(sb);
            c.write(sb);
            sb.Append("\r\n");
        }
    }

    public class ObjV {
        public int v, vt, vn;
        public ObjV(int v, int vt, int vn) {
            this.v = v; this.vt = vt; this.vn = vn;
        }

        public void write(StringBuilder sb) {
            sb.Append(v + 1); sb.Append('/'); sb.Append(vt + 1); sb.Append('/'); sb.Append(vn + 1); sb.Append(' ');
        }
    }
}
