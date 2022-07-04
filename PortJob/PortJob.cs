using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Linq;

using SoulsFormats;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PortJob {
    class PortJob {
        public static string MorrowindPath { get; set; }
        public static string OutputPath { get; set; }
        static void Main(string[] args) {
            DateTime startTime = DateTime.Now;
            SetupPaths();
            Log.SetupLogStream();
            Convert();
            TimeSpan length = DateTime.Now - startTime;
            Log.Info(0,$"Porting time: {length}");
            Log.CloseWriter();
        }

        private static void SetupPaths()
        {
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

            int i = 0;
            foreach (Layout layout in layouts) {
                /* Generate a new MSB and fill out required default data */
                int block = i++;
                MSB3 msb = new();

                MSB3.Part.Player player = new(); // Player default spawn point
                MSB3.Model.Player playerRes = new();
                player.ModelName = "c0000";
                player.Position = layout.cells[0].center + new Vector3(0.0f, 0.1f, 0.0f);
                player.Name = "c0000_0000";
                playerRes.Name = player.ModelName;
                playerRes.SibPath = "N:\\FDP\\data\\Model\\chr\\c0000\\sib\\c0000.SIB";
                msb.Models.Players.Add(playerRes);
                msb.Parts.Players.Add(player);

                /* Write cells in this layout to the MSB */
                Dictionary<string, string> modelMap = new();
                Dictionary<string, int> partMap = new();

                foreach (Cell cell in layout.cells) {
                    Log.Info(0, "Processing Cell: " + cell.region + "->" + cell.name + " [" + cell.position.x + ", " + cell.position.y + "]", "test");

                    /* Name and model name stuff */
                    string cModel = "h000000";
                    string cName;
                    if (partMap.ContainsKey(cModel)) {
                        cName = "_" + (partMap[cModel]++.ToString("D4"));
                    } else {
                        cName = "_0000";
                        partMap.Add(cModel, 1);
                    }

                    /* Flat ground for testing */
                    MSB3.Part.Collision flat = new();
                    MSB3.Model.Collision flatRes = new();
                    flat.HitFilterID = 8;
                    flat.ModelName = cModel;
                    flat.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                    flat.Position = cell.center;
                    flat.MapStudioLayer = uint.MaxValue;
                    for (int k = 0; k < cell.drawGroups.Length; k++) {
                        flat.DrawGroups[k] = cell.drawGroups[k];
                        flat.DispGroups[k] = cell.drawGroups[k];
                        flat.BackreadGroups[k] = cell.drawGroups[k];
                    }
                    flat.Name = cModel + cName;
                    flatRes.Name = flat.ModelName;
                    flatRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{cModel}.hkt";
                    msb.Models.Collisions.Add(flatRes);
                    msb.Parts.Collisions.Add(flat);

                    /* Flat connect collision for testing */
                    for (int k = 0; k < cell.connects.Count; k++) {
                        string ccModel = "h000000";
                        string ccName;
                        if (partMap.ContainsKey(ccModel)) {
                            ccName = "_" + (partMap[ccModel]++.ToString("D4"));
                        } else {
                            ccName = "_0000";
                            partMap.Add(ccModel, 1);
                        }

                        MSB3.Part.ConnectCollision con = new();
                        MSB3.Model.Collision conRes = new();

                        con.CollisionName = ccModel;
                        con.MapID[0] = area;
                        con.MapID[1] = (byte)(cell.connects[k].id);
                        con.MapID[2] = 0;
                        con.MapID[0] = 3;
                        con.Name = ccName;
                        con.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                        con.ModelName = ccModel;
                        con.Position = cell.center;
                        for (int l = 0; l < cell.drawGroups.Length; l++) {
                            flat.DrawGroups[l] = cell.drawGroups[l];
                            flat.DispGroups[l] = cell.drawGroups[l];
                            flat.BackreadGroups[l] = cell.drawGroups[l];
                        }

                        conRes.Name = con.ModelName;
                        conRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{ccModel}.hkt";

                        msb.Models.Collisions.Add(conRes);
                        msb.Parts.ConnectCollisions.Add(con);
                    }

                    /* Process content */
                    ESM.Type[] VALID_MAP_PIECE_TYPES = { ESM.Type.Static, ESM.Type.Door, ESM.Type.Container };

                    foreach (Content content in cell.content) {
                        if (!VALID_MAP_PIECE_TYPES.Contains(content.type)) { continue; }   // Only process valid world meshes
                        if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes

                        string mpModel;
                        if (modelMap.ContainsKey(content.mesh)) {
                            mpModel = modelMap[content.mesh];
                        } else {
                            mpModel = NewMapPieceID();
                            string fbxPath = MorrowindPath + "Data Files\\meshes\\" + content.mesh.Substring(0, content.mesh.Length - 3) + "fbx";
                            string flverPath = $"{OutputPath}map\\m{area:D2}_{block:D2}_00_00\\m{area:D2}_{block:D2}_00_00_{mpModel}.flver";
                            string tpfDir = OutputPath + "map\\tx\\";
                            FBXConverter.convert(fbxPath, flverPath, tpfDir);

                            modelMap.Add(content.mesh, mpModel);
                        }

                        string mpName;
                        if (partMap.ContainsKey(mpModel)) {
                            mpName = "_" + (partMap[mpModel]++.ToString("D4"));
                        } else {
                            mpName = "_0000";
                            partMap.Add(mpModel, 1);
                        }

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
                            mp.BackreadGroups[k] = cell.drawGroups[k];
                        }
                        mp.ShadowSource = true;
                        mp.DrawByReflectCam = true;
                        mp.Name = "m" + mpModel + mpName;
                        mpRes.Name = mp.ModelName;
                        mp.UnkE0E = -1;
                        mp.LodParamID = 19; //Param for: Don't switch to LOD models 
                        mpRes.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\{mpModel}.sib";
                        msb.Models.MapPieces.Add(mpRes);
                        msb.Parts.MapPieces.Add(mp);
                    }
                }

                Log.Info(0, $"MSB: m{area:D2}_{block:D2}_00_00");
                Log.Info(1, "MapPieces: " + msb.Parts.MapPieces.Count);
                Log.Info(1, "Collisions: " + msb.Parts.Collisions.Count);

                msbs.Add(new MSBData(area, block, msb));
            }

            /* Write msbs to file and build packages */
            foreach (MSBData msb in msbs) {
                string msbPath = $"{OutputPath}map\\MapStudio\\m{msb.area:D2}_{msb.block:D2}_00_00.msb.dcx";
                Log.Info(0, "Writing MSB to: " + msbPath);
                msb.msb.Write(msbPath, DCX.Type.DCX_DFLT_10000_44_9);

                Utility.PackTestCol(OutputPath, msb.area, msb.block);
            }

            PackTextures(area); // This should be run once per area, currently needs to be reworked to support some like area division stuff but not important right now

            /* Generate and write loadlists */
            string mapViewListPath = $"{OutputPath}map\\mapviewlist.loadlistlist";
            string worldMsbListPath = $"{OutputPath}map\\worldmsblist.worldloadlistlist";
            string mapViewList = "", worldMsbList = "";
            foreach(MSBData msb in msbs) {
                mapViewList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb #m{msb.area:D2}B0【Layout {msb.block}】\r\n";
                worldMsbList += $"map:/MapStudio/m{msb.area:D2}_{msb.block:D2}_00_00.msb	#m{msb.area:D2}B1yI‚Ì‰¤é_1z 0\r\n";
            }
            mapViewList += "\0\0\0\0\0\0\0\0\0\0\0\0"; // Buncha fucking \0 idk why
            worldMsbList += "\0\0\0\0\0\0\0\0\0\0\0\0";

            if(File.Exists(mapViewListPath)) { File.Delete(mapViewListPath); }
            if(File.Exists(worldMsbListPath)) { File.Delete(worldMsbListPath); }

            File.WriteAllText(mapViewListPath, mapViewList);
            File.WriteAllText(worldMsbListPath, worldMsbList);
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

            //foreach (string texPath in textures) {
            //    File.Delete(texPath);
            //}
            //Directory.Delete(OutputPath + "map\\tx", true); // Don't delete temp tpf folder because if you delete this we can't re-use it. program is a lot faster without recreating every tpf every time
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
}
