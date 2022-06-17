using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob.Solvers
{
    /* Code by Meowmartius, borrowed from FBX2FLVER <3 */
    public class OrientationSolver
    {
        public static void SolveOrientation(SoulsFormats.FLVER2 flver)
        {
            foreach (var flverMesh in flver.Meshes)
            {
                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                {

                    var m = Matrix.Identity
                    * Matrix.CreateRotationY(0) // Set all these to 0 since I don't think we will need scene rotation for what we are doing.
                    * Matrix.CreateRotationZ(0)
                    * Matrix.CreateRotationX(0)
                    ;

                    flverMesh.Vertices[i].Position = Vector3.Transform(new Vector3(flverMesh.Vertices[i].Position.X, flverMesh.Vertices[i].Position.Y, flverMesh.Vertices[i].Position.Z), m).ToNumerics();
                    Vector3 normVec = Vector3.Normalize(Vector3.Transform(new Vector3(flverMesh.Vertices[i].Normal.X, flverMesh.Vertices[i].Normal.Y, flverMesh.Vertices[i].Normal.Z), m));
                    flverMesh.Vertices[i].Normal = new System.Numerics.Vector3(normVec.X, normVec.Y, normVec.Z);
                    if (flverMesh.Vertices[i].Tangents.Count > 0)
                    {
                        var rotBitangentVec3 = Vector3.Transform(new Vector3(flverMesh.Vertices[i].Tangents[0].X, flverMesh.Vertices[i].Tangents[0].Y, flverMesh.Vertices[i].Tangents[0].Z), m);
                        flverMesh.Vertices[i].Tangents[0] = new System.Numerics.Vector4(rotBitangentVec3.X, rotBitangentVec3.Y, rotBitangentVec3.Z, flverMesh.Vertices[i].Tangents[0].W);
                    }
                }
            }

            /* Removed a few things here related to bones and weird overrides for other games */

        }
    }

    public static class ExtensionMethods
    {
        public static System.Numerics.Vector3 ToNumerics(this Microsoft.Xna.Framework.Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }
    }
}
