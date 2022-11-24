﻿using CommonFunc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using System.Numerics;
using SoulsFormats;
using System.Threading;

using gfoidl.Base64;
using ImpromptuNinjas.ZStd;
using static CommonFunc.Const;
using DirectXTexNet;

namespace PortJob {
    /* Loads and handles the JSON file of the morrowind.esm that tes3conv outputs */
    public class ESM {
        private JArray json; // Full unfiltered json of the morrowind.json
        private Dictionary<Type, List<JObject>> recordsMap;
        public List<Cell> exteriorCells, interiorCells;
        public List<TerrainTexture> terrainTextures; // LandscapeTextures but in a faster to search format

        public enum Type {
            Header, GameSetting, GlobalVariable, Class, Faction, Race, Sound, Skill, MagicEffect, Script, Region, Birthsign, LandscapeTexture, Spell, Static, Door,
            MiscItem, Weapon, Container, Creature, Bodypart, Light, Enchantment, Npc, Armor, Clothing, RepairTool, Activator, Apparatus, Lockpick, Probe, Ingredient,
            Book, Alchemy, LevelledItem, LevelledCreature, Cell, Landscape, PathGrid, SoundGen, Dialogue, Info
        }

        public ESM(string path) {
            JsonSerializer serializer = new();
            Log.Info(0,"Loading ESM...");
            DateTime startParse = DateTime.Now;
            using (FileStream s = File.Open(path, FileMode.Open))
            using (StreamReader sr = new(s))
            using (JsonReader reader = new JsonTextReader(sr)) {
                while (!sr.EndOfStream) {
                    json = serializer.Deserialize<JArray>(reader);
                }
            }
            Log.Info(0, $"Parse Json time: {DateTime.Now - startParse}");

            recordsMap = new Dictionary<Type, List<JObject>>();
            foreach (string name in Enum.GetNames(typeof(Type))) {
                Enum.TryParse(name, out Type type);
                recordsMap.Add(type, new List<JObject>());
            }

            for (int i = 0; i < json.Count; i++) {
                JObject record = (JObject)json[i];
                foreach (string name in Enum.GetNames(typeof(Type))) {
                    if (record["type"].ToString() == name) {
                        Enum.TryParse(name, out Type type);
                        recordsMap[type].Add(record);
                    }
                }
            }

            foreach (string name in Enum.GetNames(typeof(Type))) {
                Enum.TryParse(name, out Type type);
                //Console.WriteLine(name + ": " + recordsMap[type].Count);
            }

            DateTime startLoadCells = DateTime.Now;
            LoadCells();
            Log.Info(0, $"Load Cells time: {DateTime.Now - startLoadCells}\n");
        }

        const int EXTERIOR_BOUNDS = 40; // +/- Bounds of the cell grid we consider to be the 'Exterior'
        const int CELL_THREADS = 16;

        private void LoadCells() {
            /* Load TerrainTextures first since we will be referencing them a lot */
            terrainTextures = new();
            foreach(JObject record in recordsMap[Type.LandscapeTexture]) {
                TerrainTexture ttex = new TerrainTexture(record["texture"].ToString(), ushort.Parse(record["index"].ToString()));
                terrainTextures.Add(ttex);
            }

            exteriorCells = new();
            interiorCells = new();

            List<JObject> cells = recordsMap[Type.Cell];
            int partitionSize = (int)Math.Ceiling(cells.Count / (float)CELL_THREADS);
            List<CellFactoryWorker> cellFactories = new();

            for (int i = 0; i < CELL_THREADS; i++) {
                int start = i * partitionSize;
                int end = start + partitionSize;
                CellFactoryWorker cellFactoryWorker = new(this, cells, start, end);
                cellFactories.Add(cellFactoryWorker);
            }

            while (true) {
                bool workerThreadsDone = true;
                foreach (CellFactoryWorker worker in cellFactories) {
                    workerThreadsDone &= worker.IsDone;
                }

                if (workerThreadsDone)
                    break;
            }

            int count = 0;
            foreach (CellFactoryWorker cellFactory in cellFactories) {
                foreach (Cell cell in cellFactory.ProcessedCells) {
                    AddCell(cell);
                    count++;
                }
            }

            Log.Info(0,$"Processed: {count}/{cells.Count} cells");
        }

        private void AddCell(Cell genCell) {
            if (genCell.refs < 1) return; // Cull cells with nothing in them. Removes most blank ocean cells which we really don't need.

            if (genCell.position.x >= -EXTERIOR_BOUNDS &&
                genCell.position.x <= EXTERIOR_BOUNDS &&
                genCell.position.y >= -EXTERIOR_BOUNDS &&
                genCell.position.y <= EXTERIOR_BOUNDS)
            {
                exteriorCells.Add(genCell);
            }
            else
            {
                interiorCells.Add(genCell);
            }
        }

        public List<Cell> GetInteriorCells() {
            return interiorCells;
        }

        public List<Cell> GetExteriorCells() {
            return exteriorCells;
        }

        /* List of types that we should search for references */
        public readonly Type[] VALID_CONTENT_TYPES = {
            Type.Sound, Type.Skill, Type.Region, Type.Static, Type.Door, Type.MiscItem, Type.Weapon, Type.Container, Type.Creature, Type.Bodypart, Type.Light, Type.Npc,
            Type.Armor, Type.Clothing, Type.RepairTool, Type.Activator, Type.Apparatus, Type.Lockpick, Type.Probe, Type.Ingredient, Type.Book, Type.Alchemy, Type.LevelledItem,
            Type.LevelledCreature, Type.PathGrid, Type.SoundGen
        };

        public Cell GetCellByGrid(Int2 position) {
            foreach(Cell cell in exteriorCells) {
                if(cell.position == position) { return cell; }
            }
            return null;
        }

        public Cell GetCellByName(string name) {
            foreach (Cell cell in exteriorCells) {
                if (cell.name == name) { return cell; }
            }
            foreach (Cell cell in interiorCells) {
                if (cell.name == name) { return cell; }
            }
            return null;
        }

        /* Returns cell that is closest to the given worldspace coordinate. */
        public Cell GetCellByCoordinate(Vector3 position) {
            foreach (Cell cell in exteriorCells) {
                // I'll code this later lol. Sleepy
            }
            return null;
        }

        public JObject GetLandscapeByGrid(Int2 position) {
            foreach (JObject landscape in recordsMap[Type.Landscape]) {
                int x = int.Parse(landscape["grid"][0].ToString());
                int y = int.Parse(landscape["grid"][1].ToString());

                if(position.x == x && position.y == y) {
                    return landscape;
                }
            }
            return null;
        }

        /* References don't contain any explicit 'type' data so... we just gotta go find it lol */
        /* @TODO: well actually i think the 'flags' int value in some records is useed as a 32bit boolean array and that may specify record types possibly. Look into it? */
        public TypedRecord FindRecordById(string id) {
            foreach (Type type in VALID_CONTENT_TYPES) {
                List<JObject> records = recordsMap[type];

                for (int i = 0; i < records.Count; i++) {
                    JObject record = records[i];
                    if (record["id"] != null && record["id"].ToString() == id) {
                        return new TypedRecord(type, record);
                    }
                }
            }
            return null; // Not found!
        }

        /* Searches a specific set of records for one that has the specified field with a matching value */
        public JObject FindRecordByKey(Type type, string key, string value) {
            List<JObject> records = recordsMap[type];

            for (int i = 0; i < records.Count; i++) {
                JObject record = records[i];
                if (record[key] != null && record[key].ToString() == value) {
                    return record;
                }
            }
            return null; // Not found!
        }

        /* Get a TerrainTexture by it's index. We do this a lot during terrain generatino so I made a class for it for speeeed */
        public TerrainTexture GetTerrainTextureByIndex(ushort index) {
            foreach(TerrainTexture ttex in terrainTextures) {
                if(ttex.index == index) {
                    return ttex;
                }
            }
            return null;
        }
    }

    public class Cell {
        public readonly JObject raw; // Raw JSON data
        public bool generated, loaded; // True once load/generate are called.

        public readonly string name;
        public readonly string region;
        public readonly Int2 position;  // Position on the cell grid

        public readonly Vector3 center; // Float center point of cell in world space

        public readonly int flag;
        public readonly int[] flags;

        public readonly List<TerrainData> terrain;
        public readonly TerrainVertex[,] borders;

        public readonly List<Content> content;

        /* These fields are used by Layout for stuff */
        public Layout layout;         // Parent layout
        public int refs;              // Number of references in this cell, for culling purposes
        public int drawId;            // Drawgroup ID, value also correponds to the bitwise (1 << id)
        public uint[] drawGroups;
        public List<Cell> pairs;      // Cells that it borders in other msbs, these will have 'paired draw ids'
        public List<Layout> connects; // Connect collisions we need to generate

        public Cell(ESM esm, JObject data) {
            raw = data;

            name = data["id"].ToString();
            region = data["region"] != null ? data["region"].ToString() : "null";

            int x = int.Parse(data["data"]["grid"][0].ToString());
            int y = int.Parse(data["data"]["grid"][1].ToString());
            position = new Int2(x, y);

            center = new Vector3((CELL_SIZE * position.x) + (CELL_SIZE * 0.5f), 0.0f, (CELL_SIZE * position.y) + (CELL_SIZE * 0.5f));

            flag = int.Parse(data["data"]["flags"].ToString());

            JArray flc = (JArray)(data["flags"]);
            flags = new int[flc.Count];
            for (int i = 0; i < flc.Count; i++) {
                flags[i] = int.Parse(flc[i].ToString());
            }

            /* Create these arrays but we don't fill them out until we call 'generate()' on this cell */
            content = new();
            terrain = new();
            borders = new TerrainVertex[4, 65];

            /* These fields are used by Layout for stuff */
            refs = ((JArray)data["references"]).Count; // We don't actaully process cell content until we call generate() on it but we do make a quick count of it's content for culling purposes
            layout = null;
            drawId = -1;
            drawGroups = null;
            pairs = null;
            connects = null;

            /* Flags for what data has been loaded/procesed in this class */
            loaded = false;
            generated = false;
        }

        /* Loads contents of cell */
        /* We don't load every cell initially because it takes a while. We load them on demand when/if we need them */
        public void Load(ESM esm) {
            if (loaded) { return; }

            JArray refc = (JArray)(raw["references"]);
            for (int i = 0; i < refc.Count; i++) {
                JObject reference = (JObject)(refc[i]);
                content.Add(new Content(esm, reference));
            }

            loaded = true;
        }

        /* Generates terrain data */
        /* SLOW! We only call this if we are generating terrain. */
        public void Generate(ESM esm) {
            Load(esm);
            if (generated) { return; }

            /* Decode and parse terrain data if it exists for this cell */
            JObject landscape = esm.GetLandscapeByGrid(position);
            if (landscape != null && landscape["vertex_heights"] != null) {
                byte[] b64Height = Base64.Default.Decode(landscape["vertex_heights"].ToString());
                byte[] b64Normal = Base64.Default.Decode(landscape["vertex_normals"].ToString());
                ZStdDecompressor zstd = new();
                byte[] zstdHeight = new byte[4 + 65 * 65 + 3]; zstd.Decompress(zstdHeight, b64Height);
                byte[] zstdNormal = new byte[65 * 65 * 3]; zstd.Decompress(zstdNormal, b64Normal);

                byte[] zstdColor = landscape["vertex_colors"] != null ? new byte[65 * 65 * 3] : null;
                if (zstdColor != null) {
                    byte[] b64Color = Base64.Default.Decode(landscape["vertex_colors"].ToString());
                    zstd.Decompress(zstdColor, b64Color);
                }

                int bA = 0; // Buffer postion reading heights
                int bB = 0; // Buffer position reading normals
                int bC = 0; // Buffer position reading color
                int bD = 0; // Buffer position for texture indices

                /* Checks through all landscape texture data and makes sure there is no duplicate texture index that points to the same texture file. Returns same index if no dupe or a dupe at a higher index, returns dupe index if found and it's a lower value. */
                byte[] zstdTexture = landscape["texture_indices"] != null ? new byte[16 * 16 * 2] : null;
                ushort[,] ltex = new ushort[16, 16];
                if (zstdTexture != null) {
                    byte[] b64Texture = Base64.Default.Decode(landscape["texture_indices"].ToString());
                    zstd.Decompress(zstdTexture, b64Texture);

                    for (int yy = 0; yy < 15; yy += 4) {
                        for (int xx = 0; xx < 15; xx += 4) {
                            for (int yyy = 0; yyy < 4; yyy++) {
                                for (int xxx = 0; xxx < 4; xxx++) {
                                    ushort texIndex = Cell.DeDupeTextureIndex(esm, (ushort)(BitConverter.ToUInt16(new byte[] { zstdTexture[bD++], zstdTexture[bD++] }, 0) - (ushort)1));
                                    ltex[xx + xxx, yy + yyy] = texIndex;
                                }
                            }
                        }
                    }
                }

                zstd.Dispose(); // Lol memory

                float offset = BitConverter.ToSingle(new byte[] { zstdHeight[bA++], zstdHeight[bA++], zstdHeight[bA++], zstdHeight[bA++] }, 0);

                /* Vertex Data */
                Vector3 centerOffset = new Vector3((CELL_SIZE / 2f), 0f, -(CELL_SIZE / 2f));
                List<TerrainVertex> vertices = new();
                float last = offset;
                float lastEdge = last;
                for (int yy = CELL_GRID_SIZE; yy >= 0; yy--) {
                    for (int xx = 0; xx < CELL_GRID_SIZE + 1; xx++) {
                        sbyte height = (sbyte)(zstdHeight[bA++]);
                        last += height;
                        if (xx == 0) { lastEdge = last; }

                        float xxx = -xx * (CELL_SIZE / (float)(CELL_GRID_SIZE));
                        float yyy = (CELL_GRID_SIZE - yy) * (CELL_SIZE / (float)(CELL_GRID_SIZE)); // I do not want to talk about this coordinate swap
                        float zzz = last * 8f * GLOBAL_SCALE;
                        Vector3 position = new Vector3(xxx, zzz, yyy) + centerOffset;
                        Int2 grid = new Int2(xx, yy);

                        float iii = (sbyte)zstdNormal[bB++];
                        float jjj = (sbyte)zstdNormal[bB++];
                        float kkk = (sbyte)zstdNormal[bB++];

                        Byte4 color = new Byte4(Byte.MaxValue); // Default
                        if (zstdColor != null) {
                            color = new Byte4(zstdColor[bC++], zstdColor[bC++], zstdColor[bC++], byte.MaxValue);
                        }

                        vertices.Add(new TerrainVertex(position, grid, Vector3.Normalize(new Vector3(iii, jjj, kkk)), new Vector2(xx * (1f / CELL_GRID_SIZE), yy * (1f / CELL_GRID_SIZE)), color, ltex[Math.Min((xx) / 4, 15), Math.Min((CELL_GRID_SIZE - yy) / 4, 15)]));
                    }
                    last = lastEdge;
                }

                if (GENERATE_NICE_TERRAIN) {
                    // Gets and returns a TerrainVertex by it's grid position
                    TerrainVertex GetTerrainVertexByGrid(Int2 g) {
                        foreach (TerrainVertex vert in vertices) {
                            if (vert.grid == g) { return vert; }
                        }
                        return null;
                    }

                    /* Texture blend smoothing pass */
                    ushort[] nuTexIndices = new ushort[vertices.Count];
                    for (int v = 0; v < vertices.Count; v++) {
                        TerrainVertex vert = vertices[v];
                        // Get local grid
                        TerrainVertex[,] local = new TerrainVertex[3, 3];
                        for (int yy = -1; yy <= 1; yy++) {
                            for (int xx = -1; xx <= 1; xx++) {
                                Int2 g = new Int2(vert.grid.x + xx, vert.grid.y + yy);
                                local[xx + 1, yy + 1] = GetTerrainVertexByGrid(g);
                            }
                        }

                        // Count texture use in local grid
                        Dictionary<ushort, int> texCounts = new();
                        for (int yy = -1; yy <= 1; yy++) {
                            for (int xx = -1; xx <= 1; xx++) {
                                TerrainVertex clamp = local[xx + 1, yy + 1];
                                if (clamp == null) { clamp = local[1, yy + 1]; }
                                if (clamp == null) { clamp = local[xx + 1, 1]; }
                                if (clamp == null) { clamp = local[1, 1]; }
                                if (texCounts.ContainsKey(clamp.texture)) { texCounts[clamp.texture]++; } else { texCounts.Add(clamp.texture, 1); }
                            }
                        }

                        // Find most used texture in local grid
                        ushort big = ushort.MaxValue; // Has to be assigned, should never actually result in this outcome.
                        foreach (KeyValuePair<ushort, int> a in texCounts) {
                            bool test = true;
                            foreach (KeyValuePair<ushort, int> b in texCounts) {
                                if (a.Value < b.Value) { test = false; continue; }
                            }
                            if (test) { big = a.Key; break; }
                        }

                        // Store result to apply later
                        nuTexIndices[v] = big;
                    }

                    // We wait to apply the new texture indices until after calculating all of them so that we don't change the data while calculating off it.
                    for (int v = 0; v < vertices.Count; v++) {
                        TerrainVertex vert = vertices[v];
                        vert.texture = nuTexIndices[v];
                    }

                    /* Create our border vertex arrays. These are used 2-fold */
                    // We read adjacent cells border arrays to match their texture indices to create seamless blending between cels
                    // We also set these up before blending so that we can reado ur own border array instaed of having to search our vertices arrays multiple times for border vertices
                    for (int ii = 0; ii <= CELL_GRID_SIZE; ii++) {
                        borders[3, ii] = GetTerrainVertexByGrid(new Int2(ii, 0));
                        borders[1, ii] = GetTerrainVertexByGrid(new Int2(CELL_GRID_SIZE, ii));
                        borders[2, ii] = GetTerrainVertexByGrid(new Int2(ii, CELL_GRID_SIZE));
                        borders[0, ii] = GetTerrainVertexByGrid(new Int2(0, ii));
                    }

                    /* Border blending pass */
                    // We look at our bordering cells and IF they have already been fully generated we sample their texture indices to blend terrain seamlessly between cells
                    Cell[] adjacents = {
                        esm.GetCellByGrid(position + new Int2(-1, 0)),
                        esm.GetCellByGrid(position + new Int2(1, 0)),
                        esm.GetCellByGrid(position + new Int2(0, -1)),
                        esm.GetCellByGrid(position + new Int2(0, 1))
                    };
                    int[] ri = {  // Reverse index thing. Do not ask me how this works. I don't know.
                        3,
                        0,
                        1,
                        2 
                    };
                    for (int ii = 0; ii < adjacents.Length; ii++) {
                        if (adjacents[ii] != null && adjacents[ii].generated) {
                            for (int jj = 0; jj <= CELL_GRID_SIZE; jj++) {
                                borders[ii, jj].texture = adjacents[ii].borders[ri[ii], jj].texture; // Summons demons
                            }
                        }
                    }
                }

                /* Index Data */
                List<int> indices = new();                             // Full indices data, for vcol decal mult mesh
                Dictionary<UShort2, List<int>> sets = new();           // Segmented indices data, for regular meshes
                for (int yy = 0; yy < CELL_GRID_SIZE / 4; yy++) {
                    for (int xx = 0; xx < (CELL_GRID_SIZE / 4) - 1; xx++) {
                        // Pre-generate some sets to make optimizations possible below~
                        UShort2[] keys = {
                            new UShort2(ltex[xx, yy], ltex[xx + 1, yy]),
                            new UShort2(ltex[xx+1, yy], ltex[xx, yy])
                        };
                        if (keys[0].x != keys[0].y && !sets.ContainsKey(keys[0]) && !sets.ContainsKey(keys[1])) {
                            sets.Add(keys[0], new List<int>());
                        }

                    }
                }

                for (int yy = 0; yy < CELL_GRID_SIZE; yy++) {
                    for (int xx = 0; xx < CELL_GRID_SIZE; xx++) {
                        int[] quad = {
                            (yy * (CELL_GRID_SIZE + 1)) + xx,
                            (yy * (CELL_GRID_SIZE + 1)) + (xx + 1),
                            ((yy + 1) * (CELL_GRID_SIZE + 1)) + (xx + 1),
                            ((yy + 1) * (CELL_GRID_SIZE + 1)) + xx
                        };


                        int[,] tris = {
                            {
                                quad[(xx + (yy % 2) + 2) % 4],
                                quad[(xx + (yy % 2) + 1) % 4],
                                quad[(xx + (yy % 2) + 0) % 4]
                            },
                            {
                                quad[(xx + (yy % 2) + 0) % 4],
                                quad[(xx + (yy % 2) + 3) % 4],
                                quad[(xx + (yy % 2) + 2) % 4]
                            }
                        };

                        for (int t = 0; t < 2; t++) {
                            List<ushort> texs = new();
                            for (int i = 0; i < 3; i++) {
                                if (!texs.Contains(vertices[tris[t, i]].texture)) {
                                    texs.Add(vertices[tris[t, i]].texture);
                                }
                            }
                            UShort2[] pair = {
                                new UShort2(texs[0], texs.Count > 1 ? texs[1] : ushort.MaxValue),
                                new UShort2(texs.Count > 1 ? texs[1] : ushort.MaxValue, texs[0])
                            };
                            //if (texs.Count > 2) { Log.Error(0, $"Terrain Triangle in [{region}:{name}][{position.x},{position.y}] with more than 2 texture indices~~~ Ugly clamping!"); }

                            List<int> set = null;
                            if (pair[0].y == ushort.MaxValue) {
                                // Optimization! Slap any 1 material triangles into an existing material, doesn't matter what the combo is because we only use the relevant texture indice
                                foreach (var kvp in sets) {
                                    if (kvp.Key.x == pair[0].x || kvp.Key.y == pair[0].x) {
                                        set = kvp.Value; break;
                                    }
                                }
                            }

                            if (set == null) {
                                if (sets.ContainsKey(pair[0])) { set = sets[pair[0]]; } // Exact match
                                else if (sets.ContainsKey(pair[1])) { set = sets[pair[1]]; } // Optimization! The way we handle this, it's fine to flip it over
                                else { set = new(); sets.Add(pair[0], set); }
                            }

                            for (int i = 0; i < 3; i++) {
                                set.Add(tris[t, i]);
                                indices.Add(tris[t, i]);
                            }
                        }
                    }
                }

                /* Create TerrainMeshes */
                foreach (KeyValuePair<UShort2, List<int>> set in sets) {
                    /* Cull unused vertices and build new indices */
                    // This is a big optimization and compresses the size of these terrain meshes by quite a bit in some cases
                    List<TerrainVertex> cv = new();
                    List<int> ci = new();
                    
                    for(int ii=0;ii<set.Value.Count;ii++) {
                        int index = set.Value[ii];
                        TerrainVertex vert = vertices[index];

                        int re = cv.IndexOf(vert);
                        if (re != -1) { ci.Add(re); }
                        else {
                            cv.Add(vert);
                            ci.Add(cv.Count - 1);
                        }
                    }

                    /* Create terrain mesh and find textures */
                    TerrainData mesh = new TerrainData(region + ":" + name, int.Parse(landscape["landscape_flags"].ToString()), cv, ci);

                    string texDir = $"{MorrowindPath}\\Data Files\\textures\\";
                    string[] texPaths = new string[2];
                    for (int i = 0; i < 2; i++) {
                        ushort tex = set.Key.Array()[i];
                        string texPath = "CommonFunc\\DefaultTex\\def_missing.dds";     // Default is something stupid so it's obvious there was an error
                        JObject ltexRecord = esm.FindRecordByKey(ESM.Type.LandscapeTexture, "index", tex + "");
                        if (ltexRecord != null) {
                            texPath = texDir + ltexRecord["texture"].ToString().Replace(".tga", ".dds");
                        }
                        texPaths[i] = texPath;
                        mesh.texturesIndices[i] = tex;
                    }

                    /* Material Information */
                    mesh.mtd = "M[ARSN]_m";
                    mesh.material = Utility.PathToFileName(texPaths[0]) + "->" + Utility.PathToFileName(texPaths[1]);

                    /* Setup material textures */
                    const string blackTex = "CommonFunc\\DefaultTex\\def_black.dds";
                    const string greyTex = "CommonFunc\\DefaultTex\\def_grey.dds";
                    const string flatTex = "CommonFunc\\DefaultTex\\def_flat.dds";

                    mesh.textures.Add("g_DiffuseTexture", new KeyValuePair<string, Vector2>(texPaths[0], new Vector2(32f, 32f)));
                    mesh.textures.Add("g_DiffuseTexture2", new KeyValuePair<string, Vector2>(texPaths[1], new Vector2(32f, 32f)));
                    mesh.textures.Add("g_SpecularTexture", new KeyValuePair<string, Vector2>(blackTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_SpecularTexture2", new KeyValuePair<string, Vector2>(blackTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_ShininessTexture", new KeyValuePair<string, Vector2>(blackTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_ShininessTexture2", new KeyValuePair<string, Vector2>(blackTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_BumpmapTexture", new KeyValuePair<string, Vector2>(flatTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_BumpmapTexture2", new KeyValuePair<string, Vector2>(flatTex, new Vector2(32f, 32f)));
                    mesh.textures.Add("g_BlendMaskTexture", new KeyValuePair<string, Vector2>(greyTex, new Vector2(1f, 1f)));

                    terrain.Add(mesh);
                }

                /* Generate vertex color multiply decal mesh */
                // Blame From for this. Not supporting vertex colors in shaders is a crime against GPUs
                TerrainData multMesh = new TerrainData(region + ":" + name, int.Parse(landscape["landscape_flags"].ToString()), vertices, indices);
                multMesh.mtd = "M[A]_multiply";
                multMesh.material = "Color Multiply Decal Mesh";
                multMesh.textures.Add("g_DiffuseTexture", new KeyValuePair<string, Vector2>($"terrain_color_blend_map_{position.x.ToString().Replace("-", "n")}_{position.y.ToString().Replace("-","n")}.dds", new Vector2(1f, 1f)));

                /* Generate dds texture using vertex color data */
                Byte4[] colors = new Byte4[65 * 65];
                int cc = 0;
                for(int yy=CELL_GRID_SIZE;yy>=0;yy--) {
                    for (int xx = 0; xx <= CELL_GRID_SIZE; xx++) {
                        TerrainVertex vert = vertices[(yy*(CELL_GRID_SIZE+1))+xx];
                        const float reduction = 0.2f;
                        int r = byte.MaxValue - (byte)((byte.MaxValue - vert.color.x) * reduction);
                        int g = byte.MaxValue - (byte)((byte.MaxValue - vert.color.y) * reduction);
                        int b = byte.MaxValue - (byte)((byte.MaxValue - vert.color.z) * reduction);
                        colors[cc++] = new Byte4(r, g, b, Byte.MaxValue);
                    }
                }

                multMesh.color = CommonFunc.DDS.MakeTextureFromPixelData(colors, 65, 65, 512, 512, filterFlags: TEX_FILTER_FLAGS.CUBIC);

                terrain.Add(multMesh);
            }
            generated = true;
        }

        /* Returns a position in the cell that it is safe to spawn the player. This is used for debug menu to load you into cells! */
        public Vector3 getCenterOnCell() {
            /* Search for an NPC (not a creature) and place the player at their position. This should be a guaranteed safe spot to spawn at. */
            foreach(Content cnt in content) {
                if(cnt.type is not ESM.Type.Npc) { continue; }
                return cnt.position;
            }

            /* If we don't find an NPC (not terribly likely but still possible) then we use a creature. */
            foreach (Content cnt in content) {
                if (cnt.type is not ESM.Type.Npc) { continue; }
                return cnt.position;
            }

            /* Worst case! Just spawn dead center of cell. This is fairly unlikely to happen but possible. */
            return center;
        }

        /* Stupid thing to try and make this run a little faster */
        // 
        private static ushort[] preOpt = new ushort[512]; // Optimization, store any results we have in this array so we don't have to search the list for them
        private static ushort DeDupeTextureIndex(ESM esm, ushort a) {
            if (a < preOpt.Length && preOpt[a] != 0) { return preOpt[a]; }

            TerrainTexture ttex = esm.GetTerrainTextureByIndex(a);
            if (ttex == null) { return a; }
            ushort res = a;
            foreach (TerrainTexture ottex in esm.terrainTextures) {
                if (ttex != ottex && ttex.texture.ToLower() == ottex.texture.ToLower()) {
                    res = ttex.index < ottex.index ? ttex.index : ottex.index; // Lower index overrides
                    break;
                }
            }

            if (res < preOpt.Length) { preOpt[a] = res; }
            return res;
        }
    }

    public class Content {
        public readonly string id;
        public readonly ESM.Type type;

        public readonly string mesh;

        public readonly Vector3 position, rotation;
        public readonly float scale;

        public DoorContent door;
        public Content(ESM esm, JObject data) {
            id = data["id"].ToString();

            TypedRecord tr = esm.FindRecordById(id);
            type = tr.type;

            if (tr.record["mesh"] != null) { mesh = tr.record["mesh"].ToString(); }

            float x = float.Parse(((JArray)(data["translation"]))[0].ToString());
            float z = float.Parse(((JArray)(data["translation"]))[1].ToString());
            float y = float.Parse(((JArray)(data["translation"]))[2].ToString());

            float i = float.Parse(((JArray)(data["rotation"]))[0].ToString());
            float j = float.Parse(((JArray)(data["rotation"]))[1].ToString());
            float k = float.Parse(((JArray)(data["rotation"]))[2].ToString());

            /* ????????? ??????????????? ????????????? !?!?!?!?! ????????? */
            /* If I spend any more time on this I'm going to break my monitor. I don't know what, how, or why but something is fubar with rotations */
            const float q = (float)(Math.PI / 2);
            // Y -180 to -90
            if (q > k) {
                i = -i;
                j = -j;
            }
            // Y -90 to -0
            else if (q*2 > k) {
                float ii = i;
                float jj = j;
                i = -jj;
                j = -ii;
            }
            // Y 0 to 90
            else if (q*3 > k) {
                float ii = i;
                float jj = j;
                i = -jj;
                j = -ii;
            }
            // Y 90 to 180
            else {
                i = -i;
                j = -j;
            }
            k -= (float)Math.PI;

           /* Quaternion version of this cursed garbage
            * Quaternion ToQuaternion(Vector3 v) {

                float cy = (float)Math.Cos(v.Z * 0.5);
                float sy = (float)Math.Sin(v.Z * 0.5);
                float cp = (float)Math.Cos(v.Y * 0.5);
                float sp = (float)Math.Sin(v.Y * 0.5);
                float cr = (float)Math.Cos(v.X * 0.5);
                float sr = (float)Math.Sin(v.X * 0.5);

                return new Quaternion {
                    W = (cr * cp * cy + sr * sp * sy),
                    X = (sr * cp * cy - cr * sp * sy),
                    Y = (cr * sp * cy + sr * cp * sy),
                    Z = (cr * cp * sy - sr * sp * cy)
                };

            }

            Vector3 ToEulerAngles(Quaternion q) {
                Vector3 angles = new();

                // roll / x
                double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
                double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
                angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

                // pitch / y
                double sinp = 2 * (q.W * q.Y - q.Z * q.X);
                if (Math.Abs(sinp) >= 1) {
                    angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
                } else {
                    angles.Y = (float)Math.Asin(sinp);
                }

                // yaw / z
                double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
                double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
                angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

                return angles;
            }

            Quaternion q = ToQuaternion(new Vector3(i, j, k - (float)Math.PI));
            q = new Quaternion(q.X, q.Z, q.Y, q.W);
            Vector3 v = ToEulerAngles(q);*/

            position = new Vector3(x, y, z) * GLOBAL_SCALE;
            rotation = new Vector3(i, k, j) * (float)(180 / Math.PI);
            scale = data["scale"]!=null?float.Parse(data["scale"].ToString()):1f;   // Another banger

            /* Door stuff */
            door = type == ESM.Type.Door?new DoorContent(esm, data):null;
        }
    }

    public class DoorContent {
        public enum DoorType {
            Load, Decoration
        }

        DoorType type;               // Either it's a load door that warps you somewhere or it's decoration that's animated to open/close
        Cell cell;                   // Warp to...
        Vector3 position, rotation;  // Warp to...
        public DoorContent(ESM esm, JObject data) {
            if(data["door_destination_coords"] != null) {
                type = DoorType.Load;
                JArray coords = (JArray)(data["door_destination_coords"]);

                float x = float.Parse(coords[0].ToString());
                float z = float.Parse(coords[1].ToString());
                float y = float.Parse(coords[2].ToString());

                float i = float.Parse(coords[3].ToString());
                float j = float.Parse(coords[4].ToString());
                float k = float.Parse(coords[5].ToString());

                position = new Vector3(x, z, y);
                rotation = new Vector3(i, k, j);
            }
            else {
                type = DoorType.Decoration;
                cell = null;
                position = Vector3.Zero;
                rotation = Vector3.Zero;
            }

            if (data["door_destination_cell"] != null) {
                cell = esm.GetCellByName(data["door_destination_cell"].ToString());
            }
            else {
                cell = esm.GetCellByCoordinate(position);
            }
        }
    }

    public class TerrainData {
        public string name;
        public int flag;
        public List<TerrainVertex> vertices;
        public List<int> indices;
        public string material, mtd;
        public byte[] color;
        public Dictionary<string, KeyValuePair<string, Vector2>> textures;
        public ushort[] texturesIndices;

        public TerrainData(string name, int flag, List<TerrainVertex> vertices = null, List<int> indices = null) {
            this.name = name;
            this.flag = flag;
            this.vertices = vertices != null ? vertices : new();
            this.indices = indices != null ? indices : new();
            textures = new();
            texturesIndices = new ushort[2];
        }
    }

    public class TerrainVertex {
        public Vector3 position;
        public Int2 grid; // position on this cells grid
        public Vector3 normal;
        public Vector2 coordinate;
        public Byte4 color; // Bytes of a texture that contains the converted vertex color information

        public ushort texture;

        public TerrainVertex(Vector3 position, Int2 grid, Vector3 normal, Vector2 coordinate, Byte4 color, ushort texture) {
            this.position = position;
            this.grid = grid;
            this.normal = normal;
            this.coordinate = coordinate;
            this.color = color;
            this.texture = texture;
        }
    }

    public class TerrainTexture {
        public string texture;
        public ushort index;

        public TerrainTexture(string texture, ushort index) {
            this.texture = texture; this.index = index;
        }
    }
    public class TypedRecord {
        public ESM.Type type;
        public JObject record;

        public TypedRecord(ESM.Type t, JObject r) {
            type = t;
            record = r;
        }
    }
}
