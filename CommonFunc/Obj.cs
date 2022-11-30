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

        /* @TODO: it might be a good idea (not sure though) to write a method that optimizes the obj by 'welding' vertices. basically just look for duplicate vertex data and remove it + adjust indices */
        public Obj optimize() { return null; }

        /* Return a new obj (deep copy) that is identical to the current obj but scaled to the given scale value (vertex scale) */
        public Obj scale(float scale) {
            Obj nu = new();
            foreach (Vector3 v in vs) { nu.vs.Add(new Vector3(v.X, v.Y, v.Z) * scale); }
            foreach (Vector3 vt in vts) { nu.vts.Add(new Vector3(vt.X, vt.Y, vt.Z)); }
            foreach (Vector3 vn in vns) { nu.vns.Add(new Vector3(vn.X, vn.Y, vn.Z)); }
            foreach (ObjG g in gs) {
                ObjG nug = new();
                nug.mtl = g.mtl;
                nug.name = g.name;
                foreach (ObjF f in g.fs) {
                    ObjV nua = new(f.a.v, f.a.vt, f.a.vn);
                    ObjV nub = new(f.b.v, f.b.vt, f.b.vn);
                    ObjV nuc = new(f.c.v, f.c.vt, f.c.vn);
                    ObjF nuf = new(nua, nub, nuc);
                    nug.fs.Add(nuf);
                }
                nu.gs.Add(nug);
            }
            return nu;
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
