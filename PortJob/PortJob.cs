using System;
using System.IO;
using System.Text;
using System.Numerics;

using SoulsFormats;
using System.Collections.Generic;

namespace PortJob
{
    class PortJob
    {
        static void Main(string[] args)
        {

        }

        private void convert()
        {
            /* Load ESM */
            ESM esm = new ESM("D:\\Steam\\steamapps\\common\\Morrowind\\morrowind.json");

            /* Generate a new MSB and fill out required default data */
            MSB1 msb = new MSB1();

            MSB1.Part.Player player = new(); // Player default spawn point
            MSB1.Model.Player playerRes = new();
            player.ModelName = "c0000";
            player.Position = new Vector3(0.0f, 0.1f, 0.0f);
            player.Name = "c0000_0000";
            playerRes.Name = player.ModelName;
            playerRes.SibPath = "N:\\FRPG\\data\\Model\\chr\\c0000\\sib\\c0000.SIB";
            msb.Models.Players.Add(playerRes);
            msb.Parts.Players.Add(player);

            MSB1.Part.Collision flat = new(); // Flat ground for testing
            MSB1.Model.Collision flatRes = new();
            flat.HitFilterID = 8;
            flat.ModelName = "h0000B0";
            flat.SibPath = "N:\\FRPG\\data\\Model\\map\\m11_00_00_00\\layout\\h_layout.SIB";
            flat.Position = new Vector3();
            flat.DrawGroups[0] = 1;
            flat.DrawGroups[1] = 0;
            flat.DrawGroups[2] = 0;
            flat.DrawGroups[3] = 0;
            flat.DispGroups[0] = 1;
            flat.DispGroups[1] = 0;
            flat.DispGroups[2] = 0;
            flat.DispGroups[3] = 0;
            flat.NvmGroups[0] = 0;
            flat.NvmGroups[1] = 0;
            flat.NvmGroups[2] = 0;
            flat.NvmGroups[3] = 0;
            flat.Name = "h0000B0_0000";
            flatRes.Name = flat.ModelName;
            flatRes.SibPath = "N:\\FRPG\\data\\Model\\map\\m10_00_00_00\\hkxwin\\h0000B0.hkxwin";
            msb.Models.Collisions.Add(flatRes);
            msb.Parts.Collisions.Add(flat);

            MSB1.Region envRegion = new(); // Environment region and event
            envRegion.Name = "GI00";
            envRegion.Shape = new MSB.Shape.Point();
            envRegion.Position = new Vector3(0.0f, 1.0f, 0.0f);
            MSB1.Event.Environment env = new();
            env.UnkT00 = 0;
            env.UnkT04 = 50f;
            env.UnkT08 = 300f;
            env.UnkT0C = 200f;
            env.UnkT10 = 100f;
            env.UnkT14 = 50f;
            env.EventID = NewEventID();
            env.PartName = flat.Name;
            env.RegionName = envRegion.Name;
            env.Name = "GI00";
            msb.Regions.Add(envRegion);
            msb.Events.Environments.Add(env);

            /* Write a cell to the MSB */
            Dictionary<string, string> modelMap = new();
            Dictionary<string, int> partMap = new();

            int area = 11;
            int block = 0;

            Cell cell = esm.GetCell(-2, -9);

            foreach (Content content in cell.content)
            {
                string fbxPath = "D:\\Steam\\steamapps\\common\\Morrowind\\Data Files\\meshes\\" + content.mesh.Substring(0, content.mesh.Length-3) + "fbx";
                FBXConverter.convert(fbxPath);

                string mpModel;
                if (modelMap.ContainsKey(content.mesh)) {
                    mpModel = modelMap[content.mesh];
                }
                else
                {
                    mpModel = NewMapPieceID();
                    modelMap.Add(content.mesh, mpModel);
                }

                string mpName;
                if (partMap.ContainsKey(mpModel))
                {
                    mpName = "_" + (partMap[mpModel]++.ToString("D4"));
                }
                else
                {
                    mpName = "_0000";
                    partMap.Add(mpModel, 0);
                }

                MSB1.Part.MapPiece mp = new();
                MSB1.Model.MapPiece mpRes = new();
                mp.ModelName = mpModel;
                mp.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\layout\\layout.SIB";
                mp.Position = content.position;
                mp.Rotation = content.rotation;
                mp.DrawGroups[0] = 1;
                mp.DrawGroups[1] = 0;
                mp.DrawGroups[2] = 0;
                mp.DrawGroups[3] = 0;
                mp.DispGroups[0] = 1;
                mp.DispGroups[1] = 0;
                mp.DispGroups[2] = 0;
                mp.DispGroups[3] = 0;
                mp.IsShadowDest = 0x1;
                mp.DrawByReflectCam = 0x1;
                mp.Name = mpModel + mpName;
                mpRes.Name = mp.ModelName;
                mpRes.SibPath = "N:\\FRPG\\data\\Model\\map\\m" + area + "_0" + block + "_00_00\\sib\\" + mpModel + "B" + block + ".sib";
                msb.Models.MapPieces.Add(mpRes);
                msb.Parts.MapPieces.Add(mp);
            }

            /* Write to file */
            msb.Write("F:\\m11_00_00_00.msb");
        }

        private static int nextMapPieceID = 0;
        private static string NewMapPieceID()
        {
            return "m" + nextMapPieceID++.ToString("D4") + "B0";
        }

        private static int nextEventID = 1;
        private static int NewEventID()
        {
            return nextEventID++;
        }
    }
}
