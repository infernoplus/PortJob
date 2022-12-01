using CommonFunc;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class DoorMake {
        /* Class that converts a morrowind animated door into a dark souls 3 animated door. */
        /* Uses an existing ds3 door and simply skins the morrowind door to its skeleton. Doing this means I don't need to create custom collision or animations for them. */
        /* We will defo need overrides to adjust things in the future but this is good for now */
        public enum Orientation {
            pX, nX, pZ, nZ
        }
        public static Dictionary<Orientation, float> ORIENTATION_KEY = new() {
            { Orientation.nX, (float)Math.PI },
            { Orientation.pX, 0f },
            { Orientation.nZ, (float)-Math.PI/2 },
            { Orientation.pZ, (float)Math.PI/2 }
        };

        public static void Convert(ObjActInfo objActInfo) {
            FLVER2 inFlver = FLVER2.Read(objActInfo.model.path);
            FLVER2 outFlver = FLVER2.Read(Utility.GetEmbededResourceBytes("CommonFunc.Resources.door_md.flver"));

            /* Get bounding box of our new door so we can align it to the skeleton of the template door */
            Vector3 min = new(float.MaxValue), max = new(float.MinValue);
            foreach (FLVER2.Mesh mesh in inFlver.Meshes) {
                foreach (FLVER.Vertex vertex in mesh.Vertices) {
                    /* Calculate min and max bounds */
                    min.X = Math.Min(min.X, vertex.Position.X);
                    max.X = Math.Max(max.X, vertex.Position.X);
                    min.Y = Math.Min(min.Y, vertex.Position.Y);
                    max.Y = Math.Max(max.Y, vertex.Position.Y);
                    min.Z = Math.Min(min.Z, vertex.Position.Z);
                    max.Z = Math.Max(max.Z, vertex.Position.Z);
                }
            }
            /* Figure out which way this door is oriented. Morrowind doors **seem** to always be aligned with the hinge on the world root. So we just check which axis it's on */
            Orientation X = Math.Abs(min.X) > Math.Abs(max.X) ? Orientation.nX : Orientation.pX;
            Orientation Z = Math.Abs(min.Z) > Math.Abs(max.Z) ? Orientation.nZ : Orientation.pZ;
            Orientation orientation = Math.Max(Math.Abs(min.X), Math.Abs(max.X)) > Math.Max(Math.Abs(min.Z), Math.Abs(max.Z)) ? X : Z;

            Vector3 alignOffset = new(0f, -min.Y, 0f);  // Height offset since morrowind doors are not consistently aligned (fucks sake todd...) we must find the bounds and align it ourselves
            Vector3 alignRotation = new(0f, ORIENTATION_KEY[orientation], 0f); // Morrowind doors are all rotated differently (FUCKS SAKE TODD) so we have to determine that rotation and correct it
            bool invert = false; // Flips door 180 degrees around, this fixes issues with the hinge being oriented wrong for doors in nX or nZ orientation (mirrored?)

            /* Reverse hinge on doors with nZ or nX orientation */
            if (orientation is Orientation.nZ) { invert = true; } // This might be wrong? Might need to be a mirror?
            if (orientation is Orientation.nX) { invert = true; }

            foreach (FLVER2.Mesh mesh in inFlver.Meshes) {

                foreach (FLVER.Vertex vertex in mesh.Vertices) {
                    /* Rotation */
                    float cosDegrees = (float)Math.Cos(alignRotation.Y);   // Why do I have to write out a rotation function? Why is this not in the Vector3 lib? Seriously...
                    float sinDegrees = (float)Math.Sin(alignRotation.Y);

                    float x = (vertex.Position.X * cosDegrees) + (vertex.Position.Z * sinDegrees);
                    float z = (vertex.Position.X * -sinDegrees) + (vertex.Position.Z * cosDegrees);

                    vertex.Position.X = x;
                    vertex.Position.Z = z;

                    /* Translation */
                    vertex.Position += alignOffset;

                    /* Normals */
                    Matrix4x4 normalRotMatrixY = Matrix4x4.CreateRotationY(alignRotation.Y);       // Accounting for alignment rotation in normals
                    Vector3 normalInputVector = new(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);

                    Vector3 rotatedNormal = Vector3.Normalize(
                        Vector3.TransformNormal(normalInputVector, normalRotMatrixY)
                    );

                    vertex.Normal = rotatedNormal;
                }
            }

            /* Replace data in ds3 door templat flver with morrowind door */
            outFlver.BufferLayouts.Clear();
            outFlver.GXLists.Clear();
            outFlver.Materials.Clear();
            outFlver.Meshes.Clear();
            foreach (FLVER2.BufferLayout bufferLayout in inFlver.BufferLayouts) { outFlver.BufferLayouts.Add(bufferLayout); }
            foreach (FLVER2.GXList gxlist in inFlver.GXLists) { outFlver.GXLists.Add(gxlist); }
            foreach (FLVER2.Material material in inFlver.Materials) {
                outFlver.Materials.Add(material);
            }
            foreach (FLVER2.Mesh mesh in inFlver.Meshes) {
                outFlver.Meshes.Add(mesh);
                mesh.BoneIndices.Remove(0);
                foreach(FLVER.Vertex vertex in mesh.Vertices) {
                    vertex.Tangents.Add(new System.Numerics.Vector4(1, 0, 0, 1));  // Very not correct but also probaly not using it!
                    vertex.NormalW = 1;
                }
            }

            /* Calculate reverse offset for MSB coordinates */
            /* This is used to reverse the orientation/offset when placing the object in the game world */
            FLVER.Bone rootBone = outFlver.Bones[0];
            FLVER.Bone hingeBone = outFlver.Bones[1];
            Vector3 reverseOffset = Vector3.Zero - alignOffset - (hingeBone.Translation - rootBone.Translation);
            objActInfo.offset = reverseOffset;
            objActInfo.orientation = -alignRotation.Y;
            objActInfo.invert = invert;

            /* Does not work. Moving bones has no effect at all.
            FLVER.Bone rootBone = outFlver.Bones[0];
            FLVER.Bone hingeBone = outFlver.Bones[1];
            hingeBone.Translation = new Vector3(rootBone.Translation.X, rootBone.Translation.Y, rootBone.Translation.Z);
            hingeBone.Translation -= alignOffset;
            */

            /* Generate objBnd */
            TPF tpf = TPF.Read(objActInfo.model.textures[0].path); // Merge all used tpfs into a single tpf
            tpf.Compression = DCX.Type.None;
            tpf.Encoding = 0x1;
            for (int i = 1; i < objActInfo.model.textures.Count; i++) {
                TextureInfo textureInfo = objActInfo.model.textures[i];
                TPF mortpf = TPF.Read(textureInfo.path);
                foreach (TPF.Texture tex in mortpf.Textures) {
                    tpf.Textures.Add(tex);
                }
            }

            byte[] hkx = Utility.GetEmbededResourceBytes("CommonFunc.Resources.door_md.hkx");
            byte[] anibnd = Utility.GetEmbededResourceBytes("CommonFunc.Resources.door_md.anibnd");

            BND4 objBnd = new BND4();
            string objPath = $"obj\\o{0:D2}\\o{0:D2}{objActInfo.id:D4}\\o{0:D2}{objActInfo.id:D4}";
            objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 100, $"{objPath}.tpf", tpf.Write()));
            objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, $"{objPath}.flver", outFlver.Write()));
            objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 300, $"{objPath}.hkx", hkx));
            objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 400, $"{objPath}.anibnd", anibnd));
            objBnd.Write($"{Const.OutputPath}obj\\o{0:D2}{objActInfo.id:D4}.objbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }
}
