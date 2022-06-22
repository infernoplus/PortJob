﻿using System;
using System.Collections.Generic;
using System.Text;
using MBT = SoulsFormats.FLVER.LayoutType;
using MBS = SoulsFormats.FLVER.LayoutSemantic;
using Newtonsoft.Json.Linq;
using SoulsFormats;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Linq;

namespace PortJob {
    public class MTD {
        private static JArray MTD_INFO_LIST;
        private static JObject GX_INFO_LIST;

        public static FLVER2.BufferLayout getLayout(string MTDName, bool isStatic) {

            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JToken[] MTD_LAYOUT_MEMBERS = MTD_INFO["AcceptableVertexBufferDeclarations"].ToArray();
            SoulsFormats.FLVER2.BufferLayout BL = new();
            JArray buffers = (JArray)MTD_LAYOUT_MEMBERS.First()["Buffers"].First;
            for (int i = 0; i < buffers.Count; i++) {
                MBT mbt = (MBT)uint.Parse(buffers[i]["Type"].ToString());
                MBS mbs = (MBS)uint.Parse(buffers[i]["Semantic"].ToString());
                if (isStatic && mbs == MBS.BoneWeights) { continue; }
                BL.Add(new FLVER.LayoutMember(mbt, mbs));
            }

            return BL;
        }

        public static List<FLVER2.BufferLayout> getAllLayouts(string MTDName, bool isStatic) {
            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JArray MTD_LAYOUT_MEMBERS = (JArray)MTD_INFO["AcceptableVertexBufferDeclarations"];
            //JArray buffers = (JArray)MTD_LAYOUT_MEMBERS.First()["Buffers"];
            List<FLVER2.BufferLayout> layouts = new();

            for (int i = 0; i < MTD_LAYOUT_MEMBERS.Count; i++) {
                FLVER2.BufferLayout BL = new();
                JArray buffers = (JArray)MTD_LAYOUT_MEMBERS[i]["Buffers"][0];
                foreach (JObject buffer in buffers) {
                    MBT mbt = (MBT)uint.Parse(buffer["Type"].ToString());
                    MBS mbs = (MBS)uint.Parse(buffer["Semantic"].ToString());
                    if (isStatic && mbs == MBS.BoneWeights) { continue; }
                    BL.Add(new FLVER.LayoutMember(mbt, mbs));
                }
                layouts.Add(BL);
            }

            return layouts;
        }

        public static List<TextureKey> getTextureMap(string MTDName) {
            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JToken[] MTD_TEXTURE_MEMBERS = MTD_INFO["TextureChannels"].ToArray();
            List<TextureKey> TM = new();
            for (int i = 0; i < MTD_TEXTURE_MEMBERS.Length; i++) {
                string TexMem = MTD_TEXTURE_MEMBERS[i].First.ToString();
                switch (TexMem) {
                    case "g_DiffuseTexture": TM.Add(new TextureKey("Texture", TexMem, 0x1, true)); break;
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

        public static FLVER2.GXList getGXList(string MTDName) {
            if (GX_INFO_LIST == null) {
                loadGXInfoList();
            }

            JArray GX_INFO = getGXExampleInfo(MTDName);
            JArray MTD_INFO = (JArray)getMTDInfo(MTDName)["GXItems"];
            if (MTD_INFO.Count != GX_INFO.Count)
                throw new Exception($"Missing example for {MTDName} items. GXItems count and GX_INFO count do not match {nameof(MTD_INFO)}{MTD_INFO.Count} {nameof(GX_INFO)}{GX_INFO.Count}");

            FLVER2.GXList gxList = new();

            for (int i = 0; i < MTD_INFO.Count; i++) {
                gxList.Add(new FLVER2.GXItem() {
                    ID = MTD_INFO[i]["GXID"].ToString(),
                    Unk04 = int.Parse(MTD_INFO[i]["Unk04"].ToString()),
                    Data = Convert.FromBase64String(GX_INFO[i].ToString())
                });
            }

            return gxList;
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
            /* This is bad times if this happens so throw an exception that gives better info than a null reference */
            throw new Exception($"MTD Info not found for {MTDName}");
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

        private static JArray getGXExampleInfo(string MTDName) {
            JObject gx_example_info = GX_INFO_LIST;
            return (JArray)gx_example_info[MTDName.ToLower()] ?? throw new Exception($"JObject returned null for {MTDName}"); //return the GXInfo, or throw
        }

        private static void loadGXInfoList() {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "PortJob.Resources.DS3_GX_EXAMPLE_INFO.json";
            string data;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new(stream)) {
                data = reader.ReadToEnd();
            }

            JObject json = JObject.Parse(data);
            GX_INFO_LIST = json;
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