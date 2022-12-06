using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonFunc;
using System.Numerics;
using System.IO;

namespace PortJob {
    /* Generates a water plane for a cell. */
    // @TODO: Needs work! I just generally don't like how the water looks atm.
    // @TODO: Water seems very performance heavy. If we have performance issues on the GPU we should look more into this generation
    class Sploosh {
        public static string RESOURCE_PATH = "CommonFunc.Resources.";
        public static List<TextureKey> TEXTURES = new() {
            new("g_DiffuseTexture", "mw_water_blue_a", 1, true, new Vector2(7.3f, 7.3f)),
            new("g_DiffuseTexture2", "mw_water_blue_a", 1, true, new Vector2(6.1f, 6.1f)),
            new("g_SpecularTexture", "mw_water_r", 1, true, Vector2.One),
            new("g_SpecularTexture2", "mw_water_r", 1, true, Vector2.One),
            new("g_ShininessTexture", "mw_water_s", 1, true, Vector2.One),
            new("g_ShininessTexture2", "mw_water_s", 1, true, Vector2.One),
            new("g_BumpmapTexture", "mw_water_00_n", 1, true, new Vector2(2.1f, 2.1f)),
            new("g_BumpmapTexture2", "mw_water_01_n", 1, true, new Vector2(3.25f, 3.25f)),
            new("g_BumpmapTexture3", "mw_water_02_n", 1, true, new Vector2(7.75f, 7.75f))
        };

        /* M40 water gx bytes */
        /*public static Dictionary<string, Tuple<int, byte[]>> GX_DATA = new() {
            { "GX00", new(100, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })},
            { "GX09", new(100, new byte[] { 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 0, 205, 204, 204, 62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) },
            { "GX07", new(104, new byte[] { 111, 18, 131, 58, 111, 18, 131, 58, 111, 18, 131, 186, 0, 0, 0, 0, 205, 204, 76, 60, 205, 204, 76, 60, 205, 204, 76, 188, 205, 204, 76, 188, 205, 204, 204, 61, 0, 0, 128, 63, 205, 204, 204, 61, 205, 204, 204, 61, 0, 0, 0, 0, 96, 229, 208, 61, 47, 221, 36, 62, 0, 0, 128, 63, 0, 0, 192, 64, 205, 204, 204, 61, 154, 153, 153, 62, 51, 51, 35, 64, 111, 18, 131, 58, 166, 155, 196, 186, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) }
        };*/

        /* M37 water gx bytes */
        public static Dictionary<string, Tuple<int, byte[]>> GX_DATA = new() {
            { "GX00", new(100, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) },
            { "GX09", new(100, new byte[] { 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0  }) },
            { "GX07", new(104, new byte[] { 111, 18, 131, 59, 66, 96, 229, 59, 166, 155, 68, 59, 111, 18, 131, 58, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 154, 153, 153, 62, 0, 0, 128, 63, 205, 204, 76, 62, 0, 0, 0, 0, 0, 0, 0, 0, 213, 120, 233, 62, 0, 0, 0, 63, 0, 0, 0, 0, 154, 153, 153, 62, 205, 204, 12, 63, 154, 153, 153, 62, 51, 51, 35, 64, 10, 215, 35, 60, 10, 215, 35, 188, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) }
        };

        /* Create water for an entire exterior MSB. */
        public static WaterInfo CreateWater(Cache cache, Layout layout) {
            /* Defs */
            string flverName = $"EXT{layout.id:D2}";
            string mtd = "M[ARSN]_Water.mtd"; ;
            Vector2 size = new(Const.CELL_SIZE / 2);
            Vector3 BoxMin = new Vector3(layout.min.x - layout.center.x, -0.01f, layout.min.y - layout.center.y) * Const.CELL_SIZE;
            Vector3 BoxMax = new Vector3(layout.max.x + 1 - layout.center.x, 0.01f, layout.max.y + 1 - layout.center.y) * Const.CELL_SIZE;

            /* New FLVER */
            FLVER2 flver = new();

            /* Header */
            flver.Header.BoundingBoxMin = BoxMin;
            flver.Header.BoundingBoxMax = BoxMax;
            flver.Header.Unk68 = 4;

            /* Root Bone */
            FLVER.Bone root = new();
            root.Name = "root";
            root.BoundingBoxMin = BoxMin;
            root.BoundingBoxMax = BoxMax;
            flver.Bones.Add(root);

            /* Material */
            FLVER2.Material material = new();
            material.Name = "water";
            material.MTD = mtd;
            material.Flags = 1388; // Unknown, copied from m40_00_00_00\m40_00_00_00_000012
            material.GXIndex = 0;
            flver.Materials.Add(material);

            /* Textures */
            foreach (TextureKey texKey in TEXTURES) {
                FLVER2.Texture tex = new(texKey.Key, texKey.Value, texKey.uv, (byte)texKey.Unk10, texKey.Unk11, 0, 0, 0);
                material.Textures.Add(tex);
            }

            /* Buffer Layout */
            List<FLVER2.BufferLayout> bufferlayouts = CommonFunc.MTD.getLayouts(material.MTD, true);
            foreach (FLVER2.BufferLayout bufferlayout in bufferlayouts) {
                flver.BufferLayouts.Add(bufferlayout); // Example mesh has both layouts. IDK why. Just following orders~
            }

            /* GXLists */
            FLVER2.GXList gxlist = new();
            foreach (KeyValuePair<string, Tuple<int, byte[]>> gxdata in GX_DATA) {
                FLVER2.GXItem gxitem = new();
                gxitem.ID = gxdata.Key;
                gxitem.Unk04 = gxdata.Value.Item1;
                gxitem.Data = gxdata.Value.Item2;
                gxlist.Add(gxitem);
            }
            flver.GXLists.Add(gxlist);

            /* Mesh */
            FLVER2.Mesh mesh = new();
            mesh.BoundingBox = new();
            mesh.DefaultBoneIndex = 0;
            mesh.BoundingBox.Min = BoxMin;
            mesh.BoundingBox.Max = BoxMax;
            flver.Meshes.Add(mesh);

            /* Generate vertices indices info from layout */
            List<int> indices = new();
            List<Vector2> vertices = new();

            // Returns the indice of the vertex if it already exists in our vertex data. Or the indice of the added vertex if it does not.
            int AddVertex(Vector2 vertex) {
                for(int i=0;i<vertices.Count;i++) {
                    if(vertices[i] == vertex) { return i; }
                }
                vertices.Add(vertex);
                return vertices.Count-1;
            }

            foreach(Cell cell in layout.cells) {
                // Skip if terrain not generated or terrain is above ext water line
                TerrainInfo terrainInfo = cache.GetTerrainInfo(cell.position);
                if(terrainInfo == null || terrainInfo.min > 0) { continue; }

                int[] ind = new int[] {
                    AddVertex(new Vector2(cell.position.x, cell.position.y)),
                    AddVertex(new Vector2(cell.position.x + 1, cell.position.y)),
                    AddVertex(new Vector2(cell.position.x, cell.position.y + 1)),
                    AddVertex(new Vector2(cell.position.x + 1, cell.position.y + 1))
                };

                int[] magic = new[] { 0, 1, 3, 3, 2, 0 };
                foreach (int i in magic) { indices.Add(ind[i]); }
            }

            /* Vertices */
            Vector3 up = new(0, 1, 0);
            Vector2 center = new(layout.center.x, layout.center.y);
            foreach (Vector2 v in vertices) {
                Vector2 pos = (v - center) * Const.CELL_SIZE;
                Vector2 uv = (v - center) * 2;

                FLVER.Vertex vertex = new();
                vertex.Position = new Vector3(pos.X, 0, pos.Y);
                vertex.Colors.Add(new FLVER.VertexColor(1f, 1f, 0, 0));   // God only knows why
                vertex.Normal = up;
                for (int i = 0; i < 3; i++) {
                    vertex.Tangents.Add(new Vector4(0.8031496f, 0, 0.5905512f, -1));  // Not even god knows
                }
                for (int i = 0; i < 3; i++) {
                    vertex.UVs.Add(new Vector3(uv.X, uv.Y, 0));   // God is dead
                }
                mesh.Vertices.Add(vertex);
            }

            /* Vertex Buffer */
            FLVER2.VertexBuffer vertexBuffer = new(0);
            mesh.VertexBuffers.Add(vertexBuffer);

            /* Face Set */
            FLVER2.FaceSet faceset = new();
            faceset.CullBackfaces = true;
            faceset.TriangleStrip = false;
            faceset.Unk06 = 1;
            foreach (int index in indices) {
                faceset.Indices.Add(index);
            }
            mesh.FaceSets.Add(faceset);

            /* Write to file */
            string waterPath = $"{Const.OutputPath}cache\\water\\WATER {flverName}.flver";
            Directory.CreateDirectory(Path.GetDirectoryName(waterPath));
            flver.Write(waterPath);
            WaterInfo waterInfo = new(waterPath);
            waterInfo.layout = layout.id;
            foreach (TextureKey texKey in TEXTURES) {
                string texPath = $"{Const.OutputPath}cache\\water\\{texKey.Value}.tpf.dcx";
                string texRes = $"{RESOURCE_PATH}{texKey.Value}.tpf.dcx";
                if (!File.Exists(texPath)) { File.WriteAllBytes(texPath, Utility.GetEmbededResourceBytes(texRes)); }
                TextureInfo textureInfo = new(texKey.Value, texPath);
                waterInfo.textures.Add(textureInfo);
            }

            return waterInfo;
        }

        /* Creates water for a single interior cell */
        public static WaterInfo CreateWater(Cell cell, Bounds bounds) {
            /* Test stuff */
            /*FLVER2 test1 = FLVER2.Read(@"C:\Games\steamapps\common\DARK SOULS III\Game\map\m40_00_00_00\m40_00_00_00_000012-mapbnd-dcx\map\m40_00_00_00\m40_00_00_00_000012\Model\m40_00_00_00_000012.flver");
            FLVER2 test2 = FLVER2.Read(@"C:\Games\steamapps\common\DARK SOULS III\Game\map\m37_00_00_00\m37_00_00_00_000023-mapbnd-dcx\map\m37_00_00_00\m37_00_00_00_000023\Model\m37_00_00_00_000023.flver");
            foreach(FLVER2.GXList G1 in test2.GXLists) {
                foreach(FLVER2.GXItem G2 in G1) {
                    foreach(byte G3 in G2.Data) {
                        Console.Write($"{G3}, ");
                    }
                    Console.WriteLine("\n\n");
                }
            }*/

            /* Defs */
            float w = bounds.width / 2;
            float h = bounds.length / 2;
            string flverName = $"{cell.name}";
            string mtd = "M[ARSN]_Water.mtd"; Vector2 size = new(w, h);

            /* New FLVER */
            FLVER2 flver = new();

            /* Header */
            flver.Header.BoundingBoxMin = new Vector3(-size.X, 1f, -size.Y);
            flver.Header.BoundingBoxMax = new Vector3(size.X, 1f, size.Y);
            flver.Header.Unk68 = 4;

            /* Root Bone */
            FLVER.Bone root = new();
            root.Name = "root";
            root.BoundingBoxMin = flver.Header.BoundingBoxMin;
            root.BoundingBoxMax = flver.Header.BoundingBoxMax;
            flver.Bones.Add(root);

            /* Material */
            FLVER2.Material material = new();
            material.Name = "water";
            material.MTD = mtd;
            material.Flags = 1388; // Unknown, copied from m40_00_00_00\m40_00_00_00_000012
            material.GXIndex = 0;
            flver.Materials.Add(material);

            /* Textures */
            foreach (TextureKey texKey in TEXTURES) {
                FLVER2.Texture tex = new(texKey.Key, texKey.Value, texKey.uv, (byte)texKey.Unk10, texKey.Unk11, 0, 0, 0);
                material.Textures.Add(tex);
            }

            /* Buffer Layout */
            List<FLVER2.BufferLayout> layouts = CommonFunc.MTD.getLayouts(material.MTD, true);
            foreach (FLVER2.BufferLayout layout in layouts) {
                flver.BufferLayouts.Add(layout); // Example mesh has both layouts. IDK why. Just following orders~
            }

            /* GXLists */
            FLVER2.GXList gxlist = new();
            foreach (KeyValuePair<string, Tuple<int, byte[]>> gxdata in GX_DATA) {
                FLVER2.GXItem gxitem = new();
                gxitem.ID = gxdata.Key;
                gxitem.Unk04 = gxdata.Value.Item1;
                gxitem.Data = gxdata.Value.Item2;
                gxlist.Add(gxitem);
            }
            flver.GXLists.Add(gxlist);

            /* Mesh */
            FLVER2.Mesh mesh = new();
            mesh.BoundingBox = new();
            mesh.DefaultBoneIndex = 0;
            mesh.BoundingBox.Min = flver.Header.BoundingBoxMin;
            mesh.BoundingBox.Max = flver.Header.BoundingBoxMax;
            flver.Meshes.Add(mesh);

            /* Vertices */
            Vector3 up = new Vector3(0, 1, 0);
            Vector2 uv = new Vector2(size.X / Const.CELL_SIZE, size.Y / Const.CELL_SIZE);
            List<Tuple<Vector3, Vector3>> vdata = new() {
                new(new Vector3(size.X, 0, size.Y), new Vector3(uv.X, uv.Y, 0)),
                new(new Vector3(-size.X, 0, size.Y), new Vector3(-uv.X, uv.Y, 0)),
                new(new Vector3(size.X, 0, -size.Y), new Vector3(uv.X, -uv.Y, 0)),
                new(new Vector3(-size.X, 0, -size.Y), new Vector3(-uv.X, -uv.Y, 0))
            };

            foreach (Tuple<Vector3, Vector3> dat in vdata) {
                FLVER.Vertex vertex = new();
                vertex.Position = dat.Item1;
                vertex.Colors.Add(new FLVER.VertexColor(1f, 1f, 0, 0));   // God only knows why
                vertex.Normal = up;
                for (int i = 0; i < 3; i++) {
                    vertex.Tangents.Add(new Vector4(0.8031496f, 0, 0.5905512f, -1));  // Not even god knows
                }
                for (int i = 0; i < 3; i++) {
                    vertex.UVs.Add(dat.Item2);   // God is dead
                }
                mesh.Vertices.Add(vertex);
            }

            /* Vertex Buffer */
            FLVER2.VertexBuffer vertexBuffer = new(0);
            mesh.VertexBuffers.Add(vertexBuffer);

            /* Face Set */
            int[] indices = new[] {
                0, 1, 3, 3, 2, 0
            };

            FLVER2.FaceSet faceset = new();
            faceset.CullBackfaces = true;
            faceset.TriangleStrip = false;
            faceset.Unk06 = 1;
            foreach (int index in indices) {
                faceset.Indices.Add(index);
            }
            mesh.FaceSets.Add(faceset);

            /* Write to file */
            string waterPath = $"{Const.OutputPath}cache\\water\\WATER {flverName}.flver";
            Directory.CreateDirectory(Path.GetDirectoryName(waterPath));
            flver.Write(waterPath);
            WaterInfo waterInfo = new(waterPath);
            waterInfo.cell = cell.name;
            foreach (TextureKey texKey in TEXTURES) {
                string texPath = $"{Const.OutputPath}cache\\water\\{texKey.Value}.tpf.dcx";
                string texRes = $"{RESOURCE_PATH}{texKey.Value}.tpf.dcx";
                if(!File.Exists(texPath)) { File.WriteAllBytes(texPath, Utility.GetEmbededResourceBytes(texRes)); }
                TextureInfo textureInfo = new(texKey.Value, texPath);
                waterInfo.textures.Add(textureInfo);
            }

            return waterInfo;
        }
    }
}
