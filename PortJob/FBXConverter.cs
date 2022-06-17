using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoulsFormats;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.IO;

namespace PortJob
{
    /* Converts static meshes from Morrowind into the Dark Souls FLVER format. */
    /* Morrowinds native NIF format has to be mass converted to FBX first for this program to work. */
    /* Heavily references code from Meowmartius's FBX2FLVER. He's a secret gamer god. */
    class FBXConverter
    {
        const int FLVER_VERSION = 0x2000C;
        const byte TPF_ENCODING = 2;
        const byte TPF_FLAG_2 = 3;

        const byte FLVER_UNK_0x5C = 0;
        const int FLVER_UNK_0x68 = 0;

        const string HARDCODE_TEXTURE_KEY = "g_detailBumpmap";
        const string HARDCODE_TEXTURE_VAL = "";
        const byte HARDCODE_TEXTURE_UNK10 = 0x0;
        const bool HARDCODE_TEXTURE_UNK11 = false;

        const bool ABSOLUTE_VERT_POSITIONS = true;
        const float GLOBAL_SCALE = 0.01f;

        const int FACESET_MAX_TRIANGLES = 65535; // Max triangles in a mesh for the DS1 engine.

        // Return an object containing the flver, tpfs, and generated ids and names stuff later
        public static void convert(string fbxPath)
        {
            /* Create a blank FLVER */
            var flver = new SoulsFormats.FLVER2();
            flver.Header.Version = FLVER_VERSION;

            /* List for storing all TPF textures that this FLVER will use */
            List<SoulsFormats.TPF> tpfs = new List<SoulsFormats.TPF>();

            /* Load FBX */
            Log.Info(0, "Converting static mesh: " + fbxPath);
            var flverMeshNameMap = new Dictionary<SoulsFormats.FLVER2.Mesh, string>();

            var fbxImporter = new FbxImporter();
            var context = new FBXConverterContext();
            var fbx = fbxImporter.Import(fbxPath, context);

            /* Grab all mesh content from FBX */
            var FBX_Meshes = new Dictionary<SoulsFormats.FLVER2.Mesh, MeshContent>();

            void FBXHierarchySearch(NodeContent node)
            {
                foreach (var fbxComponent in node.Children)
                {
                    if (fbxComponent.Name.ToLower() == "collision")
                    {
                        continue;
                    }
                    if (fbxComponent is MeshContent meshContent)
                    {
                        FBX_Meshes.Add(new SoulsFormats.FLVER2.Mesh(), meshContent);
                    }
                    if(fbxComponent.Children.Count > 0)
                    {
                        FBXHierarchySearch(fbxComponent);
                    }
                }
            }
            FBXHierarchySearch(fbx);

            if (fbx is MeshContent topLevelMeshContent)
            {
                FBX_Meshes.Add(new SoulsFormats.FLVER2.Mesh(), topLevelMeshContent);
            }

            if (FBX_Meshes.Count < 1) { Log.Error(1, "FBX contains no mesh data"); }
            else { Log.Info(1, "FBX contains " + FBX_Meshes.Count + " meshes."); }

            /* Add root bone */
            FLVER.Bone rb = new FLVER.Bone();
            rb.Name = "root";
            flver.Bones.Add(rb);

            /* Begin converting mesh data */
            int mc = 0;
            foreach (var kvp in FBX_Meshes)
            {
                Log.Info(2, "Mesh #" + mc + " :: " + kvp.Value.Name);
                var flverMesh = kvp.Key;
                var fbxMesh = kvp.Value;

                var bonesReferencedByThisMesh = new List<SoulsFormats.FLVER.Bone>();

                var submeshHighQualityNormals = new List<Vector3>();
                var submeshHighQualityTangents = new List<Vector4>();
                var submeshVertexHighQualityBasePositions = new List<Vector3>();
                var submeshVertexHighQualityBaseUVs = new List<Vector2>();

                /* Create face sets, split them if they are to big. */
                int gc = 0;
                foreach (var geometryNode in fbxMesh.Geometry)
                {
                    Log.Info(3, "Geometry #" + gc + " :: " + geometryNode.Name);
                    if (geometryNode is GeometryContent geometryContent)
                    {
                        int numTriangles = geometryContent.Indices.Count / 3;

                        int numFacesets = numTriangles / FACESET_MAX_TRIANGLES;

                        {
                            var faceSet = new SoulsFormats.FLVER2.FaceSet();

                            faceSet.CullBackfaces = geometryNode.Material.Name.StartsWith("!");

                            for (int i = 0; i < geometryContent.Indices.Count; i += 3)
                            {
                                if (faceSet.Indices.Count >= FACESET_MAX_TRIANGLES * 3)
                                {
                                    flverMesh.FaceSets.Add(faceSet);
                                    faceSet = new SoulsFormats.FLVER2.FaceSet();
                                }
                                else
                                {
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 2]);
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 1]);
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 0]);
                                }
                            }

                            if (faceSet.Indices.Count > 0)
                            {
                                flverMesh.FaceSets.Add(faceSet);
                            }

                        }

                        if (flverMesh.FaceSets.Count > 1)
                        {
                            Log.Info(4, "Mesh exceeds vertex limit [" + flverMesh.Vertices.Count + " > " + FACESET_MAX_TRIANGLES + "], it will be auto-split. ");
                        }

                        /* Handle materials */
                        string matName = null;
                        string mtdName = null;

                        List<TextureKey> matTextures = new List<TextureKey>();

                        if (geometryContent.Material != null)
                        {
                            mtdName = "M[D]";
                            matName = geometryContent.Material.Name;

                            Log.Info(5, "[MTD: " + mtdName + ", Material: " + matName + "]");

                            /* Handle textures */
                            // Reworked this so that it preserves the TextureMember order.
                            // The order that the textures are written to the material matters!
                            List<TextureKey> TextureChannelMap = MTD.getTextureMap(mtdName + ".mtd");
                            if (TextureChannelMap == null) { Log.Error(6, "Invalid MTD: " + mtdName);  }
                            foreach (var TEX in TextureChannelMap)
                            {
                                KeyValuePair<string, ExternalReference<TextureContent>> texKvp = new KeyValuePair<string, ExternalReference<TextureContent>>(null, null); //???? C# why the fuck even ??????????
                                foreach (var boop in geometryContent.Material.Textures)
                                {
                                    if (boop.Key.Equals(TEX.Key)) { texKvp = boop; break; }
                                }

                                if (texKvp.Key == null) { Log.Error(6, "Missing key: " + TEX.Value); }

                                var shortTexName = "mw_" + Utility.PathToFileName(texKvp.Value.Filename);
                                matTextures.Add(new TextureKey(TEX.Value, shortTexName, TEX.Unk10, TEX.Unk11));

                                // Writes every texture to a seperate file.
                                // This is how From handles their enviroment textures so I'm just following their pattern.
                                TPF nuTpf = new TPF();
                                nuTpf.Encoding = TPF_ENCODING;
                                nuTpf.Flag2 = TPF_FLAG_2;
                                var texBytes = File.ReadAllBytes(texKvp.Value.Filename);
                                var texFormat = DDS.GetTpfFormatFromDdsBytes(shortTexName, texBytes);

                                if(texFormat == 0) { Log.Error(6, "Texture is an unrecognized format [" + shortTexName + "::" + texFormat + "]"); }
                                else { Log.Info(6, "Texure [" + shortTexName + "::" + texFormat + "]"); }

                                nuTpf.Textures.Add(new SoulsFormats.TPF.Texture(shortTexName, (byte)texFormat, 0, texBytes));
                                tpfs.Add(nuTpf);
                            }

                            /* Add hardcoded detail bump texture data */
                            matTextures.Add(new TextureKey(HARDCODE_TEXTURE_KEY, HARDCODE_TEXTURE_VAL, HARDCODE_TEXTURE_UNK10, HARDCODE_TEXTURE_UNK11));
                        }
                        else
                        {
                            Log.Error(5, "Missing material data for this mesh");
                        }

                        flverMesh.MaterialIndex = flver.Materials.Count;

                        /* Write material to FLVER */
                        FLVER2.Material mat = new FLVER2.Material(matName, mtdName + ".mtd", 0);
                        foreach(var t in matTextures)
                        {
                            FLVER2.Texture tex = new FLVER2.Texture(t.Key, t.Value, System.Numerics.Vector2.One, t.Unk10, t.Unk11, 0, 0, 0);
                            mat.Textures.Add(tex);
                        }
                        flver.Materials.Add(mat);

                        /* Write position data to FLVER */
                        Log.Info(5, "Writing vertices");
                        for (int i = 0; i < geometryContent.Vertices.Positions.Count; i++)
                        {
                            var nextPosition = geometryContent.Vertices.Positions[i];
                            var posVec3 = Vector3.Transform(
                                new Vector3(-nextPosition.X, nextPosition.Y, nextPosition.Z)
                                , (ABSOLUTE_VERT_POSITIONS ? fbxMesh.AbsoluteTransform : fbx.Transform) * Matrix.CreateScale(GLOBAL_SCALE)
                                );

                            var newVert = new SoulsFormats.FLVER.Vertex()
                            {
                                Position = new System.Numerics.Vector3(posVec3.X, posVec3.Y, posVec3.Z),
                                BoneIndices = new SoulsFormats.FLVER.VertexBoneIndices(),
                                BoneWeights = new SoulsFormats.FLVER.VertexBoneWeights(),
                            };

                            /* Add placeholder vertex data to FLVER */
                            foreach (var memb in MTD.getLayout(mtdName + ".mtd", true))
                            {
                                switch (memb.Semantic)
                                {
                                    case SoulsFormats.FLVER.LayoutSemantic.Position: break;
                                    case SoulsFormats.FLVER.LayoutSemantic.Normal: newVert.Normal = new System.Numerics.Vector3(0, 0, 0); break;
                                    case SoulsFormats.FLVER.LayoutSemantic.Tangent: newVert.Tangents.Add(new System.Numerics.Vector4()); break;
                                    case SoulsFormats.FLVER.LayoutSemantic.BoneIndices: newVert.BoneIndices = new SoulsFormats.FLVER.VertexBoneIndices(); break;
                                    case SoulsFormats.FLVER.LayoutSemantic.BoneWeights: newVert.BoneWeights = new SoulsFormats.FLVER.VertexBoneWeights(); break;
                                    case SoulsFormats.FLVER.LayoutSemantic.UV:
                                        if (memb.Type == SoulsFormats.FLVER.LayoutType.UVPair)
                                        {
                                            newVert.UVs.Add(new System.Numerics.Vector3());
                                        }
                                        newVert.UVs.Add(new System.Numerics.Vector3());
                                        break;
                                    case SoulsFormats.FLVER.LayoutSemantic.VertexColor: newVert.Colors.Add(new SoulsFormats.FLVER.VertexColor(255, 255, 255, 255)); break;
                                    case SoulsFormats.FLVER.LayoutSemantic.Bitangent: newVert.Bitangent = System.Numerics.Vector4.Zero; break;
                                }
                            }

                            flverMesh.Vertices.Add(newVert);

                            submeshVertexHighQualityBasePositions.Add(new Vector3(posVec3.X, posVec3.Y, posVec3.Z));

                        }

                        /* Write real vertex data to FLVER */
                        Log.Info(5, "Writing vertex data");
                        foreach (var channel in geometryContent.Vertices.Channels)
                        {
                            if (channel.Name == "Normal0")
                            {
                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (Vector3)(channel[i]);
                                    var normalRotMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2);
                                    var normalInputVector = new Vector3(-channelValue.X, channelValue.Y, channelValue.Z);

                                    Vector3 rotatedNormal = Vector3.Normalize(
                                        Vector3.TransformNormal(normalInputVector, normalRotMatrix)
                                        );

                                    flverMesh.Vertices[i].Normal = new System.Numerics.Vector3()
                                    {
                                        X = rotatedNormal.X,
                                        Y = rotatedNormal.Y,
                                        Z = rotatedNormal.Z
                                    };

                                    submeshHighQualityNormals.Add(new Vector3(rotatedNormal.X, rotatedNormal.Y, rotatedNormal.Z));
                                }
                            }
                            else if (channel.Name.StartsWith("TextureCoordinate"))
                            {
                                var uvIndex = Utility.GetChannelIndex(channel.Name);

                                if (uvIndex > 2)
                                {
                                    Log.Error(6, "UV channel [" + uvIndex + "] is out of range and will be discarded.");
                                }

                                bool isBaseUv = submeshVertexHighQualityBaseUVs.Count == 0;

                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (Vector2)channel[i];

                                    var uv = new System.Numerics.Vector3(channelValue.X, channelValue.Y, 0);

                                    if (flverMesh.Vertices[i].UVs.Count > uvIndex)
                                    {
                                        flverMesh.Vertices[i].UVs[uvIndex] = uv;
                                    }
                                    else if (flverMesh.Vertices[i].UVs.Count == uvIndex)
                                    {
                                        flverMesh.Vertices[i].UVs.Add(uv);
                                    }
                                    else if (uvIndex <= 2)
                                    {
                                        while (flverMesh.Vertices[i].UVs.Count <= uvIndex)
                                        {
                                            flverMesh.Vertices[i].UVs.Add(System.Numerics.Vector3.Zero);
                                        }
                                    }

                                    if (isBaseUv)
                                    {
                                        submeshVertexHighQualityBaseUVs.Add(
                                            new Vector2(uv.X, uv.Y));
                                    }
                                }
                            }
                            else if (channel.Name == "Color0")
                            {
                                Log.Info(6, "Mesh vertex color data.");
                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (Vector4)channel[i];
                                    flverMesh.Vertices[i].Colors[0] = new SoulsFormats.FLVER.VertexColor(channelValue.W, channelValue.X, channelValue.Y, channelValue.Z);
                                }
                            }
                            else if (channel.Name == "Weights0")
                            {
                                Log.Info(6, "Mesh has weight data that will be discarded.");
                            }
                        }

                        /* Set blank weights for all vertices */
                        // According to meows code, not doing this causes weird things to happen
                        foreach (var vert in flverMesh.Vertices)
                        {
                            vert.BoneIndices = new SoulsFormats.FLVER.VertexBoneIndices();
                            vert.BoneWeights = new SoulsFormats.FLVER.VertexBoneWeights();
                        }
                        flverMesh.Dynamic = 0x0;
                    }
                }

                /* Don't know */
                if (bonesReferencedByThisMesh.Count == 0 && flver.Bones.Count > 0)
                {
                    bonesReferencedByThisMesh.Add(flver.Bones[0]);
                }

                /* Don't know */
                foreach (var refBone in bonesReferencedByThisMesh)
                {
                    flverMesh.BoneIndices.Add(flver.Bones.IndexOf(refBone));
                }

                /* Don't know */
                var submeshVertexIndices = new List<int>();

                foreach (var faceSet in flverMesh.FaceSets)
                {
                    submeshVertexIndices.AddRange(faceSet.Indices);
                }

                /* Don't know */
                if (submeshVertexHighQualityBaseUVs.Count == 0)
                {
                    Log.Error(6, "Missing UV data for mesh");
                }

                /* Generate tangents */
                Log.Info(6, "Generating tangents");
                if (submeshHighQualityTangents.Count > 0)
                {
                    submeshHighQualityTangents = Solvers.TangentSolver.SolveTangents(flverMesh, submeshVertexIndices,
                        submeshHighQualityNormals,
                        submeshVertexHighQualityBasePositions,
                        submeshVertexHighQualityBaseUVs);

                    for (int i = 0; i < flverMesh.Vertices.Count; i++)
                    {
                        Vector3 thingy = Vector3.Normalize(Vector3.Cross(submeshHighQualityNormals[i],
                           new Vector3(submeshHighQualityTangents[i].X,
                           submeshHighQualityTangents[i].Y,
                           submeshHighQualityTangents[i].Z) * submeshHighQualityTangents[i].W));

                        flverMesh.Vertices[i].Tangents[0] = new System.Numerics.Vector4(thingy.X, thingy.Y, thingy.Z, submeshHighQualityTangents[i].W);
                    }
                }

                /* Meow just wrote "bone shit" here so I guess that? */
                foreach (var mesh in flver.Meshes)
                {
                    if (mesh.BoneIndices.Count == 0)
                        mesh.BoneIndices.Add(0);

                    mesh.DefaultBoneIndex = 0;
                }

                var topLevelParentBones = flver.Bones.Where(x => x.ParentIndex == -1).ToArray();

                if (topLevelParentBones.Length > 0)
                {
                    for (int i = 0; i < topLevelParentBones.Length; i++)
                    {
                        if (i == 0)
                            topLevelParentBones[i].PreviousSiblingIndex = -1;
                        else
                            topLevelParentBones[i].PreviousSiblingIndex = (short)flver.Bones.IndexOf(topLevelParentBones[i - 1]);

                        if (i == topLevelParentBones.Length - 1)
                            topLevelParentBones[i].NextSiblingIndex = -1;
                        else
                            topLevelParentBones[i].NextSiblingIndex = (short)flver.Bones.IndexOf(topLevelParentBones[i + 1]);
                    }
                }

                flverMesh.VertexBuffers.Add(new SoulsFormats.FLVER2.VertexBuffer(layoutIndex: flverMesh.MaterialIndex));

                flver.Meshes.Add(flverMesh);
            }

            /* Orientation Solver */
            Solvers.OrientationSolver.SolveOrientation(flver);

            /* Generate LODS */
            // @TODO!! No idea how LODs work in the context of static meshes but we should defo look into this for performance later on!
            // GeneratePlaceholderLODs(flver);

            /* Write Buffer Layouts */
            flver.BufferLayouts = new List<SoulsFormats.FLVER2.BufferLayout>();
            foreach (var mat in flver.Materials)
            {
                flver.BufferLayouts.Add(MTD.getLayout(mat.MTD, true));
            }

            /* Couple of random FLVER flags to set */
            flver.Header.Unk5C = FLVER_UNK_0x5C;
            flver.Header.Unk68 = FLVER_UNK_0x68;

            /* Add dummies */
            // @TODO: likely don't do it at this point in code, will maybe need dummies for some SFX stuff possibly later on, should not be hard to implement since these are statics 
            // It's also fairly possible we may instead just place down our 'dummies' as points in the msb to attach SFX to. Idk. figure it out later.

            /* Solve bounding box */
            Solvers.BoundingBoxSolver.FixAllBoundingBoxes(flver);

            /* Don't know */
            foreach (var kvp in FBX_Meshes)
            {
                flverMeshNameMap.Add(kvp.Key, kvp.Value.Name);
            }

            //FLVER2 compa = FLVER2.Read("E:\\Backups\\Backup Remastest Source\\Dark Souls Prepare to Die Edition\\DATA\\map\\m10_00_00_00\\m2120B0A10.flver");

            /* Write FLVER to file */
            string outPath = "F:\\";
            string flverPath = outPath + "test.flver";
            Log.Info(1, "Writing FLVER to: " + flverPath);
            flver.Write(flverPath);
            foreach (var tpf in tpfs)
            {
                string tpfPath = outPath + tpf.Textures[0].Name + ".tpf";
                Log.Info(2, "Writing TPF to: " + tpfPath);
                tpf.Write(tpfPath);
            }
        }
    }

    /* Extending some abstracts that I need */
    internal class FBXConverterContext : ContentImporterContext
    {
        public override void AddDependency(string filename)
        {
            throw new NotImplementedException();
        }

        ContentBuildLogger _logger = new FBXConverterContentBuildLogger();

        public override ContentBuildLogger Logger => _logger;
        public override string OutputDirectory => "ContentImporterContext_out";
        public override string IntermediateDirectory => "ContentImporterContext_inter";
    }

    public class FBXConverterContentBuildLogger : ContentBuildLogger
    {
        public override void LogMessage(string message, params object[] messageArgs)
        {
            Console.WriteLine("XNA CONTENT BUILD LOG --> " + string.Format(message, messageArgs));
        }

        public override void LogImportantMessage(string message, params object[] messageArgs)
        {
            Console.WriteLine("XNA CONTENT BUILD LOG (\"IMPORTANT\") --> " + string.Format(message, messageArgs));
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
            Console.WriteLine("XNA CONTENT BUILD LOG (WARNING) --> " + string.Format(message, messageArgs));
        }
    }
}
