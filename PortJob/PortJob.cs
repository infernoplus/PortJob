using Microsoft.Xna.Framework.Graphics;
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
        public static string MorrowindPath = "G:\\Steam\\steamapps\\common\\Morrowind\\";
        public static string OutputPath = "G:\\test\\";
        static void Main(string[] args) {
            
            Convert();
            Utility.PackTestCol(OutputPath);
        }

        private static void Convert() {
            /* Load ESM */
            ESM esm = new(MorrowindPath + "morrowind.json");

            /* Generate a new MSB and fill out required default data */
            MSB3 msb = new();

            MSB3.Part.Player player = new(); // Player default spawn point
            MSB3.Model.Player playerRes = new();
            player.ModelName = "c0000";
            player.Position = new Vector3(0.0f, 0.1f, 0.0f);
            player.Name = "c0000_0000";
            playerRes.Name = player.ModelName;
            playerRes.SibPath = "N:\\FRPG\\data\\Model\\chr\\c0000\\sib\\c0000.SIB";
            msb.Models.Players.Add(playerRes);
            msb.Parts.Players.Add(player);

            /* Write a cell to the MSB */
            Dictionary<string, string> modelMap = new();
            Dictionary<string, int> partMap = new();

            const int area = 30;
            const int block = 0;

            //I think this got moved to the bottom.  
            //int nextEnv = 0;
            //int NewEnvID() {
            //    return nextEnv++;
            //}

            int CELLS = 3;

            /* Precalculate draw group information for each cell */
            Dictionary<string, uint> drawGroupGrid = new();
            int c = 0;
            for (int gx = -CELLS; gx <= CELLS; gx++) {
                for (int gy = -CELLS; gy <= CELLS; gy++) {
                    uint drawGroup = 0;
                    drawGroup |= (uint)(1) << c++;

                    drawGroupGrid.Add(gx + "," + gy, drawGroup);
                }
            }

            //int c = 0; // Cell count for this MSB
            for (int gx = -CELLS; gx <= CELLS; gx++) {
                for (int gy = -CELLS; gy <= CELLS; gy++) {
                    Log.Info(0, "Processing Cell [" + gx + ", " + gy + "]");
                    Cell cell = esm.GetCell(gx, gy);

                    /* Set drawgroup for this cell and adjacent cells */
                    uint drawGroup = drawGroupGrid[gx + "," + gy];
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx + 1) + "," + (gy));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx - 1) + "," + (gy));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx) + "," + (gy + 1));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx) + "," + (gy - 1));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx + 1) + "," + (gy + 1));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx - 1) + "," + (gy - 1));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx - 1) + "," + (gy + 1));
                    drawGroup += drawGroupGrid.GetValueOrDefault((gx + 1) + "," + (gy - 1));

                    string cModel = "h000000";
                    string cName;
                    if (partMap.ContainsKey(cModel)) {
                        cName = "_" + (partMap[cModel]++.ToString("D4"));
                    } else {
                        cName = "_0000";
                        partMap.Add(cModel, 1);
                    }

                    MSB3.Part.Collision flat = new(); // Flat ground for testing
                    MSB3.Model.Collision flatRes = new();
                    flat.HitFilterID = 8;
                    flat.ModelName = cModel;
                    flat.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\layout\\h_layout.SIB";
                    flat.Position = cell.center;
                    flat.MapStudioLayer = uint.MaxValue;
                    flat.DrawGroups[0] = drawGroup;
                    flat.DrawGroups[1] = 0;
                    flat.DrawGroups[2] = 0;
                    flat.DrawGroups[3] = 0;
                    flat.DrawGroups[4] = 0;
                    flat.DrawGroups[5] = 0;
                    flat.DrawGroups[6] = 0;
                    flat.DrawGroups[7] = 0;
                    flat.DispGroups[0] = drawGroup;
                    flat.DispGroups[1] = 0;
                    flat.DispGroups[2] = 0;
                    flat.DispGroups[3] = 0;
                    flat.DispGroups[3] = 0;
                    flat.DispGroups[4] = 0;
                    flat.DispGroups[5] = 0;
                    flat.DispGroups[6] = 0;
                    flat.DispGroups[7] = 0;
                    flat.BackreadGroups[0] = drawGroup;
                    flat.BackreadGroups[1] = 0;
                    flat.BackreadGroups[2] = 0;
                    flat.BackreadGroups[3] = 0;
                    flat.BackreadGroups[3] = 0;
                    flat.BackreadGroups[4] = 0;
                    flat.BackreadGroups[5] = 0;
                    flat.BackreadGroups[6] = 0;
                    flat.BackreadGroups[7] = 0;
                    //flat.NvmGroups[0] = 0;
                    //flat.NvmGroups[1] = 0;
                    //flat.NvmGroups[2] = 0;
                    //flat.NvmGroups[3] = 0;
                    flat.Name = cModel + cName;
                    flatRes.Name = flat.ModelName;
                    flatRes.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\hkxwin\\" + cModel + ".hkxwin";
                    msb.Models.Collisions.Add(flatRes);
                    msb.Parts.Collisions.Add(flat);

                    //MSB3.Region envRegion = new(); // Environment region and event
                    //envRegion.Name = "GI" + NewEnvID().ToString("D2");
                    //envRegion.Shape = new MSB.Shape.Point();
                    //envRegion.Position = cell.center + new Vector3(0.0f, 5.0f, 0.0f);
                    //MSB1.Event.Environment env = new();
                    //env.UnkT00 = 0;
                    //env.UnkT04 = 50f;
                    //env.UnkT08 = 300f;
                    //env.UnkT0C = 200f;
                    //env.UnkT10 = 100f;
                    //env.UnkT14 = 50f;
                    //env.EventID = NewEventID();
                    //env.PartName = flat.Name;
                    //env.RegionName = envRegion.Name;
                    //env.Name = "evt " + envRegion.Name;
                    //msb.Regions.Add(envRegion);
                    //msb.Events.Environments.Add(env);

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
                            string flverPath = OutputPath + "map\\m" + area + "_0" + block + "_00_00\\m" + area + "_0" + block + "_00_00_" + mpModel + ".flver";
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
                        mp.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\layout\\layout.SIB";
                        mp.Position = content.position;
                        mp.Rotation = content.rotation;
                        mp.MapStudioLayer = uint.MaxValue;
                        mp.DrawGroups[0] = drawGroup;
                        mp.DrawGroups[1] = 0;
                        mp.DrawGroups[2] = 0;
                        mp.DrawGroups[3] = 0;
                        mp.DrawGroups[4] = 0;
                        mp.DrawGroups[5] = 0;
                        mp.DrawGroups[6] = 0;
                        mp.DrawGroups[7] = 0;
                        mp.DispGroups[0] = drawGroup;
                        mp.DispGroups[1] = 0;
                        mp.DispGroups[2] = 0;
                        mp.DispGroups[3] = 0;
                        mp.DispGroups[4] = 0;
                        mp.DispGroups[5] = 0;
                        mp.DispGroups[6] = 0;
                        mp.DispGroups[7] = 0;
                        mp.BackreadGroups[0] = drawGroup;
                        mp.BackreadGroups[1] = 0;
                        mp.BackreadGroups[2] = 0;
                        mp.BackreadGroups[3] = 0;
                        mp.BackreadGroups[3] = 0;
                        mp.BackreadGroups[4] = 0;
                        mp.BackreadGroups[5] = 0;
                        mp.BackreadGroups[6] = 0;
                        mp.BackreadGroups[7] = 0;
                        //mp.IsShadowDest = 0x1;
                        mp.DrawByReflectCam = true;
                        mp.Name = "m" + mpModel + mpName;
                        mpRes.Name = mp.ModelName;
                        mp.UnkE0E = -1;
                        mp.LodParamID = 19;
                        mpRes.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\sib\\" + mpModel + ".sib";
                        msb.Models.MapPieces.Add(mpRes);
                        msb.Parts.MapPieces.Add(mp);
                    }
                    c++;
                }
            }

            Log.Info(0, "Generated MSB:");
            Log.Info(1, "MapPieces: " + msb.Parts.MapPieces.Count);
            Log.Info(1, "Collisions: " + msb.Parts.Collisions.Count);
            //Log.Info(1, "Regions: " + msb.Regions.Regions.Count);


            /* Write to file */
            string msbPath = OutputPath + "map\\MapStudio\\m" + area + "_0" + block + "_00_00.msb.dcx";
            Log.Info(0, "Writing MSB to: " + msbPath);
            msb.Write(msbPath, DCX.Type.DCX_DFLT_10000_44_9);

            PackTextures(area);
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

            Directory.Delete(OutputPath + "map\\tx", true);
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
}
