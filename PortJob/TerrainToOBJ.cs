using CommonFunc;
using static CommonFunc.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class TerrainToOBJ {
        /* Converts terraindata into an OBJ */
        /* OBJ is then converted into an hkx by an external program */
        public static void convert(string objPath, Cell cell) {
            Obj obj = new();

            /* Sanity check */
            if(cell.terrain.Count < 1) {
                throw new Exception("I really hope this never happens!");
            }

            foreach (TerrainData terrain in cell.terrain) {
                ObjG g = new();
                g.name = terrain.name;
                g.mtl = "hkm_Cobblestone_Safe1";    // Not sure how we are going to define this yet. Just using this material type as a default for now

                /* Add index data first so we can use vertex array sizes as offsets */
                for (int i = 0; i < terrain.indices.Count; i += 3) {
                    List<int> indices = terrain.indices;
                    ObjV[] v = new ObjV[3];
                    for (int j = 0; j < 3; j++) {
                        int vi = indices[i + j] + obj.vs.Count;
                        int vti = 0 + obj.vts.Count;
                        int vni = indices[i + j] + obj.vns.Count;
                        v[j] = new ObjV(vi, vti, vni);
                    }
                    g.fs.Add(new ObjF(v[0], v[1], v[2]));
                }

                /* Add vertex data */
                Vector3 textureCoordinate = Vector3.Zero; // We don't need texture coordinates in collision data, so we just write a single zero and point to that
                obj.vts.Add(textureCoordinate);

                foreach (TerrainVertex vertex in terrain.vertices) {
                    // Get position and transform it
                    Vector3 position = new(-vertex.position.X, vertex.position.Y, vertex.position.Z); // X is flipped. Don't know why but it is correct and we do it in all other model conversions as well.

                    // Get normal and rotate it (x is flipped so normals have to be rotated to match)
                    Matrix4x4 normalRotMatrixX = Matrix4x4.CreateRotationX((float)-Math.PI / 2f);       // Accounting for -X and ZY swap (i assume, ask meow lol)
                    Matrix4x4 normalRotMatrixY = Matrix4x4.CreateRotationY((float)Math.PI);             // Accounting for 180 rotation around up axis
                    Vector3 normalInputVector = new(-vertex.normal.X, vertex.normal.Y, vertex.normal.Z);

                    Vector3 rotatedNormal = Vector3.Normalize(
                        Vector3.TransformNormal(
                            Vector3.TransformNormal(normalInputVector, normalRotMatrixX),
                        normalRotMatrixY)
                    );

                    obj.vs.Add(position);
                    obj.vns.Add(rotatedNormal);
                }

                obj.gs.Add(g);
            }
            obj.write(objPath);
        }
    }
}
