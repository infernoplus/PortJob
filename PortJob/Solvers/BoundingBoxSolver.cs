using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using static SoulsFormats.GRASS;
using Vector3 = System.Numerics.Vector3;
using NMatrix = System.Numerics.Matrix4x4;
using NVector3 = System.Numerics.Vector3;
using NQuaternion = System.Numerics.Quaternion;

namespace PortJob.Solvers {
    /* Code by Meowmartius, borrowed from FBX2FLVER <3 */
    public static class BoundingBoxSolver {
        public static void FixAllBoundingBoxes(FLVER2 flver) {
            flver.Header.BoundingBoxMin = new System.Numerics.Vector3();
            flver.Header.BoundingBoxMax = new System.Numerics.Vector3();
            foreach (FLVER.Bone bone in flver.Bones) {
                bone.BoundingBoxMin = new System.Numerics.Vector3();
                bone.BoundingBoxMax = new System.Numerics.Vector3();
            }

            for (int i = 0; i < flver.Meshes.Count; i++) {

                FLVER2.Mesh mesh = flver.Meshes[i];
                if (mesh.BoundingBox != null)
                    mesh.BoundingBox = new FLVER2.Mesh.BoundingBoxes();

                foreach (FLVER.Vertex vertex in mesh.Vertices) {
                    flver.Header.UpdateBoundingBox(vertex.Position);
                    if (mesh.BoundingBox != null)
                        mesh.UpdateBoundingBox(vertex.Position);

                    for (int j = 0; j < vertex.BoneIndices.Length; j++) {
                        var boneIndex = vertex.BoneIndices[j];
                        var boneDoesNotExist = false;

                        // Mark bone as not-dummied-out since there is geometry skinned to it.
                        if (boneIndex >= 0 && boneIndex < flver.Bones.Count) {
                            flver.Bones[boneIndex].Unk3C = 0;
                        } else {
                            boneDoesNotExist = true;
                        }

                        if (!boneDoesNotExist)
                            flver.Bones[boneIndex].UpdateBoundingBox(flver.Bones, vertex.Position);
                    }
                }

            }
        }
    }

    public static class ExtensionMethods {
        public static System.Numerics.Vector3 ToNumerics(this Vector4 v) {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Vector3 ToNumerics(this Vector3 v) {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static void UpdateBoundingBox(this FLVER2.FLVERHeader header, NVector3 vertexPos) {
            var minX = Math.Min(header.BoundingBoxMin.X, vertexPos.X);
            var minY = Math.Min(header.BoundingBoxMin.Y, vertexPos.Y);
            var minZ = Math.Min(header.BoundingBoxMin.Z, vertexPos.Z);
            var maxX = Math.Max(header.BoundingBoxMax.X, vertexPos.X);
            var maxY = Math.Max(header.BoundingBoxMax.Y, vertexPos.Y);
            var maxZ = Math.Max(header.BoundingBoxMax.Z, vertexPos.Z);
            header.BoundingBoxMin = new NVector3(minX, minY, minZ);
            header.BoundingBoxMax = new NVector3(maxX, maxY, maxZ);
        }

        public static void UpdateBoundingBox(this FLVER2.Mesh mesh, NVector3 vertexPos) {
            var minX = Math.Min(mesh.BoundingBox.Min.X, vertexPos.X);
            var minY = Math.Min(mesh.BoundingBox.Min.Y, vertexPos.Y);
            var minZ = Math.Min(mesh.BoundingBox.Min.Z, vertexPos.Z);
            var maxX = Math.Max(mesh.BoundingBox.Max.X, vertexPos.X);
            var maxY = Math.Max(mesh.BoundingBox.Max.Y, vertexPos.Y);
            var maxZ = Math.Max(mesh.BoundingBox.Max.Z, vertexPos.Z);
            mesh.BoundingBox.Min = new NVector3(minX, minY, minZ);
            mesh.BoundingBox.Max = new NVector3(maxX, maxY, maxZ);
        }

        public static void UpdateBoundingBox(this FLVER.Bone b, List<FLVER.Bone> bones, NVector3 vertexPos) {
            var boneAbsoluteMatrix = b.GetAbsoluteNMatrix(bones);

            if (NMatrix.Invert(boneAbsoluteMatrix, out NMatrix invertexBoneMat)) {
                var posForBBox = NVector3.Transform(vertexPos, invertexBoneMat);

                var minX = Math.Min(b.BoundingBoxMin.X, posForBBox.X);
                var minY = Math.Min(b.BoundingBoxMin.Y, posForBBox.Y);
                var minZ = Math.Min(b.BoundingBoxMin.Z, posForBBox.Z);
                var maxX = Math.Max(b.BoundingBoxMax.X, posForBBox.X);
                var maxY = Math.Max(b.BoundingBoxMax.Y, posForBBox.Y);
                var maxZ = Math.Max(b.BoundingBoxMax.Z, posForBBox.Z);

                b.BoundingBoxMin = new NVector3(minX, minY, minZ);
                b.BoundingBoxMax = new NVector3(maxX, maxY, maxZ);
            }

            //ErrorTODO: when this fails, else {}
        }

        public static NMatrix GetNMatrix(this FLVER.Bone b) {
            return NMatrix.CreateScale(b.Scale) *
                NMatrix.CreateRotationX(b.Rotation.X) *
                NMatrix.CreateRotationZ(b.Rotation.Z) *
                NMatrix.CreateRotationY(b.Rotation.Y) *
                NMatrix.CreateTranslation(b.Translation);
        }

        public static FLVER.Bone GetParent(this FLVER.Bone b, List<FLVER.Bone> bones) {
            if (b.ParentIndex >= 0 && b.ParentIndex < bones.Count)
                return bones[b.ParentIndex];
            else
                return null;
        }

        public static NMatrix GetAbsoluteNMatrix(this FLVER.Bone b, List<FLVER.Bone> bones) {
            NMatrix result = NMatrix.Identity;
            var parentBone = b;
            while (parentBone != null) {
                var m = parentBone.GetNMatrix();
                result *= m;
                parentBone = parentBone.GetParent(bones);
            }
            return result;
        }
    }
}
