using System;
using System.Collections.Generic;
using System.Text;
using MBT = SoulsFormats.FLVER.LayoutType;
using MBS = SoulsFormats.FLVER.LayoutSemantic;
using Newtonsoft.Json.Linq;

using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Linq;

namespace PortJob {
    public class MTD {
        private static JArray MTD_INFO_LIST;

        public static SoulsFormats.FLVER2.BufferLayout getLayout(string MTDName, bool isStatic) {

            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JArray MTD_LAYOUT_MEMBERS = (JArray)MTD_INFO["LayoutMembers"];

            SoulsFormats.FLVER2.BufferLayout BL = new();
            for (int i = 0; i < MTD_LAYOUT_MEMBERS.Count; i++) {
                MBT mbt = (MBT)uint.Parse(MTD_LAYOUT_MEMBERS[i]["Type"].ToString());
                MBS mbs = (MBS)uint.Parse(MTD_LAYOUT_MEMBERS[i]["Semantic"].ToString());
                if (isStatic && mbs == MBS.BoneWeights) { continue; }
                BL.Add(new SoulsFormats.FLVER.LayoutMember(mbt, mbs));
            }

            return BL;
        }

        public static List<TextureKey> getTextureMap(string MTDName) {
            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JArray MTD_TEXTURE_MEMBERS = (JArray)MTD_INFO["TextureChannels"];
            List<TextureKey> TM = new();
            for (int i = 0; i < MTD_TEXTURE_MEMBERS.Count; i++) {
                string TexMem = MTD_TEXTURE_MEMBERS[i].ToString();
                switch (TexMem) {
                    case "g_Diffuse": TM.Add(new TextureKey("Texture", TexMem, 0x1, true)); break;
                    case "g_Diffuse_2": TM.Add(new TextureKey("SpecularFactor", TexMem, 0x1, true)); break;
                    case "g_Specular": TM.Add(new TextureKey("Specular", TexMem, 0x1, true)); break;
                    case "g_Specular_2": TM.Add(new TextureKey("SpecularPower", TexMem, 0x1, true)); break;
                    case "g_Bumpmap": TM.Add(new TextureKey("NormalMap", TexMem, 0x1, true)); break;
                    case "g_Bumpmap_2": TM.Add(new TextureKey("Reflection", TexMem, 0x1, true)); break;
                    case "g_Envmap": TM.Add(new TextureKey("Transparency", TexMem, 0x1, true)); break;
                    case "g_Lightmap": TM.Add(new TextureKey("Emissive", TexMem, 0x1, true)); break;
                    default: throw new Exception($"The texture member {TexMem} does not exist in current MTD info");
                }
            }
            return TM;
        }

        public static List<TextureKey> getHardcodedTextureMap() {
            List<TextureKey> TM = new();
            TM.Add(new TextureKey("g_DetailBumpmap", "", 0x0, false));
            return TM;
        }


        private static JObject getMTDInfo(string MTDName) {
            JToken[] mtd_info = MTD_INFO_LIST[0].ToArray();
            for (int i = 0; i < mtd_info.Length; i++) {
                if (mtd_info[i].First["MTD"].ToString().Equals(MTDName, StringComparison.InvariantCultureIgnoreCase)) {
                    return (JObject)mtd_info[i].First;
                }
            }
            /* This is bad times if this happens */
            throw new Exception("MTD Info not found");
        }

        private static void loadMTDInfoList() {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "PortJob.Resources.DS3_MTD_INFO.json";
            string data;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new(stream)) {
                data = reader.ReadToEnd();
            }

            JObject json = JObject.Parse(data);
            MTD_INFO_LIST = (JArray)json["mtds"];
        }
    }

    public class TextureKey {
        public string Key, Value;
        public byte Unk10;
        public bool Unk11;
        public TextureKey(string k, string v, byte u, bool uu) {
            Key = k; Value = v; Unk10 = u; Unk11 = uu;
        }
    }
}