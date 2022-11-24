using CommonFunc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Linq;

using SoulsFormats;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static CommonFunc.Const;
using SoulsIds;

namespace PortJob {
    class PortJob {
        public static readonly ESM.Type[] CONVERT_TO_MAP = { ESM.Type.Static, ESM.Type.Container };
        public static readonly ESM.Type[] CONVERT_TO_OBJ = { ESM.Type.Door };
        public static readonly ESM.Type[] CONVERT_ALL = { ESM.Type.Static, ESM.Type.Container, ESM.Type.Door };

        static void Main(string[] args) {
            //BND4 bnd = BND4.Read(@"G:\Steam\steamapps\common\DARK SOULS III\Game\mod\map\m54_00_00_00\m54_00_00_00_009000.mapbnd.dcx");
            //FLVER2 flver = FLVER2.Read(bnd.Files.First(x => x.Name.EndsWith(".flver")).Bytes);
            CheckIsDarkSouls3IsRunning();
            DateTime startTime = DateTime.Now;
            SetupPaths();
            Log.SetupLogStream();

            Convert();
            //FLVER2 myFlver = FLVER2.Read("C:\\Games\\steamapps\\common\\DARK SOULS III\\Game\\mod\\map\\m54_00_00_00\\m54_00_00_00_000009-mapbnd-dcx\\m54_00_00_00_000009.flver");
            //FLVER2 fromFlver = FlverSearch(Directory.GetFiles("C:\\Games\\steamapps\\common\\DARK SOULS III\\Game\\map", "*.mapbnd.dcx", SearchOption.AllDirectories));

            TimeSpan length = DateTime.Now - startTime;
            Log.Info(0, $"Porting time: {length}");
            Log.CloseWriter();

        }

        /* Random testing and debug stuff */
        private static FLVER2 FlverSearch(string[] files) {
            foreach (string file in files) {
                BND4 bnd = BND4.Read(file);

                foreach (BinderFile binderFile in bnd.Files) {
                    if (binderFile.Name.ToLower().Contains("flver")) {
                        FLVER2 flver = FLVER2.Read(binderFile.Bytes);

                        /*foreach(FLVER2.Mesh mesh in flver.Meshes) {
                            foreach(FLVER.Vertex vert in mesh.Vertices) {
                                if(vert.Colors.Count > 1) {
                                    Console.WriteLine("Multiple Vertex Colors " + file);
                                    Console.WriteLine("    " + flver.Materials[mesh.MaterialIndex].MTD + " :: " + flver.Materials[mesh.MaterialIndex].Name);
                                    Console.WriteLine("    [" + vert.Colors[0].R + ", " + vert.Colors[0].G + ", " + vert.Colors[0].B + "]");
                                    Console.WriteLine("    [" + vert.Colors[1].R + ", " + vert.Colors[1].G + ", " + vert.Colors[1].B + "]");
                                    break;
                                }
                                if (vert.Colors.Count > 0 && vert.Colors[0].R != vert.Colors[0].B && vert.Colors[0].R is not(1f or 0f) && vert.Colors[0].G is not (1f or 0f)) {
                                    Console.WriteLine("Varied vertex color data in: " + file);
                                    Console.WriteLine("    " + flver.Materials[mesh.MaterialIndex].MTD + " :: " + flver.Materials[mesh.MaterialIndex].Name);
                                    Console.WriteLine("    [" + vert.Colors[0].R + ", " + vert.Colors[0].G + ", " + vert.Colors[0].B + "]");
                                    break;
                                }
                            }
                        }*/

                        //foreach (FLVER2.Material mat in flver.Materials) {
                            //Console.WriteLine(file);
                            //if (mat.MTD.ToLower().EndsWith("m[arsn]_m.mtd"))
                                //return flver;


                            //   Console.WriteLine($"Material found {binderFile.Name} {file} {mat.Name}");

                            //if (mat.MTD.ToLower().Contains("m[a]"))
                            //    Console.WriteLine($"Material found {binderFile.Name} {file} {mat.Name}");
                        //}
                    }
                }
            }
            return null;
        }

        private static void CheckIsDarkSouls3IsRunning() {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes) {
                if (process.MainWindowTitle is "DARK SOULS™ III" or "DARK SOULS III") {
                    Console.WriteLine("Dark Souls III is running! Close the game or exit the map and press any key or close Dark Souls III to continue");
                    while (!process.HasExited) {
                        if (Console.KeyAvailable)
                            break;

                        Thread.Sleep(500);
                    }

                    Console.WriteLine("Resuming");
                }
            }
        }

        private static void SetupPaths() {
            Settings.InitSettings();
        }

        private static void Convert() {
            /* Load overrides */
            Override.Override.load();

            /* Load ESM */
            ESM esm = new(MorrowindPath + "morrowind.json");

            /* Call Layout & Layint to calculate data we will use to create all exterior and interior MSBs. */
            List<Layout> layouts = Layout.Calculate(esm);
            List<Layint> layints = Layint.Calculate(esm, MAX_MSB_COUNT - layouts.Count);

            /* Call Mass Convert to convert all models, textures, and collisions and return info about all those files. */
            Cache cache = MassConvert.Convert(esm, layouts, layints);

            List<MSBData> msbs = new();
            List<NVAData> nvas = new();
            HashSet<TextureInfo> extTextures = new();
            HashSet<TextureInfo> intTextures = new();

            /* Generate Exterior MSBs from layouts */
            {
                const int area = 54;

                for (int block=0;block<layouts.Count;block++) {
                    if (!DEBUG_GEN_EXT_LAYOUT(block)) { continue; } //for rapid debugging

                    Layout layout = layouts[block];
                    MSB3 msb = new();
                    NVA nva = new();   //One nva per msb. I put this up here so you can easily add the navmeshes in the loop. 

                    Log.Info(0, $"=== Generating Exterior MSB[{block}] === [{layout.cells.Count} cells]", "test");

                    /* File resource lists */
                    HashSet<ModelInfo> usedMapPieces = new();
                    HashSet<CollisionInfo> usedCollision = new();
                    List<TerrainInfo> usedTerrain = new();         // Terrain is 1 to 1 usage so no need to use hashset. Also no need to track counts below.

                    /* Part counters */
                    Dictionary<ModelInfo, int> mapPieceCounts = new();
                    int GetMapPieceCount(ModelInfo modelInfo) {
                        if(mapPieceCounts.ContainsKey(modelInfo)) { return mapPieceCounts[modelInfo]++; }
                        else { mapPieceCounts.Add(modelInfo, 1); return 0; }
                    }
                    Dictionary<CollisionInfo, int> collisionCounts = new();
                    int GetCollisionCount(CollisionInfo collisionInfo) {
                        if (collisionCounts.ContainsKey(collisionInfo)) { return collisionCounts[collisionInfo]++; } else { collisionCounts.Add(collisionInfo, 1); return 0; }
                    }
                    Dictionary<ObjectInfo, int> objectCounts = new();
                    int GetObjectCount(ObjectInfo objectInfo) {
                        if (objectCounts.ContainsKey(objectInfo)) { return objectCounts[objectInfo]++; } else { objectCounts.Add(objectInfo, 1); return 0; }
                    }


                    /* Pick the debug spawn point for this MSB. We try to place this in a city or named cell */
                    Cell spawnCell = null;
                    for (int c = 0; c < layout.cells.Count; c++) {
                        if (c > DEBUG_MAX_EXT_CELLS) { break; }
                        Cell cell = layout.cells[c];
                        if (spawnCell == null && cell.name != "" && !Override.DebugCells.IsAvoid(cell.name)) { spawnCell = cell; }
                        if(Override.DebugCells.IsPrefer(cell.name)) { spawnCell = cell; }
                    }
                    spawnCell.Load(esm);

                    /* Create player default spawn point */
                    MSB3.Part.Player player = new();
                    player.ModelName = "c0000";
                    player.Position = spawnCell.getCenterOnCell();
                    player.Name = "c0000_0000";
                    msb.Parts.Players.Add(player);

                    /* MSB population pass */
                    for (int c = 0; c < layout.cells.Count; c++) {
                        if (c > DEBUG_MAX_EXT_CELLS) { break; }

                        Cell cell = layout.cells[c];
                        cell.Load(esm);
                        Log.Info(0, "Populating Exterior Cell: " + cell.region + (cell.name != "" ? ":" + cell.name : "") + " -> [" + cell.position.x + ", " + cell.position.y + "]", "test");

                        /* Add cell terrain map piece */
                        TerrainInfo terrainInfo = cache.GetTerrainInfo(cell.position);

                        MSB3.Part.MapPiece terrain = new();
                        terrain.ModelName = $"m{terrainInfo.id:D6}";
                        terrain.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{terrainInfo.id:D6}.SIB";
                        terrain.Position = cell.center;
                        terrain.Rotation = Vector3.Zero;
                        terrain.MapStudioLayer = uint.MaxValue;
                        for (int k = 0; k < cell.drawGroups.Length; k++) {
                            terrain.DrawGroups[k] = cell.drawGroups[k];
                            terrain.DispGroups[k] = cell.drawGroups[k];
                            terrain.BackreadGroups[k] = 0;
                        }
                        terrain.ShadowSource = true;
                        terrain.DrawByReflectCam = true;
                        terrain.Name = terrain.ModelName + "_0000";   // Terrains are unique so no need to worry about counts
                        terrain.UnkE0E = -1;
                        terrain.LodParamID = 19; //Param for: Don't switch to LOD models 

                        msb.Parts.MapPieces.Add(terrain);
                        usedTerrain.Add(terrainInfo);

                        /* Add cell terrain collision */
                        MSB3.Part.Collision terrainCol = new();
                        terrainCol.HitFilterID = 8;
                        terrainCol.ModelName = $"h{terrainInfo.collision.id:D6}";
                        terrainCol.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                        terrainCol.Position = terrain.Position;
                        terrainCol.Rotation = terrain.Rotation;
                        terrainCol.MapStudioLayer = uint.MaxValue;
                        for (int k = 0; k < cell.drawGroups.Length; k++) {
                            terrainCol.DrawGroups[k] = cell.drawGroups[k];
                            terrainCol.DispGroups[k] = cell.drawGroups[k];
                            terrainCol.BackreadGroups[k] = cell.drawGroups[k];
                        }
                        terrainCol.Name = terrainCol.ModelName + "_0000"; // Same as abovev
                        terrainCol.LodParamID = -1;
                        terrainCol.UnkE0E = -1;

                        msb.Parts.Collisions.Add(terrainCol);
                        usedCollision.Add(terrainInfo.collision);
                        

                        /* Static map pieces and collision */
                        foreach (Content content in cell.content) {
                            if (!CONVERT_TO_MAP.Contains(content.type)) { continue; }   // Only process things we want as static world meshes
                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes

                            /* Add map piece */
                            ModelInfo modelInfo = cache.GetModelInfo(content.mesh);
                            int mpCount = GetMapPieceCount(modelInfo);

                            MSB3.Part.MapPiece mp = new();
                            mp.ModelName = $"m{modelInfo.id:D6}";
                            mp.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{modelInfo.id:D6}.SIB"; //put the right number here
                            mp.Position = content.position;
                            mp.Rotation = content.rotation;
                            mp.Scale = new Vector3(content.scale);
                            mp.MapStudioLayer = uint.MaxValue;
                            for (int k = 0; k < cell.drawGroups.Length; k++) {
                                mp.DrawGroups[k] = cell.drawGroups[k];
                                mp.DispGroups[k] = cell.drawGroups[k];
                                mp.BackreadGroups[k] = 0;
                            }
                            mp.ShadowSource = true;
                            mp.DrawByReflectCam = true;
                            mp.Name = $"{mp.ModelName}_{mpCount:D4}";
                            mp.UnkE0E = -1;
                            mp.LodParamID = 19; //Param for: Don't switch to LOD models 
                            
                            msb.Parts.MapPieces.Add(mp);
                            usedMapPieces.Add(modelInfo);

                            /* Add collision if there is any */
                            CollisionInfo collisionInfo = modelInfo.GetCollision(1f);
                            if (collisionInfo != null) {
                                int colCount = GetCollisionCount(collisionInfo);

                                MSB3.Part.Collision col = new();
                                col.HitFilterID = 8;
                                col.ModelName = $"h{collisionInfo.id:D6}";
                                col.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                                col.Position = mp.Position;
                                col.Rotation = mp.Rotation;
                                col.Scale = mp.Scale;
                                col.MapStudioLayer = uint.MaxValue;
                                for (int k = 0; k < cell.drawGroups.Length; k++) {
                                    col.DrawGroups[k] = cell.drawGroups[k];
                                    col.DispGroups[k] = cell.drawGroups[k];
                                    col.BackreadGroups[k] = cell.drawGroups[k];
                                }

                                col.Name = $"{col.ModelName}_{colCount:D4}";
                                col.LodParamID = -1;
                                col.UnkE0E = -1;

                                msb.Parts.Collisions.Add(col);
                                usedCollision.Add(collisionInfo);
                            }
                        }
                        
                        /* Objects */
                        foreach(Content content in cell.content) {
                            if (!CONVERT_TO_OBJ.Contains(content.type)) { continue; }   // Doors please
                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes

                            ObjectInfo objectInfo = cache.GetObjectInfo(content.id);
                            int objCount = GetObjectCount(objectInfo);

                            MSB3.Part.Object obj = new();
                            obj.ModelName = $"o{objectInfo.id:D6}";
                            obj.SibPath = "";
                            obj.Position = content.position;
                            obj.Rotation = content.rotation;
                            obj.Scale = new Vector3(content.scale);
                            obj.MapStudioLayer = uint.MaxValue;
                            for (int k = 0; k < cell.drawGroups.Length; k++) {
                                obj.DrawGroups[k] = cell.drawGroups[k];
                                obj.DispGroups[k] = cell.drawGroups[k];
                                obj.BackreadGroups[k] = 0;
                            }
                            obj.ShadowDest = true;
                            obj.DrawByReflectCam = true;
                            obj.Name = $"{obj.ModelName}_{objCount:D4}";
                            obj.UnkE0E = -1;
                            obj.LodParamID = 19; //Param for: Don't switch to LOD models 

                            msb.Parts.Objects.Add(obj);
                        }
                    }

                    /* Auto-generate model resources section of MSB */
                    AutoResource.Generate(area, block, msb);

                    /* Copy resource files into MSB directory and add textures to final bnd list */
                    string msbDir = $"{OutputPath}map\\m{area:D2}_{block:D2}_00_00\\";

                    foreach (ModelInfo modelInfo in usedMapPieces) { // Map pieces
                        FLVER2 flver = FLVER2.Read(modelInfo.path);
                        string flverName = $"m{area:D2}_{block:D2}_{0:D2}_{0:D2}_{modelInfo.id:D6}";

                        BND4 bnd = new() { Compression = DCX.Type.DCX_DFLT_10000_44_9 };
                        bnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, $"{flverName}.flver", flver.Write()));
                        bnd.Write($"{msbDir}{flverName}.mapbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
                        foreach(TextureInfo textureInfo in modelInfo.textures) { extTextures.Add(textureInfo); } // Textures
                    }

                    foreach (TerrainInfo terrainInfo in usedTerrain) { // Terrain
                        FLVER2 flver = FLVER2.Read(terrainInfo.path);
                        string flverName = $"m{area:D2}_{block:D2}_{0:D2}_{0:D2}_{terrainInfo.id:D6}";

                        BND4 bnd = new() { Compression = DCX.Type.DCX_DFLT_10000_44_9 };
                        bnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, $"{flverName}.flver", flver.Write()));
                        bnd.Write($"{msbDir}{flverName}.mapbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
                        foreach (TextureInfo textureInfo in terrainInfo.textures) { extTextures.Add(textureInfo); } // Textures
                    }

                    foreach(CollisionInfo collisionInfo in usedCollision) { // Collision
                        string colPath = $"{msbDir}h{area:D2}_{block:D2}_{0:D2}_{0:D2}_{collisionInfo.id:D6}.hkx.dcx";
                        if (File.Exists(colPath)) { File.Delete(colPath); }
                        File.Copy(collisionInfo.path, colPath);
                    }

                    /* Generate Object Files */
                    foreach(ObjectInfo objectInfo in cache.objects) {
                        /* Generate objBnd */
                        TPF tpf = TPF.Read(objectInfo.model.textures[0].path); // Merge all used tpfs into a single tpf
                        tpf.Compression = DCX.Type.None;
                        tpf.Encoding = 0x1;
                        for (int i=1;i<objectInfo.model.textures.Count;i++) {
                            TextureInfo textureInfo = objectInfo.model.textures[i];
                            TPF mortpf = TPF.Read(textureInfo.path);
                            foreach (TPF.Texture tex in mortpf.Textures) {
                                tpf.Textures.Add(tex);
                            }
                        }
                        FLVER2 flver = FLVER2.Read(objectInfo.model.path);
                        byte[] hkxdcx = File.ReadAllBytes(objectInfo.model.GetCollision(1f).path);
                        byte[] hkx = DCX.Decompress(hkxdcx);

                        BND4 objBnd = new BND4();
                        string objPath = $"obj\\o{0:D2}\\o{0:D2}{objectInfo.id:D4}\\o{0:D2}{objectInfo.id:D4}";
                        objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 100, $"{objPath}.tpf", tpf.Write()));
                        objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, $"{objPath}.flver", flver.Write()));
                        objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 300, $"{objPath}.hkx", hkx));                     // Crashing?
                        objBnd.Write($"{OutputPath}obj\\o{0:D2}{objectInfo.id:D4}.objbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
                    }

                    //AddTempNavMeshToNVA(block, area, nva);
                    Log.Info(0, $"Completed: m{area:D2}_{block:D2}_00_00.msb");
                    Log.Info(2, "MapPieces: " + msb.Parts.MapPieces.Count);
                    Log.Info(2, "Collisions: " + msb.Parts.Collisions.Count);

                    msbs.Add(new MSBData(area, block, msb, spawnCell.name));
                    nvas.Add(new NVAData(area, block, nva));
                    Log.Info(0, "\n");
                }
            }
            
            /* Write msbs to file and build packages */
            foreach (MSBData msb in msbs) {
                string msbPath = $"{OutputPath}map\\MapStudio\\m{msb.area:D2}_{msb.block:D2}_00_00.msb.dcx";
                Log.Info(0, "Writing MSB to: " + msbPath);
                msb.msb.Write(msbPath, DCX.Type.DCX_DFLT_10000_44_9);
                Utility.PackAreaCol(msb.area, msb.block);
                //Utility.PackTestColAndNavMeshes(msb.area, msb.block);
            }

            /* Write custom mtdbnd */
            string mtdDir = OutputPath + "mtd\\";
            string mtdPath = mtdDir + "allmaterialbnd.mtdbnd.dcx";
            if (!Directory.Exists(mtdDir)) { Directory.CreateDirectory(mtdDir); }
            if (File.Exists(mtdPath)) { File.Delete(mtdPath); }
            byte[] hBytes = Utility.GetEmbededResourceBytes("CommonFunc.Resources.mtdbnd.dcx");
            Log.Info(0, "Writing MTDBND to: " + mtdPath);
            File.WriteAllBytes(mtdPath, hBytes);

            foreach (NVAData nva in nvas) {
                string nvaPath = $"{OutputPath}map\\m{nva.area:D2}_{nva.block:D2}_00_00\\m{nva.area:D2}_{nva.block:D2}_00_00.nva.dcx";
                Log.Info(0, "Writing MSB to: " + nvaPath);
                nva.nva.Write(nvaPath, DCX.Type.DCX_DFLT_10000_44_9);

                Utility.PackAreaCol(nva.area, nva.block);
            }

            PackTextures(54, extTextures); //PackTextures(30, intTextures); // This should be run once per area, currently needs to be reworked to support some like area division stuff but not important right now

            /* Generate and write loadlists */
            string mapViewListPath = $"{OutputPath}map\\mapviewlist.loadlistlist";
            string worldMsbListPath = $"{OutputPath}map\\worldmsblist.worldloadlistlist";
            string mapViewList = "", worldMsbList = "";
            foreach (MSBData msb in msbs) {

                mapViewList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb {msb.debugName}\r\n";
                worldMsbList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb	#m{msb.area:D2}B1yI‚Ì‰¤é_1z 0\r\n";
            }
            mapViewList += "\0\0\0\0\0\0\0\0\0\0\0\0"; // Buncha fucking \0 idk why
            worldMsbList += "\0\0\0\0\0\0\0\0\0\0\0\0";

            if (File.Exists(mapViewListPath)) { File.Delete(mapViewListPath); }
            if (File.Exists(worldMsbListPath)) { File.Delete(worldMsbListPath); }

            File.WriteAllText(mapViewListPath, mapViewList);
            File.WriteAllText(worldMsbListPath, worldMsbList);
        }
        private static void MakeTestEnemy(int c, Cell cell, MSB3 msb) {

            /* Enemy for testing */
            string eModel = "c1100";
            string eName = $"_{c:D4}";

            MSB3.Part.Enemy enemy = new();
            //enemy.CollisionName = flat.Name;
            enemy.ThinkParamID = 110050;
            enemy.NPCParamID = 110010;
            enemy.TalkID = 0;
            enemy.CharaInitID = -1;
            enemy.UnkT78 = 128;
            enemy.UnkT84 = 1;
            enemy.Name = eModel + eName;
            enemy.SibPath = "";
            enemy.ModelName = eModel;
            enemy.Position = cell.center + new Vector3(0f, 5f, 0f);
            enemy.MapStudioLayer = 4294967295;
            for (int k = 0; k < cell.drawGroups.Length; k++) {
                enemy.DrawGroups[k] = 0;
                enemy.DispGroups[k] = 0;
                enemy.BackreadGroups[k] = 0;
            }
            enemy.LodParamID = -1;
            enemy.UnkE0E = -1;

            msb.Parts.Enemies.Add(enemy);
        }
        private static MSB3.Part.Collision AddTestcol(string cModel, int area, int block, Cell cell, Vector3 OFFSET, Vector3 ROTATION, MSB3 msb, Dictionary<string, int> partMap) {
            partMap.Add(cModel, 0);
            /* Flat ground for testing */
            MSB3.Part.Collision flat = new();
            flat.HitFilterID = 8;
            flat.ModelName = cModel;
            flat.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
            flat.Position = cell.center + OFFSET + new Vector3(0f, -15f, 0f);
            flat.Rotation = ROTATION;
            flat.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < cell.drawGroups.Length; k++) {
                flat.DrawGroups[k] = cell.drawGroups[k];
                flat.DispGroups[k] = cell.drawGroups[k];
                flat.BackreadGroups[k] = cell.drawGroups[k];
            }

            flat.Name = cModel; // + cName;
            flat.LodParamID = -1;
            flat.UnkE0E = -1;

            WriteTestCollision(cModel, area, block);
            msb.Parts.Collisions.Add(flat);

            /* Flat connect collision for testing */
            for (int k = 0; k < cell.connects.Count; k++) {
                string ccModel = cModel;

                if (!partMap.ContainsKey(ccModel))
                    throw new Exception("Connect col must reference an exisiting collision");

                string ccName = "_" + (partMap[ccModel]++.ToString("D4"));
                //ccName = "_0000";
                //ccName = "_" + (partMap[ccModel]++.ToString("D4"));
                //} else {
                //    ccName = "_0000";
                //    partMap.Add(ccModel, 1);
                //}

                MSB3.Part.ConnectCollision con = new();
                con.CollisionName = flat.Name;
                con.MapID[0] = (byte)area;
                con.MapID[1] = (byte)(cell.connects[k].id);
                con.MapID[2] = 0;
                con.MapID[3] = 0;
                con.Name = ccModel + ccName;
                //con.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB"; // Looks like connnect collision does not ever use sibs
                con.ModelName = ccModel;
                con.Position = cell.center + OFFSET + new Vector3(0f, 5f, 0f);
                con.Rotation = ROTATION;
                con.MapStudioLayer = 4294967295; // Not a clue what this does... Should probably ask about it
                for (int l = 0; l < cell.drawGroups.Length; l++) {
                    con.DrawGroups[l] = cell.drawGroups[l];
                    con.DispGroups[l] = cell.drawGroups[l];
                    con.BackreadGroups[l] = 0; // Seems like DS3 doesn't use this for collision at all
                }
                con.LodParamID = -1;
                con.UnkE0E = -1;

                msb.Parts.ConnectCollisions.Add(con);
            }
            return flat;
        }
        /// <summary>
        /// Adds temporary pre-generated flat nav mesh to NVA
        /// </summary>
        /// <param name="block"></param>
        /// <param name="area"></param>
        /// <param name="nva"></param>
        private static void AddTempNavMeshToNVA(int block, int area, NVA nva) {

            /* Just add one Navmesh to each nva. Model and Name are not a string, so no '_0000' format, and we have to use a unique ID here. */
            int nModelID = 0;
            if (block == 8)
                nModelID = 91;

            if (int.TryParse($"{area}{block}{nModelID:D6}", out int id)) //This is just for testing so we don't go over int.MaxValue.
            {
                nva.Navmeshes.Add(new NVA.Navmesh() {
                    NameID = id,
                    ModelID = nModelID,
                    Position = block == 3 ? new Vector3(798, 3, -185) : new Vector3(), //new Vector3(716, 2, -514),// player.Position, //using player position, here. Change this to cell.center in loop.
                    VertexCount = 1,
                    //Unk38 = 12399,
                    //Unk4C = true
                });
            }

            //WriteTestNavMesh(nModelID, area, block);

            /* There has to be an entry for each vertex in each navmesh in nav.Navmashes */
            foreach (NVA.Navmesh navmesh in nva.Navmeshes) {
                for (int j = 0; j < navmesh.VertexCount; j++) {
                    nva.Entries1.Add(new NVA.Entry1());
                }
            }
        }

        /// <summary>
        /// Copies the pre-rendered flat test collision from embedded resources into the map\mXX_XX_00_00\hkx\col\ folder so it can be packaged later.
        /// </summary>
        /// <param name="cModel"></param>
        /// <param name="area"></param>
        /// <param name="block"></param>
        /// <exception cref="Exception"></exception>
        private static void WriteTestCollision(string cModel, int area, int block) {
            /* Setup area_block and output path*/
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";

            /* Write the high col bhd/bdt pair */
            if (!int.TryParse(cModel.Substring(1), out int hModelId))
                throw new Exception($"Could not parse cModel ID: {cModel}");

            string hPreGenPath = $"PortJob.TestCol.h30_00_00_00_{0:D6}.hkx";
            string hPath = $"h{area_block}_00_00";
            byte[] hBytes = Utility.GetEmbededResourceBytes(hPreGenPath);
            hBytes = DCX.Compress(hBytes, DCX.Type.DCX_DFLT_10000_44_9); //File is compressed inside the bdt.
            Directory.CreateDirectory($"{OutputPath}\\map\\{mapName}\\");
            File.WriteAllBytes($"{OutputPath}\\map\\{mapName}\\{hPath}_{hModelId:D6}.hkx.dcx", hBytes);
        }
        /// <summary>
        /// Copies the pre-rendered flat test nav mesh from embedded resources into the map\mXX_XX_00_00\hkx\nav\ folder so it can be packaged later.
        /// </summary>
        /// <param name="cModel"></param>
        /// <param name="area"></param>
        /// <param name="block"></param>
        /// <exception cref="Exception"></exception>
        private static void WriteTestNavMesh(int nModelId, int area, int block) {
            /* Setup area_block and output path*/
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";

            /* Write the nav mesh bnd */
            string nPreGenPath = $"PortJob.TestCol.n{area_block}_00_00_{nModelId:D6}.hkx"; //:fatcat:
            if (block is not (3 or 8))
                nPreGenPath = $"PortJob.TestCol.n54_03_00_00_{0:D6}.hkx";

            string nPath = $"n{area_block}_00_00";
            byte[] nBytes = Utility.GetEmbededResourceBytes(nPreGenPath);
            Directory.CreateDirectory($"{OutputPath}\\map\\{mapName}\\hkx\\nav\\");
            File.WriteAllBytes($"{OutputPath}\\map\\{mapName}\\hkx\\nav\\{nPath}_{nModelId:D6}.hkx", nBytes);
        }

        //Going to keep this until we know which version works best.  
        private static void CallFBXConverter(string fbxPath, string flverPath, string tpfDir) {
            string cmdArgs = $"{fbxPath.Replace(" ", "%%")} | {flverPath.Replace(" ", "%%")} | {tpfDir.Replace(" ", "%%")} | {OutputPath.Replace(" ", "%%")} | {MorrowindPath.Replace(" ", "%%")} | {GLOBAL_SCALE}"; //the double quotes here serve to provide double quotes to the arg paths, in case of spaces.
            var proc = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = $"{Environment.CurrentDirectory}\\FBXConverter\\FBXConverter.exe",
                    Arguments = cmdArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };

            proc.Start();
            //proc.WaitForExit();
            //Console.WriteLine(proc.StandardOutput.ReadToEnd());
        }



        private static void PackTextures(int area, HashSet<TextureInfo> textureSet) {
            string[] textures = new string[textureSet.Count];
            int t = 0;
            foreach (TextureInfo textureInfo in textureSet) {
                textures[t++] = textureInfo.path;
            }

            int CHUNK_AMOUNT = textures.Length / 4; //get the amount of textures per BXF3.

            for (int i = 0; i < 4; i++) {

                BXF4 bxf = new();
                string[] chunk = textures.Skip(CHUNK_AMOUNT * i).Take(CHUNK_AMOUNT).ToArray(); //We skip the ones we already processed and move to the next set

                for (int j = 0; j < chunk.Length; j++) {
                    string file = chunk[j];
                    byte[] tpf = File.ReadAllBytes(file);
                    string name = Path.GetFileName(file);
                    bxf.Files.Add(new BinderFile(Binder.FileFlags.Flag1, j, name, tpf));
                }

                string texPath = OutputPath + "map\\m" + area + "\\" + "m" + area + "_" + i.ToString("D4");

                bxf.Write(texPath + ".tpfbhd", texPath + ".tpfbdt");
            }

            string[] excess = textures.Skip(CHUNK_AMOUNT * 4).Take(CHUNK_AMOUNT).ToArray();
            if (excess.Length > 0) {
                TPF tpf = new();
                foreach (string file in excess) {
                    TPF tpfF = TPF.Read(File.ReadAllBytes(file));
                    foreach (TPF.Texture texture in tpfF) {
                        tpf.Textures.Add(texture);
                    }
                }

                tpf.Write(OutputPath + "map\\m" + area + "\\" + "m" + area + "_9999.tpf.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            }
            //Directory.Delete(OutputPath + "map\\tx", true); // Don't delete temp tpf folder because if you delete this we can't re-use it. program is a lot faster without recreating every tpf every time
        }
    }

    public class MSBData {
        public int area, block;
        public MSB3 msb;
        public string debugName; // Name of the default spawn cell that will show up in the debug load list

        public MSBData(int area, int block, MSB3 msb, string debugName) {
            this.area = area;
            this.block = block;
            this.msb = msb;
            this.debugName = debugName.Replace(" ", "").Replace(",", "").Replace(":", "").Replace("'", "");
        }
    }

    public class NVAData {
        public int area, block;
        public NVA nva;
        public NVAData(int area, int block, NVA nva) {
            this.area = area;
            this.block = block;
            this.nva = nva;
        }
    }
}
