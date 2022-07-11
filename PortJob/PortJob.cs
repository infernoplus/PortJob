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

namespace PortJob {
    class PortJob {
        public static string MorrowindPath { get; set; }
        public static string OutputPath { get; set; }
        public static readonly float GLOBAL_SCALE = 0.01f;
        static void Main(string[] args) {
            CheckIsDarkSouls3IsRunning();
            DateTime startTime = DateTime.Now;
            SetupPaths();
            Log.SetupLogStream();

            Convert();

            //FLVER2 myFlver = FLVER2.Read("C:\\Games\\steamapps\\common\\DARK SOULS III\\Game\\mod\\map\\m54_00_00_00\\m54_00_00_00_009000-mapbnd-dcx\\m54_00_00_00_009000.flver");
            //FLVER2 fromFlver = GetDonorFlver(Directory.GetFiles("C:\\Games\\steamapps\\common\\DARK SOULS III\\Game\\map\\m31_00_00_00", "*.mapbnd.dcx"));

            TimeSpan length = DateTime.Now - startTime;
            Log.Info(0, $"Porting time: {length}");
            Log.CloseWriter();

        }


        private static FLVER2 GetDonorFlver(string[] files) {
            foreach (string file in files) {
                BND4 bnd = BND4.Read(file);

                foreach (BinderFile binderFile in bnd.Files) {
                    if (binderFile.Name.ToLower().Contains("flver")) {
                        FLVER2 flver = FLVER2.Read(binderFile.Bytes);

                        foreach (FLVER2.Material mat in flver.Materials) {
                            Console.WriteLine(file);
                            if (mat.MTD.ToLower().EndsWith("m[arsn]_m.mtd"))
                                return flver;
                            //   Console.WriteLine($"Material found {binderFile.Name} {file} {mat.Name}");

                            //if (mat.MTD.ToLower().Contains("m[a]"))
                            //    Console.WriteLine($"Material found {binderFile.Name} {file} {mat.Name}");
                        }
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

        private static void WaitForWorkers() {
            while (_workers.Count > 0) {
                for (int i = _workers.Count - 1; i >= 0; i--) {
                    if (_workers[i].IsDone) {
                        if (_workers[i].ExitCode != 0) {
                            Console.WriteLine($"Worker exited with error code {_workers[i].ExitCode}");
                            Console.WriteLine(_workers[i].ErrorMessage);
                        }
                        _workers.RemoveAt(i);
                    }
                }
            }
        }

        private static List<Worker> _workers = new();

        private static void SetupPaths() {
            string jsonString = Utility.GetEmbededResource("PortJob.Resources.settings.json");
            JObject settings = JObject.Parse(jsonString);
            MorrowindPath = settings["morrowind"].ToString();
            OutputPath = settings["output"].ToString();
            if (!MorrowindPath.EndsWith("\\"))
                MorrowindPath += "\\";

            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";
        }

        private static void Convert() {
            /* Load ESM */
            ESM esm = new(MorrowindPath + "morrowind.json");

            /* Call Layout to calculate data we will use to create all exterior MSBs. */
            List<Layout> layouts = Layout.CalculateLayout(esm);

            /* Generate MSBs from layouts */
            const int area = 54;
            List<MSBData> msbs = new();
            List<NVAData> nvas = new();

            string tpfDir = OutputPath + "map\\tx\\";

            int i = 0;
            foreach (Layout layout in layouts) {
                /* Generate a new MSB and fill out required default data */
                int block = i++;
                MSB3 msb = new();

                if (block > 9 ) { continue; } //for rapid debugging 

                MSB3.Part.Player player = new(); // Player default spawn point
                MSB3.Model.Player playerRes = new();
                player.ModelName = "c0000";
                player.Position = layout.cells[0].center + new Vector3(0.0f, 5.1f, 0.0f);
                player.Name = "c0000_0000";
                playerRes.Name = player.ModelName;
                playerRes.SibPath = "N:\\FDP\\data\\Model\\chr\\c0000\\sib\\c0000.SIB";
                msb.Models.Players.Add(playerRes);
                msb.Parts.Players.Add(player);

                /* Write cells in this layout to the MSB */
                Dictionary<string, string> modelMap = new();
                Dictionary<string, int> partMap = new();

                /* This offset and rotation will be applies to all collision below this. Left for testing purposes */
                Vector3 OFFSET = new(0, 0, 0);
                Vector3 ROTATION = new(0, 0, 0);

                NVA nva = new(); //One nva per msb. I put this up here so you can easily add the navmeshes in the loop.  
                List<FBXInfo> fbxList = new();

                for (int c = 0; c < layout.cells.Count; c++) {
                    //if (c > 0) { break; } //DEBUG DEBUG @TODO DEBUG
                    Cell cell = layout.cells[c];
                    Log.Info(0, "Processing Cell: " + cell.region + "->" + cell.name + " [" + cell.position.x + ", " + cell.position.y + "]", "test");

                    /* Name and model name stuff */
                    string cModel = NewCollisionID();
                    //string cName;
                    if (partMap.ContainsKey(cModel)) {
                        throw new Exception("No duplicate non-connect col");
                    }

                    //cName = "_" + (partMap[cModel]++.ToString("D4"));
                    //cName = "_0000";
                    partMap.Add(cModel, 0);

                    /* Flat ground for testing */
                    MSB3.Part.Collision flat = new();
                    MSB3.Model.Collision flatRes = new();
                    flat.HitFilterID = 8;
                    flat.ModelName = cModel;
                    flat.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                    flat.Position = cell.center + OFFSET + new Vector3(0f, 5f, 0f);
                    flat.Rotation = ROTATION;
                    flat.MapStudioLayer = uint.MaxValue;
                    for (int k = 0; k < cell.drawGroups.Length; k++) {
                        flat.DrawGroups[k] = cell.drawGroups[k];
                        flat.DispGroups[k] = cell.drawGroups[k];
                        flat.BackreadGroups[k] = cell.drawGroups[k];
                    }

                    flat.Name = cModel;// + cName;
                    flat.LodParamID = -1;
                    flat.UnkE0E = -1;

                    flatRes.Name = flat.ModelName;
                    flatRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{cModel}.hkt";

                    WriteTestCollision(cModel, area, block);
                    AddResource(msb, flatRes);
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
                        MSB3.Model.Collision conRes = new();

                        con.CollisionName = flat.Name;
                        con.MapID[0] = (byte)area;
                        con.MapID[1] = (byte)(cell.connects[k].id);
                        con.MapID[2] = 0;
                        con.MapID[3] = 0;
                        con.Name = ccModel + ccName;
                        //con.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB"; // Looks like connnect collision does not ever use sibs
                        con.ModelName = ccModel;
                        con.Position = cell.center + OFFSET + new Vector3(0f, 5f, 0f); ;
                        con.Rotation = ROTATION;
                        con.MapStudioLayer = 4294967295;                          // Not a clue what this does... Should probably ask about it
                        for (int l = 0; l < cell.drawGroups.Length; l++) {
                            con.DrawGroups[l] = cell.drawGroups[l];
                            con.DispGroups[l] = cell.drawGroups[l];
                            con.BackreadGroups[l] = 0;                            // Seems like DS3 doesn't use this for collision at all
                        }
                        con.LodParamID = -1;
                        con.UnkE0E = -1;

                        conRes.Name = con.ModelName;
                        conRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{ccModel}.hkt";

                        AddResource(msb, conRes);
                        msb.Parts.ConnectCollisions.Add(con);
                    }

                    /* Generate cell terrain map piece */
                    if (cell.terrain != null) {
                        string terrainModel = (9000 + c).ToString("D6");
                        string terrainName = "_0000";

                        TerrainConverter.convert(cell, $"{OutputPath}map\\m{area:D2}_{block:D2}_00_00\\m{area:D2}_{block:D2}_00_00_{terrainModel}.flver", tpfDir);

                        MSB3.Part.MapPiece terrain = new();
                        MSB3.Model.MapPiece terrainRes = new();
                        terrain.ModelName = "m" + terrainModel;
                        terrain.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{terrainModel}.SIB";
                        terrain.Position = cell.center;
                        terrain.Rotation = new Vector3(0, 0, 0);
                        terrain.MapStudioLayer = uint.MaxValue;
                        for (int k = 0; k < cell.drawGroups.Length; k++) {
                            terrain.DrawGroups[k] = cell.drawGroups[k];
                            terrain.DispGroups[k] = cell.drawGroups[k];
                            terrain.BackreadGroups[k] = 0;
                        }
                        terrain.ShadowSource = true;
                        terrain.DrawByReflectCam = true;
                        terrain.Name = terrain.ModelName + terrainName;
                        terrain.UnkE0E = -1;
                        terrain.LodParamID = 19; //Param for: Don't switch to LOD models 
                        terrainRes.Name = terrain.ModelName;
                        terrainRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\{terrainModel}.sib";

                        AddResource(msb, terrainRes);
                        msb.Parts.MapPieces.Add(terrain);
                    }

                    /* Enemy for testing */
                    string eModel = "c1100";
                    string eName = $"_{c:D4}";

                    MSB3.Part.Enemy enemy = new();
                    MSB3.Model.Enemy enemyRes = new();

                    enemy.CollisionName = flat.Name;
                    enemy.ThinkParamID = 110050;
                    enemy.NPCParamID = 110010;
                    enemy.TalkID = 0;
                    enemy.CharaInitID = -1;
                    enemy.UnkT78 = 128;
                    enemy.UnkT84 = 1;
                    enemy.Name = eModel + eName;
                    enemy.SibPath = "";
                    enemy.ModelName = eModel;
                    enemy.Position = cell.center + new Vector3(0f, 5f, 0f); ;
                    enemy.MapStudioLayer = 4294967295;

                    for (int k = 0; k < cell.drawGroups.Length; k++) {
                        enemy.DrawGroups[k] = 0;
                        enemy.DispGroups[k] = 0;
                        enemy.BackreadGroups[k] = 0;
                    }

                    enemy.LodParamID = -1;
                    enemy.UnkE0E = -1;

                    enemyRes.Name = enemy.ModelName;
                    enemyRes.SibPath = "";

                    AddResource(msb, enemyRes);
                    msb.Parts.Enemies.Add(enemy);

                    /* Process content */
                    ESM.Type[] VALID_MAP_PIECE_TYPES = { ESM.Type.Static, ESM.Type.Door, ESM.Type.Container };


                    foreach (Content content in cell.content) {
                        if (!VALID_MAP_PIECE_TYPES.Contains(content.type)) { continue; }   // Only process valid world meshes
                        if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes

                        /* Name and model name stuff */
                        string mpModel;
                        if (modelMap.ContainsKey(content.mesh)) {
                            mpModel = modelMap[content.mesh];
                        } else {
                            mpModel = NewMapPieceID();
                            string fbxPath = MorrowindPath + "Data Files\\meshes\\" + content.mesh.Substring(0, content.mesh.Length - 3) + "fbx";
                            string flverPath = $"{OutputPath}map\\m{area:D2}_{block:D2}_00_00\\m{area:D2}_{block:D2}_00_00_{mpModel}.flver";
                            if (!File.Exists(flverPath.Replace("flver", "mapbnd.dcx"))) fbxList.Add(new FBXInfo(fbxPath, flverPath, tpfDir));

                            modelMap.Add(content.mesh, mpModel);
                        }

                        string mpName;
                        if (partMap.ContainsKey(mpModel)) {
                            mpName = "_" + (partMap[mpModel]++.ToString("D4"));
                        } else {
                            mpName = "_0000";
                            partMap.Add(mpModel, 1);
                        }

                        /* Create map piece */
                        MSB3.Part.MapPiece mp = new();
                        MSB3.Model.MapPiece mpRes = new();
                        mp.ModelName = "m" + mpModel;
                        mp.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{Utility.DeleteFromEnd(int.Parse(mpName.Split("_")[1]), 2).ToString("D2")}.SIB"; //put the right number here
                        mp.Position = content.position;
                        mp.Rotation = content.rotation;
                        mp.MapStudioLayer = uint.MaxValue;
                        for (int k = 0; k < cell.drawGroups.Length; k++) {
                            mp.DrawGroups[k] = cell.drawGroups[k];
                            mp.DispGroups[k] = cell.drawGroups[k];
                            mp.BackreadGroups[k] = 0;
                        }
                        mp.ShadowSource = true;
                        mp.DrawByReflectCam = true;
                        mp.Name = "m" + mpModel + mpName;
                        mpRes.Name = mp.ModelName;
                        mp.UnkE0E = -1;
                        mp.LodParamID = 19; //Param for: Don't switch to LOD models 
                        mpRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\{mpModel}.sib";

                        AddResource(msb, mpRes);
                        msb.Parts.MapPieces.Add(mp);
                    }
                }

                _workers.Add(new FBXConverterWorker(OutputPath, MorrowindPath, GLOBAL_SCALE, fbxList));

                /* Just add one Navmesh to each nva. Model and Name are not a string, so no '_0000' format, and we have to use a unique ID here. */
                int nModelID = 0;
                if (block == 8)
                    nModelID = 91;

                if (int.TryParse($"{area}{block}{nModelID:D6}", out int id)) //This is just for testing so we don't go over int.MaxValue.
                {
                    nva.Navmeshes.Add(new NVA.Navmesh() {
                        NameID = id,
                        ModelID = nModelID,
                        Position = block == 3 ? new Vector3(798, 3, -185) : new Vector3(),//new Vector3(716, 2, -514),// player.Position, //using player position, here. Change this to cell.center in loop.
                        VertexCount = 1,
                        //Unk38 = 12399,
                        //Unk4C = true
                    });
                }

                WriteTestNavMesh(nModelID, area, block);

                /* There has to be an entry for each vertex in each navmesh in nav.Navmashes */
                foreach (NVA.Navmesh navmesh in nva.Navmeshes) {
                    for (int j = 0; j < navmesh.VertexCount; j++) {
                        nva.Entries1.Add(new NVA.Entry1());
                    }
                }

                Log.Info(0, $"MSB: m{area:D2}_{block:D2}_00_00");
                Log.Info(1, "MapPieces: " + msb.Parts.MapPieces.Count);
                Log.Info(1, "Collisions: " + msb.Parts.Collisions.Count);

                msbs.Add(new MSBData(area, block, msb));
                nvas.Add(new NVAData(area, block, nva));
            }

            /* Write msbs to file and build packages */
            foreach (MSBData msb in msbs) {
                string msbPath = $"{OutputPath}map\\MapStudio\\m{msb.area:D2}_{msb.block:D2}_00_00.msb.dcx";
                Log.Info(0, "Writing MSB to: " + msbPath);
                msb.msb.Write(msbPath, DCX.Type.DCX_DFLT_10000_44_9);

                //Utility.PackTestCol(msb.area, msb.block);
            }

            foreach (NVAData nva in nvas) {
                string nvaPath = $"{OutputPath}map\\m{nva.area:D2}_{nva.block:D2}_00_00\\m{nva.area:D2}_{nva.block:D2}_00_00.nva.dcx";
                Log.Info(0, "Writing MSB to: " + nvaPath);
                nva.nva.Write(nvaPath, DCX.Type.DCX_DFLT_10000_44_9);

                Utility.PackTestCol(nva.area, nva.block);
            }

            WaitForWorkers();
            PackTextures(area); // This should be run once per area, currently needs to be reworked to support some like area division stuff but not important right now

            /* Generate and write loadlists */
            string mapViewListPath = $"{OutputPath}map\\mapviewlist.loadlistlist";
            string worldMsbListPath = $"{OutputPath}map\\worldmsblist.worldloadlistlist";
            string mapViewList = "", worldMsbList = "";
            foreach (MSBData msb in msbs) {

                mapViewList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb #m{msb.area:D2}B0【Layout {msb.block}】\r\n";
                worldMsbList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb	#m{msb.area:D2}B1yI‚Ì‰¤é_1z 0\r\n";
            }
            mapViewList += "\0\0\0\0\0\0\0\0\0\0\0\0"; // Buncha fucking \0 idk why
            worldMsbList += "\0\0\0\0\0\0\0\0\0\0\0\0";

            if (File.Exists(mapViewListPath)) { File.Delete(mapViewListPath); }
            if (File.Exists(worldMsbListPath)) { File.Delete(worldMsbListPath); }

            File.WriteAllText(mapViewListPath, mapViewList);
            File.WriteAllText(worldMsbListPath, worldMsbList);
        }

        private static void WriteTestCollision(string cModel, int area, int block) {
            /* Setup area_block and output path*/
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";

            /* Write the high col bhd/bdt pair */
            if (!int.TryParse(cModel.Substring(1), out int hModelId))
                throw new Exception($"Could not parse cModel ID: {cModel}");

            string hPreGenPath = $"PortJob.TestCol.h30_00_00_00_{0:D6}.hkx";
            string hPath = $"{mapName}-h{area_block}_00_00";
            byte[] hBytes = Utility.GetEmbededResourceBytes(hPreGenPath);
            hBytes = DCX.Compress(hBytes, DCX.Type.DCX_DFLT_10000_44_9); //File is compressed inside the bdt.
            Directory.CreateDirectory($"{OutputPath}\\map\\{mapName}\\hkx\\col\\");
            File.WriteAllBytes($"{OutputPath}\\map\\{mapName}\\hkx\\col\\{hPath}_{hModelId:D6}.hkx.dcx", hBytes);
        }
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



        private static void PackTextures(int area) {
            string[] textures = Directory.GetFiles(OutputPath + "map\\tx\\", "*.tpf.dcx");

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

        /* Function add collision model resources, avoids adding duplicates */
        private static void AddResource(MSB3 msb, MSB3.Model.Collision res) {
            foreach (MSB3.Model.Collision collision in msb.Models.Collisions) {
                if (collision.Name == res.Name) {
                    return;
                }
            }
            msb.Models.Collisions.Add(res);
        }

        /* Function top add map piece model resources, avoids adding duplicates */
        private static void AddResource(MSB3 msb, MSB3.Model.MapPiece res) {
            foreach (MSB3.Model.MapPiece map in msb.Models.MapPieces) {
                if (map.Name == res.Name) {
                    return;
                }
            }
            msb.Models.MapPieces.Add(res);
        }

        /* Function top add enemy model resources, avoids adding duplicates */
        private static void AddResource(MSB3 msb, MSB3.Model.Enemy res) {
            foreach (MSB3.Model.Enemy enemy in msb.Models.Enemies) {
                if (enemy.Name == res.Name) {
                    return;
                }
            }
            msb.Models.Enemies.Add(res);
        }

        private static int nextCollisionID = 0;
        private static string NewCollisionID() {
            return "h" + nextCollisionID++.ToString("D6");
        }

        private static int nextMapPieceID = 0;
        private static string NewMapPieceID() {
            return nextMapPieceID++.ToString("D6");
        }

        private static int nextEventID = 1;
        private static int NewEventID() {
            return nextEventID++;
        }
    }

    public class MSBData {
        public int area, block;
        public MSB3 msb;

        public MSBData(int area, int block, MSB3 msb) {
            this.area = area;
            this.block = block;
            this.msb = msb;
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
