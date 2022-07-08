using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoulsFormats;

using System.IO;
using System.Numerics;


namespace PortJob {
    /* Converts landscape data into a flver model */
    class TerrainConverter {
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

        const int FACESET_MAX_TRIANGLES = 65535; // Max triangles in a mesh for the DS1 engine.

        // Return an object containing the flver, tpfs, and generated ids and names stuff later
        public static void convert(Cell cell, string flverPath, string tpfDir) {

            /* Skip if file already exists */
            // Lol

            /* Create a blank FLVER */
            FLVER2 flver = new();
            flver.Header.Version = FLVER_VERSION;

            /* List for storing all TPF textures that this FLVER will use */
            List<TPF> tpfs = new();

            /* Grab terrain data from cell */
            Log.Info(0, "Converting terrain data for cell: ");
            Dictionary<FLVER2.Mesh, string> flverMeshNameMap = new();
            Dictionary<FLVER2.Mesh, TerrainData> TerrainMeshes = new();
            Vector3 rootPosition = new Vector3(0f, 0f, 0f);

            foreach (TerrainData terrainData in cell.terrain) {
                TerrainMeshes.Add(new FLVER2.Mesh(), terrainData);
            }

            /* Buffer Layout dictionary */
            Dictionary<string, int> flverMaterials = new();

            /* Add root bone */
            FLVER.Bone rb = new();
            rb.Name = Path.GetFileNameWithoutExtension(flverPath);
            flver.Bones.Add(rb);

            /* Read FBX mesh data */
            int mc = 0;
            foreach (KeyValuePair<FLVER2.Mesh, TerrainData> kvp in TerrainMeshes) {
                Log.Info(2, "Mesh #" + mc + " :: " + kvp.Value.name);
                FLVER2.Mesh flverMesh = kvp.Key;
                TerrainData terrainMesh = kvp.Value;

                List<FLVER.Bone> bonesReferencedByThisMesh = new();

                List<Vector3> submeshHighQualityNormals = new();
                List<Vector4> submeshHighQualityTangents = new();
                List<Vector3> submeshVertexHighQualityBasePositions = new();
                List<Vector2> submeshVertexHighQualityBaseUVs = new();

                /* Create face sets, split them if they are to big. */
                int gc = 0;
                int gxIndex = 0;

                int numTriangles = terrainMesh.indices.Count / 3;

                int numFacesets = numTriangles / FACESET_MAX_TRIANGLES;

                {
                    FLVER2.FaceSet faceSet = new() {
                        Unk06 = 1
                    };

                    faceSet.CullBackfaces = true;

                    for (int i = 0; i < terrainMesh.indices.Count; i += 3) {
                        if (faceSet.Indices.Count >= FACESET_MAX_TRIANGLES * 3) {
                            flverMesh.FaceSets.Add(faceSet);
                            faceSet = new FLVER2.FaceSet();
                        } else {
                            faceSet.Indices.Add((ushort)terrainMesh.indices[i + 2]);
                            faceSet.Indices.Add((ushort)terrainMesh.indices[i + 1]);
                            faceSet.Indices.Add((ushort)terrainMesh.indices[i + 0]);
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

                mtdName = "M[ARSN]";
                string texA = terrainMesh.textures[0];
                string texB = terrainMesh.textures[terrainMesh.textures[1] != null ? 1 : 0];
                matName = terrainMesh.name + ":" + Utility.PathToFileName(texA) + "->" + Utility.PathToFileName(texB);

                Log.Info(5, "[MTD: " + mtdName + ", Material: " + matName + "]");

                /* Handle textures */
                string blackTex = "PortJob\\DefaultTex\\def_black.dds"; // @TODO IMPORTANT! generic textures!
                string greyTex = "PortJob\\DefaultTex\\def_grey.dds";
                string flatTex = "PortJob\\DefaultTex\\def_flat.dds";
                Dictionary<string, string> boopers = new();
                boopers.Add("g_DiffuseTexture", texA);
                //boopers.Add("g_DiffuseTexture2", texB);
                boopers.Add("g_SpecularTexture", blackTex);
                //boopers.Add("g_SpecularTexture2", blackTex);
                boopers.Add("g_ShininessTexture", blackTex);
                //boopers.Add("g_ShininessTexture2", blackTex);
                boopers.Add("g_BumpmapTexture", flatTex);
                //boopers.Add("g_BumpmapTexture2", flatTex);
                //boopers.Add("g_DetailBumpmapTexture", "");
                //boopers.Add("g_DetailBumpmapTexture2", "");
                //boopers.Add("g_DisplacementTexture", null");
                //boopers.Add("g_BlendMaskTexture", greyTex);

                List<TextureKey> TextureChannelMap = MTD.getTextureMap(mtdName + ".mtd");
                if (TextureChannelMap == null) { Log.Error(6, "Invalid MTD: " + mtdName); }
                foreach (TextureKey TEX in TextureChannelMap) {
                    if (TEX.Value == "g_DisplacementTexture") {
                        matTextures.Add(new TextureKey(TEX.Value, "N:\\LiveTokyo\\data\\model\\common\\tex\\dummy128.tga", TEX.Unk10, TEX.Unk11));  // God save our souls...
                    } else {
                        string tex = boopers[TEX.Value];

                        string shortTexName = "mw_" + Utility.PathToFileName(tex);
                        matTextures.Add(new TextureKey(TEX.Value, shortTexName, TEX.Unk10, TEX.Unk11));
                        //flverMaterials.Add(matName, 0);

                        // Writes every texture to a seperate file.
                        // This is how From handles their enviroment textures so I'm just following their pattern.
                        TPF nuTpf = new();
                        nuTpf.Encoding = TPF_ENCODING;
                        nuTpf.Flag2 = TPF_FLAG_2;
                        bool srgb = !(TEX.Value.ToLower().Contains("blend") || TEX.Value.ToLower().Contains("normal") || TEX.Value.ToLower().Contains("bumpmap"));
                        byte[] texBytes = srgb ? MTD.GetSRGBTexture(tex) : MTD.GetTexture(tex);

                        int texFormat = DDS.GetTpfFormatFromDdsBytes(texBytes);

                        // @TODO this checek is pointless because it doesn't actually know wtf even
                        //if (texFormat == 0) { Log.Error(6, "Texture is an unrecognized format [" + shortTexName + "::" + texFormat + "]"); } else { Log.Info(6, "Texure [" + shortTexName + "::" + texFormat + "]"); }

                        nuTpf.Textures.Add(new TPF.Texture(shortTexName, (byte)texFormat, 0, texBytes));
                        tpfs.Add(nuTpf);
                    }
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
                for (int i = 0; i < terrainMesh.vertices.Count; i++) {
                    Vector3 posVec3 = new Vector3(terrainMesh.vertices[i].position.X, terrainMesh.vertices[i].position.Y, terrainMesh.vertices[i].position.Z);

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
                for (int i = 0; i < terrainMesh.vertices.Count; i++) {
                    TerrainVertex vert = terrainMesh.vertices[i];

                    // Normal
                    Matrix4x4 normalRotMatrix = Matrix4x4.CreateRotationX((float)-(Math.PI / 2));
                    Vector3 normalInputVector = new(-vert.normal.X, vert.normal.Y, vert.normal.Z);

                    Vector3 rotatedNormal = Vector3.Normalize(
                        Vector3.TransformNormal(normalInputVector, normalRotMatrix)
                        );

                    flverMesh.Vertices[i].Normal = new System.Numerics.Vector3() {
                        X = rotatedNormal.X,
                        Y = rotatedNormal.Y,
                        Z = rotatedNormal.Z
                    };

                    submeshHighQualityNormals.Add(new Vector3(rotatedNormal.X, rotatedNormal.Y, rotatedNormal.Z));

                    // Texture Coordinate
                    int uvIndex = 0;

                    if (uvIndex > 2) {
                        Log.Error(6, "UV channel [" + uvIndex + "] is out of range and will be discarded.");
                    }

                    bool isBaseUv = submeshVertexHighQualityBaseUVs.Count == 0;

                    System.Numerics.Vector3 uv = new(vert.coordinate.X, vert.coordinate.Y, 0);

                    flverMesh.Vertices[i].UVs.Add(uv);
                    flverMesh.Vertices[i].UVs.Add(uv);
                    flverMesh.Vertices[i].UVs.Add(uv);

                    if (isBaseUv) {
                        submeshVertexHighQualityBaseUVs.Add(
                            new Vector2(uv.X, uv.Y));
                    }

                    // Color
                    float blend = terrainMesh.texturesIndices[0] == vert.texture ? 1f : 0f;
                    flverMesh.Vertices[i].Colors[0] = new FLVER.VertexColor(vert.color.X, vert.color.Y, vert.color.Z, 1.0f);
                }

                /* Set blank weights for all vertices */
                // According to meows code, not doing this causes weird things to happen
                foreach (FLVER.Vertex vert in flverMesh.Vertices) {
                    vert.BoneIndices = new FLVER.VertexBoneIndices();
                    vert.BoneWeights = new FLVER.VertexBoneWeights();
                }
                flverMesh.Dynamic = 0x0;


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
            foreach (KeyValuePair<FLVER2.Mesh, TerrainData> kvp in TerrainMeshes) {
                flverMeshNameMap.Add(kvp.Key, kvp.Value.name);
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
                if (File.Exists(tpfPath)) { continue; } // Skip if file already exists
                Log.Info(2, "Writing TPF to: " + tpfPath);
                tpf.Write(tpfPath, DCX.Type.DCX_DFLT_10000_24_9);
            }
        }
    }
}
