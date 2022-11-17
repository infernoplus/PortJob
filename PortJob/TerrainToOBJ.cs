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
        public static void convert(string objPath, TerrainData terrain) {
            Obj obj = new();

            ObjG g = new();
            g.name = terrain.name;
            g.mtl = "hkm_Cobblestone_Safe1";    // Not sure how we are going to define this yet. Just using this material type as a default for now
            
            /* Add index data */
            for(int i=0;i<terrain.indices.Count;i+=3) {
                List<int> indices = terrain.indices;
                ObjV[] v = new ObjV[3];
                for (int j = 0; j < 3; j++) {
                    int vi = indices[i + j] + obj.vs.Count;
                    int vti = 0 + obj.vts.Count;
                    int vni = indices[i + j] + obj.vns.Count;
                    v[j] = new ObjV(vi, vti, vni);
                }
                g.fs.Add(new ObjF(v[2], v[1], v[0]));  // Reverse order because idk, either obj or fbx is backwards. don't know which
            }

            /* Add vertex data */
            Vector3 textureCoordinate = Vector3.Zero; // We don't need texture coordinates in collision data, so we just write a single zero and point to that
            obj.vts.Add(textureCoordinate);

            foreach (TerrainVertex vertex in terrain.vertices) {
                obj.vs.Add(vertex.position);
                obj.vns.Add(vertex.normal);
            }

            obj.gs.Add(g);

            obj.write(objPath);
        }
    }
}
