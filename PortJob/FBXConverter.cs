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

namespace PortJob {
    /* Converts static meshes from Morrowind into the Dark Souls FLVER format. */
    /* Morrowinds native NIF format has to be mass converted to FBX first for this program to work. */
    /* Heavily references code from Meowmartius's FBX2FLVER. He's a secret gamer god. */
    class FBXConverter {
        const int FLVER_VERSION = 0x20014;
        const byte TPF_ENCODING = 2;
        const byte TPF_FLAG_2 = 3;

        const byte FLVER_UNK_0x5C = 0;
        const int FLVER_UNK_0x68 = 4;

        const string HARDCODE_TEXTURE_KEY = "g_DetailBumpmap";
        const string HARDCODE_TEXTURE_VAL = "";
        const byte HARDCODE_TEXTURE_UNK10 = 0x01;
        const bool HARDCODE_TEXTURE_UNK11 = true;

        const bool ABSOLUTE_VERT_POSITIONS = true;
        public static readonly float GLOBAL_SCALE = 0.01f;

        const int FACESET_MAX_TRIANGLES = 65535; // Max triangles in a mesh for the DS1 engine.

        // Return an object containing the flver, tpfs, and generated ids and names stuff later
        public static void convert(string fbxPath, string flverPath, string tpfDir) {
            /* Create a blank FLVER */
            FLVER2 flver = new();
            flver.Header.Version = FLVER_VERSION;

            /* List for storing all TPF textures that this FLVER will use */
            List<TPF> tpfs = new();

            /* Load FBX */
            Log.Info(0, "Converting static mesh: " + fbxPath);
            Dictionary<FLVER2.Mesh, string> flverMeshNameMap = new();

            FbxImporter fbxImporter = new();
            FBXConverterContext context = new();
            NodeContent fbx = fbxImporter.Import(fbxPath, context);

            /* Grab all mesh content from FBX */
            Dictionary<FLVER2.Mesh, MeshContent> FBX_Meshes = new();
            Vector3 rootPosition = fbx.Transform.Translation;

            /* Buffer Layout dictionary */
            Dictionary<string, int> flverMaterials = new();


            void FBXHierarchySearch(NodeContent node) {
                foreach (NodeContent fbxComponent in node.Children) {
                    if (fbxComponent.Name.ToLower() == "collision") {
                        continue;
                    }
                    if (fbxComponent is MeshContent meshContent) {
                        FBX_Meshes.Add(new FLVER2.Mesh(), meshContent);
                    }
                    if (fbxComponent.Children.Count > 0) {
                        FBXHierarchySearch(fbxComponent);
                    }
                }
            }
            FBXHierarchySearch(fbx);

            if (fbx is MeshContent topLevelMeshContent) {
                FBX_Meshes.Add(new FLVER2.Mesh(), topLevelMeshContent);
            }

            if (FBX_Meshes.Count < 1) { Log.Error(1, "FBX contains no mesh data"); } else { Log.Info(1, "FBX contains " + FBX_Meshes.Count + " meshes."); }

            /* Add root bone */
            FLVER.Bone rb = new();
            rb.Name = Path.GetFileNameWithoutExtension(flverPath);
            flver.Bones.Add(rb);

            /* Read FBX mesh data */
            int mc = 0;
            foreach (KeyValuePair<FLVER2.Mesh, MeshContent> kvp in FBX_Meshes) {
                Log.Info(2, "Mesh #" + mc + " :: " + kvp.Value.Name);
                FLVER2.Mesh flverMesh = kvp.Key;
                MeshContent fbxMesh = kvp.Value;

                List<FLVER.Bone> bonesReferencedByThisMesh = new();

                List<Vector3> submeshHighQualityNormals = new();
                List<Vector4> submeshHighQualityTangents = new();
                List<Vector3> submeshVertexHighQualityBasePositions = new();
                List<Vector2> submeshVertexHighQualityBaseUVs = new();

                /* Create face sets, split them if they are to big. */
                int gc = 0;
                int gxIndex = 0;
                foreach (GeometryContent geometryNode in fbxMesh.Geometry) {
                    Log.Info(3, "Geometry #" + gc + " :: " + geometryNode.Name);
                    if (geometryNode is GeometryContent geometryContent) {
                        int numTriangles = geometryContent.Indices.Count / 3;

                        int numFacesets = numTriangles / FACESET_MAX_TRIANGLES;

                        {
                            FLVER2.FaceSet faceSet = new() {
                                Unk06 = 1
                            };

                            faceSet.CullBackfaces = geometryNode.Material.Name.StartsWith("!");

                            for (int i = 0; i < geometryContent.Indices.Count; i += 3) {
                                if (faceSet.Indices.Count >= FACESET_MAX_TRIANGLES * 3) {
                                    flverMesh.FaceSets.Add(faceSet);
                                    faceSet = new FLVER2.FaceSet();
                                } else {
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 2]);
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 1]);
                                    faceSet.Indices.Add((ushort)geometryContent.Indices[i + 0]);
                                }
                            }

                            if (faceSet.Indices.Count > 0) {
                                flverMesh.FaceSets.Add(faceSet);
                            }

                        }

                        if (flverMesh.FaceSets.Count > 1) {
                            Log.Info(4, "Mesh exceeds vertex limit [" + flverMesh.Vertices.Count + " > " + FACESET_MAX_TRIANGLES + "], it will be auto-split. ");
                        }

                        /* Handle materials */
                        string matName = null;
                        string mtdName = null;

                        List<TextureKey> matTextures = new();

                        if (geometryContent.Material != null) {
                            mtdName = "M[A]";
                            matName = geometryContent.Material.Name;
                            //int ind = matName.IndexOf("|") + 1;
                            ////@TODO - Change the way the materials are named in NIF2FBX and then change this god awful trimming code
                            //matName = matName.Substring(ind, matName.Length - (ind + 1)).Trim(); //Cursed. I should change the material names in the conversion process. 

                            //if (flverMaterials.ContainsKey(matName)) continue;

                            Log.Info(5, "[MTD: " + mtdName + ", Material: " + matName + "]");

                            /* Handle textures */
                            // Reworked this so that it preserves the TextureMember order.
                            // The order that the textures are written to the material matters!
                            List<TextureKey> TextureChannelMap = MTD.getTextureMap(mtdName + ".mtd");
                            if (TextureChannelMap == null) { Log.Error(6, "Invalid MTD: " + mtdName); }
                            foreach (TextureKey TEX in TextureChannelMap) {
                                KeyValuePair<string, ExternalReference<TextureContent>> texKvp = new(null, null); //???? C# why the fuck even ??????????
                                foreach (KeyValuePair<string, ExternalReference<TextureContent>> boop in geometryContent.Material.Textures) {
                                    if (boop.Key.Equals(TEX.Key)) { texKvp = boop; break; }
                                }

                                if (texKvp.Key == null) { Log.Error(6, "Missing key: " + TEX.Value); continue;                                }

                                string shortTexName = "mw_" + Utility.PathToFileName(texKvp.Value.Filename);
                                matTextures.Add(new TextureKey(TEX.Value, shortTexName, TEX.Unk10, TEX.Unk11));
                                //flverMaterials.Add(matName, 0);

                                // Writes every texture to a seperate file.
                                // This is how From handles their enviroment textures so I'm just following their pattern.
                                TPF nuTpf = new();
                                nuTpf.Encoding = TPF_ENCODING;
                                nuTpf.Flag2 = TPF_FLAG_2;
                                byte[] texBytes = MTD.GetSRGBTexture(texKvp.Value.Filename);
                                int texFormat = DDS.GetTpfFormatFromDdsBytes(texBytes);
                                if (texFormat == 3) mtdName += "_al";

                                if (texFormat == 0) { Log.Error(6, "Texture is an unrecognized format [" + shortTexName + "::" + texFormat + "]"); } else { Log.Info(6, "Texure [" + shortTexName + "::" + texFormat + "]"); }

                                nuTpf.Textures.Add(new TPF.Texture(shortTexName, (byte)texFormat, 0, texBytes));
                                tpfs.Add(nuTpf);
                            }

                            /* Add hardcoded detail bump texture data */
                            //matTextures.Add(new TextureKey(HARDCODE_TEXTURE_KEY, HARDCODE_TEXTURE_VAL, HARDCODE_TEXTURE_UNK10, HARDCODE_TEXTURE_UNK11));
                        } else {
                            Log.Error(5, "Missing material data for this mesh");
                        }

                        flverMesh.MaterialIndex = flver.Materials.Count;

                        /* Write material to FLVER */
                        FLVER2.Material mat = new(matName, mtdName + ".mtd", 232) {
                            GXIndex = gxIndex
                        };

                        foreach (TextureKey t in matTextures) {
                            FLVER2.Texture tex = new(t.Key, t.Value, System.Numerics.Vector2.One, t.Unk10, t.Unk11, 0, 0, 0);
                            mat.Textures.Add(tex);
                        }
                        flver.Materials.Add(mat);

                        /* Write GXList to FLVER */
                        FLVER2.GXList gxinfo = MTD.getGXList(mtdName + ".mtd");
                        flver.GXLists.Add(gxinfo);

                        /* Write position data to FLVER */
                        Log.Info(5, "Writing vertices");
                        for (int i = 0; i < geometryContent.Vertices.Positions.Count; i++) {
                            Vector3 nextPosition = geometryContent.Vertices.Positions[i];
                            Vector3 posVec3 = Vector3.Transform(
                                new Vector3(nextPosition.X, nextPosition.Y, nextPosition.Z)
                                , (ABSOLUTE_VERT_POSITIONS ? fbxMesh.AbsoluteTransform : fbx.Transform) * Matrix.CreateScale(GLOBAL_SCALE)
                                );

                            posVec3.X = -posVec3.X; // Flip X after applying root transform, bugfix from FBX2FLVER

                            FLVER.Vertex newVert = new() {
                                Position = new System.Numerics.Vector3(posVec3.X, posVec3.Y, posVec3.Z),
                                BoneIndices = new FLVER.VertexBoneIndices(),
                                BoneWeights = new FLVER.VertexBoneWeights(),
                            };

                            /* Add placeholder vertex data to FLVER */
                            foreach (FLVER.LayoutMember memb in MTD.getLayout(mtdName + ".mtd", true)) {
                                switch (memb.Semantic) {
                                    case FLVER.LayoutSemantic.Position: break;
                                    case FLVER.LayoutSemantic.Normal: newVert.Normal = new System.Numerics.Vector3(0, 0, 0); break;
                                    case FLVER.LayoutSemantic.Tangent: newVert.Tangents.Add(new System.Numerics.Vector4()); break;
                                    case FLVER.LayoutSemantic.BoneIndices: newVert.BoneIndices = new FLVER.VertexBoneIndices(); break;
                                    case FLVER.LayoutSemantic.BoneWeights: newVert.BoneWeights = new FLVER.VertexBoneWeights(); break;
                                    case FLVER.LayoutSemantic.UV:
                                        if (memb.Type == FLVER.LayoutType.UVPair) {
                                            newVert.UVs.Add(new System.Numerics.Vector3());
                                        }
                                        newVert.UVs.Add(new System.Numerics.Vector3());
                                        break;
                                    case FLVER.LayoutSemantic.VertexColor: newVert.Colors.Add(new FLVER.VertexColor(255, 255, 255, 255)); break;
                                    case FLVER.LayoutSemantic.Bitangent: newVert.Bitangent = System.Numerics.Vector4.Zero; break;
                                }
                            }

                            flverMesh.Vertices.Add(newVert);

                            submeshVertexHighQualityBasePositions.Add(new Vector3(posVec3.X, posVec3.Y, posVec3.Z));



                        }


                        /* Write real vertex data to FLVER */
                        Log.Info(5, "Writing vertex data");
                        foreach (VertexChannel channel in geometryContent.Vertices.Channels) {
                            if (channel.Name == "Normal0") {
                                for (int i = 0; i < flverMesh.Vertices.Count; i++) {
                                    Vector3 channelValue = (Vector3)(channel[i]);
                                    Matrix normalRotMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2);
                                    Vector3 normalInputVector = new(-channelValue.X, channelValue.Y, channelValue.Z);

                                    Vector3 rotatedNormal = Vector3.Normalize(
                                        Vector3.TransformNormal(normalInputVector, normalRotMatrix)
                                        );

                                    flverMesh.Vertices[i].Normal = new System.Numerics.Vector3() {
                                        X = rotatedNormal.X,
                                        Y = rotatedNormal.Y,
                                        Z = rotatedNormal.Z
                                    };

                                    submeshHighQualityNormals.Add(new Vector3(rotatedNormal.X, rotatedNormal.Y, rotatedNormal.Z));
                                }
                            } else if (channel.Name.StartsWith("TextureCoordinate")) {
                                int uvIndex = Utility.GetChannelIndex(channel.Name);

                                if (uvIndex > 2) {
                                    Log.Error(6, "UV channel [" + uvIndex + "] is out of range and will be discarded.");
                                }

                                bool isBaseUv = submeshVertexHighQualityBaseUVs.Count == 0;

                                for (int i = 0; i < flverMesh.Vertices.Count; i++) {
                                    Vector2 channelValue = (Vector2)channel[i];

                                    System.Numerics.Vector3 uv = new(channelValue.X, channelValue.Y, 0);

                                    if (flverMesh.Vertices[i].UVs.Count > uvIndex) {
                                        flverMesh.Vertices[i].UVs[uvIndex] = uv;
                                    } else if (flverMesh.Vertices[i].UVs.Count == uvIndex) {
                                        flverMesh.Vertices[i].UVs.Add(uv);
                                    } else if (uvIndex <= 2) {
                                        while (flverMesh.Vertices[i].UVs.Count <= uvIndex) {
                                            flverMesh.Vertices[i].UVs.Add(System.Numerics.Vector3.Zero);
                                        }
                                    }

                                    if (isBaseUv) {
                                        submeshVertexHighQualityBaseUVs.Add(
                                            new Vector2(uv.X, uv.Y));
                                    }
                                }
                            } else if (channel.Name == "Color0") {
                                Log.Info(6, "Mesh vertex color data.");
                                for (int i = 0; i < flverMesh.Vertices.Count; i++) {
                                    Vector4 channelValue = (Vector4)channel[i];
                                    flverMesh.Vertices[i].Colors[0] = new FLVER.VertexColor(channelValue.W, channelValue.X, channelValue.Y, channelValue.Z);
                                }
                            } else if (channel.Name == "Weights0") {
                                Log.Info(6, "Mesh has weight data that will be discarded.");
                            }
                        }

                        /* Set blank weights for all vertices */
                        // According to meows code, not doing this causes weird things to happen
                        foreach (FLVER.Vertex vert in flverMesh.Vertices) {
                            vert.BoneIndices = new FLVER.VertexBoneIndices();
                            vert.BoneWeights = new FLVER.VertexBoneWeights();
                        }
                        flverMesh.Dynamic = 0x0;
                    }
                }

                /* Don't know */
                if (bonesReferencedByThisMesh.Count == 0 && flver.Bones.Count > 0) {
                    bonesReferencedByThisMesh.Add(flver.Bones[0]);
                }

                /* Don't know */
                foreach (FLVER.Bone refBone in bonesReferencedByThisMesh) {
                    flverMesh.BoneIndices.Add(flver.Bones.IndexOf(refBone));
                }

                /* Don't know */
                List<int> submeshVertexIndices = new();

                foreach (FLVER2.FaceSet faceSet in flverMesh.FaceSets) {
                    submeshVertexIndices.AddRange(faceSet.Indices);
                }

                /* Don't know */
                if (submeshVertexHighQualityBaseUVs.Count == 0) {
                    Log.Error(6, "Missing UV data for mesh");
                }

                /* Generate tangents */
                Log.Info(6, "Generating tangents");
                if (submeshHighQualityTangents.Count > 0) {
                    submeshHighQualityTangents = Solvers.TangentSolver.SolveTangents(flverMesh, submeshVertexIndices,
                        submeshHighQualityNormals,
                        submeshVertexHighQualityBasePositions,
                        submeshVertexHighQualityBaseUVs);

                    for (int i = 0; i < flverMesh.Vertices.Count; i++) {
                        Vector3 thingy = Vector3.Normalize(Vector3.Cross(submeshHighQualityNormals[i],
                           new Vector3(submeshHighQualityTangents[i].X,
                           submeshHighQualityTangents[i].Y,
                           submeshHighQualityTangents[i].Z) * submeshHighQualityTangents[i].W));

                        flverMesh.Vertices[i].Tangents[0] = new System.Numerics.Vector4(thingy.X, thingy.Y, thingy.Z, submeshHighQualityTangents[i].W);
                    }
                }

                /* Meow just wrote "bone shit" here so I guess that? */
                foreach (FLVER2.Mesh mesh in flver.Meshes) {
                    if (mesh.BoneIndices.Count == 0)
                        mesh.BoneIndices.Add(0);

                    mesh.DefaultBoneIndex = 0;
                }

                FLVER.Bone[] topLevelParentBones = flver.Bones.Where(x => x.ParentIndex == -1).ToArray();

                if (topLevelParentBones.Length > 0) {
                    for (int i = 0; i < topLevelParentBones.Length; i++) {
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


                flverMesh.VertexBuffers.Add(new FLVER2.VertexBuffer(layoutIndex: flverMesh.MaterialIndex));
                flver.Meshes.Add(flverMesh);

            }

            /* Orientation Solver */
            Solvers.OrientationSolver.SolveOrientation(flver);

            /* Generate LODS */
            // @TODO!! No idea how LODs work in the context of static meshes but we should defo look into this for performance later on!
            // GeneratePlaceholderLODs(flver);

            /* Write Buffer Layouts */
            flver.BufferLayouts = new List<FLVER2.BufferLayout>();
            foreach (FLVER2.Material mat in flver.Materials) {
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
            foreach (KeyValuePair<FLVER2.Mesh, MeshContent> kvp in FBX_Meshes) {
                flverMeshNameMap.Add(kvp.Key, kvp.Value.Name);
            }

            /* Write FLVER to file */
            Log.Info(1, "Writing FLVER to: " + flverPath);
            BND4 bnd = new() {
                Compression = DCX.Type.DCX_DFLT_10000_44_9
            };

            string flverName = Path.GetFileNameWithoutExtension(flverPath);
            //string mapName = Path.GetFileName(flverPath.Substring(0, flverPath.LastIndexOf('_'))); //file name is just the top level directory, here. There is no GetTopLevelDirectory :(

            string internalFlverPath = flverName + ".flver"; //"N:\\FDP\\data\\INTERROOT_win64\\map\\" + mapName + "\\" + flverName + "\\Model\\" + flverName + ".flver" //full internal path
            bnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, internalFlverPath, flver.Write()));
            bnd.Write(flverPath.Replace("flver", "mapbnd.dcx"), DCX.Type.DCX_DFLT_10000_44_9);
            //flver.Write(flverPath, DCX.Type.DCX_DFLT_10000_24_9);
            foreach (TPF tpf in tpfs) {
                string tpfPath = tpfDir + tpf.Textures[0].Name + ".tpf.dcx";
                Log.Info(2, "Writing TPF to: " + tpfPath);
                tpf.Write(tpfPath, DCX.Type.DCX_DFLT_10000_24_9);
            }
        }
    }

    /* Extending some abstracts that I need */
    internal class FBXConverterContext : ContentImporterContext {
        public override void AddDependency(string filename) {
            throw new NotImplementedException();
        }

        ContentBuildLogger _logger = new FBXConverterContentBuildLogger();

        public override ContentBuildLogger Logger => _logger;
        public override string OutputDirectory => "ContentImporterContext_out";
        public override string IntermediateDirectory => "ContentImporterContext_inter";
    }

    public class FBXConverterContentBuildLogger : ContentBuildLogger {
        public override void LogMessage(string message, params object[] messageArgs) {
            Console.WriteLine("XNA CONTENT BUILD LOG --> " + string.Format(message, messageArgs));
        }

        public override void LogImportantMessage(string message, params object[] messageArgs) {
            Console.WriteLine("XNA CONTENT BUILD LOG (\"IMPORTANT\") --> " + string.Format(message, messageArgs));
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs) {
            Console.WriteLine("XNA CONTENT BUILD LOG (WARNING) --> " + string.Format(message, messageArgs));
        }
    }
}
