using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace PortJob.Solvers
{
    /* Code by Meowmartius, borrowed from FBX2FLVER <3 */
    public class BoundingBoxSolver
    {
        private static List<FLVER.Bone> GetAllBonesReferencedByVertex(FLVER2 f, FLVER2.Mesh m, FLVER.Vertex v, Dictionary<FLVER.Vertex, List<FLVER.Bone>> pcbl)
        {
            if (!pcbl.ContainsKey(v))
            {
                List<FLVER.Bone> result = new List<FLVER.Bone>();

                for (int i = 0; i < v.BoneIndices.Length; i++)
                {
                    int vertBoneIndex = v.BoneIndices[i];
                    if (vertBoneIndex >= 0)
                    {
/*                        if (Importer.JOBCONFIG.UseDirectBoneIndices) // Don't need this for DS1, just commented out
                        {
                            result.Add(f.Bones[vertBoneIndex]);
                        }
                        else
                        {*/
                            if (m.BoneIndices[vertBoneIndex] >= 0)
                                result.Add(f.Bones[m.BoneIndices[vertBoneIndex]]);
                        /*}*/
                    }
                }

                pcbl.Add(v, result);
            }

            return pcbl[v];
        }

        private static List<FLVER.Vertex> GetVerticesParentedToBone(FLVER2 f, FLVER.Bone b, Dictionary<FLVER.Vertex, List<FLVER.Bone>> pcbl)
        {
            List<FLVER.Vertex> result = new List<FLVER.Vertex>();
            foreach (FLVER2.Mesh sm in f.Meshes)
            {
                foreach (FLVER.Vertex v in sm.Vertices)
                {
                    List<FLVER.Bone> bonesReferencedByThisShit = GetAllBonesReferencedByVertex(f, sm, v, pcbl);
                    if (bonesReferencedByThisShit.Contains(b))
                        result.Add(v);
                }
            }
            return result;
        }

        private static BoundingBox GetBoundingBox(List<Vector3> verts)
        {
            if (verts.Count > 0)
                return BoundingBox.CreateFromPoints(verts);
            else
                return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        private static Matrix GetParentBoneMatrix(FLVER2 f, FLVER.Bone bone)
        {
            FLVER.Bone parent = bone;

            Matrix boneParentMatrix = Matrix.Identity;

            do
            {
                boneParentMatrix *= Matrix.CreateScale(parent.Scale.X, parent.Scale.Y, parent.Scale.Z);
                boneParentMatrix *= Matrix.CreateRotationX(parent.Rotation.X);
                boneParentMatrix *= Matrix.CreateRotationZ(parent.Rotation.Z);
                boneParentMatrix *= Matrix.CreateRotationY(parent.Rotation.Y);

                //boneParentMatrix *= Matrix.CreateRotationY(parent.EulerRadian.Y);
                //boneParentMatrix *= Matrix.CreateRotationZ(parent.EulerRadian.Z);
                //boneParentMatrix *= Matrix.CreateRotationX(parent.EulerRadian.X);
                boneParentMatrix *= Matrix.CreateTranslation(parent.Translation.X, parent.Translation.Y, parent.Translation.Z);
                //boneParentMatrix *= Matrix.CreateScale(parent.Scale);

                if (parent.ParentIndex >= 0)
                {
                    parent = f.Bones[parent.ParentIndex];
                }
                else
                {
                    parent = null;
                }
            }
            while (parent != null);

            return boneParentMatrix;
        }

        private static void SetBoneBoundingBox(FLVER2 f, FLVER.Bone b, Dictionary<FLVER.Vertex, List<FLVER.Bone>> pcbl)
        {
            BoundingBox bb = GetBoundingBox(GetVerticesParentedToBone(f, b, pcbl).Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToList());
            if (bb.Max.LengthSquared() != 0 || bb.Min.LengthSquared() != 0)
            {
                Matrix matrix = GetParentBoneMatrix(f, b);
                b.BoundingBoxMin = Vector3.Transform(bb.Min, Matrix.Invert(matrix)).ToNumerics();
                b.BoundingBoxMax = Vector3.Transform(bb.Max, Matrix.Invert(matrix)).ToNumerics();
            }
            else
            {
                b.BoundingBoxMin = new System.Numerics.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                b.BoundingBoxMax = new System.Numerics.Vector3(float.MinValue, float.MinValue, float.MinValue);
            }
        }

        public static void FixAllBoundingBoxes(FLVER2 f)
        {
            Dictionary<FLVER.Vertex, List<FLVER.Bone>> pcbl = new Dictionary<FLVER.Vertex, List<FLVER.Bone>>();

            foreach (FLVER.Bone b in f.Bones)
            {
                SetBoneBoundingBox(f, b, pcbl);
            }


            List<BoundingBox> submeshBBs = new List<BoundingBox>();

            foreach (FLVER2.Mesh sm in f.Meshes)
            {
                BoundingBox bb = GetBoundingBox(sm.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToList());
                if (bb.Max.LengthSquared() != 0 || bb.Min.LengthSquared() != 0)
                {
                    submeshBBs.Add(bb);
                    sm.BoundingBox = new FLVER2.Mesh.BoundingBoxes();
                    sm.BoundingBox.Min = bb.Min.ToNumerics();
                    sm.BoundingBox.Max = bb.Max.ToNumerics();
                }
                else
                {
                    sm.BoundingBox = null;
                }
            }

            if (submeshBBs.Count > 0)
            {
                BoundingBox finalBB = submeshBBs[0];
                for (int i = 1; i < submeshBBs.Count; i++)
                {
                    finalBB = BoundingBox.CreateMerged(finalBB, submeshBBs[i]);
                }

                f.Header.BoundingBoxMin = new System.Numerics.Vector3(finalBB.Min.X, finalBB.Min.Y, finalBB.Min.Z);
                f.Header.BoundingBoxMax = new System.Numerics.Vector3(finalBB.Max.X, finalBB.Max.Y, finalBB.Max.Z);
            }
            else
            {
                f.Header.BoundingBoxMin = new System.Numerics.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                f.Header.BoundingBoxMax = new System.Numerics.Vector3(float.MinValue, float.MinValue, float.MinValue);
            }



        }
    }
}
