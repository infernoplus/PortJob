using DirectXTexNet;
using System;
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
using System.Runtime.InteropServices;

namespace PortJob {
    public static class MTD {
        private static JArray MTD_INFO_LIST;
        private static JObject GX_INFO_LIST;

        public static FLVER2.BufferLayout getLayoutObsolete(string MTDName, bool isStatic) {

            if (MTD_INFO_LIST == null) {
                loadMTDInfoList();
            }

            JObject MTD_INFO = getMTDInfo(MTDName);
            JToken[] MTD_LAYOUT_MEMBERS = MTD_INFO["AcceptableVertexBufferDeclarations"].ToArray();
            FLVER2.BufferLayout BL = new();
            JArray buffers = (JArray)MTD_LAYOUT_MEMBERS.Last()["Buffers"].First;
            for (int i = 0; i < buffers.Count; i++) {
                MBT mbt = (MBT)uint.Parse(buffers[i]["Type"].ToString());
                MBS mbs = (MBS)uint.Parse(buffers[i]["Semantic"].ToString());
                int index = int.Parse(buffers[i]["Index"].ToString());
                int unk00 = int.Parse(buffers[i]["Unk00"].ToString());
                //int size = int.Parse(buffers[i]["Size"].ToString());
                if (isStatic && (mbs == MBS.BoneIndices || mbs == MBS.BoneWeights)) { continue; }
                BL.Add(new FLVER.LayoutMember(mbt, mbs, index, unk00));
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
                    if (isStatic && (mbs == MBS.BoneIndices || mbs == MBS.BoneWeights)) { continue; }
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
                    /* 'Normal' texture slot names */
                    case "g_DiffuseTexture": TM.Add(new TextureKey("Texture", TexMem, 0x1, true)); break;
                    case "g_DiffuseTexture2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_SpecularTexture": TM.Add(new TextureKey("Specular", TexMem, 0x1, true)); break;
                    case "g_SpecularTexture2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_ShininessTexture": TM.Add(new TextureKey("Specular", TexMem, 0x1, true)); break;
                    case "g_ShininessTexture2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_BumpmapTexture": TM.Add(new TextureKey("NormalMap", TexMem, 0x1, true)); break;
                    case "g_BumpmapTexture2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_DetailBumpmapTexture": TM.Add(new TextureKey("NormalMap", TexMem, 0x1, true)); break;
                    case "g_DetailBumpmapTexture2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_Envmap": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_DisplacementTexture": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
                    case "g_BlendMaskTexture": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "g_Lightmap": TM.Add(new TextureKey("Emissive", TexMem, 0x1, true)); break;
                    /* Absolute From Software tier wtf texture slot names */
                    case "MultiBlend3_et1_snp_Texture2D_1_GSBlendMap_AlbedoMap_0": TM.Add(new TextureKey("Texture", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_2_GSBlendMap_AlbedoMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_3_GSBlendMap_AlbedoMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_0_GSBlendMap_BlendEdgeTexture": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_13_GSBlendMap_NormalMap_0": TM.Add(new TextureKey("NormalMap", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_14_GSBlendMap_NormalMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_15_GSBlendMap_NormalMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_5_GSBlendMap_ReflectanceMap_0": TM.Add(new TextureKey("Specular", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_6_GSBlendMap_ReflectanceMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_7_GSBlendMap_ReflectanceMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend3_et1_snp_Texture2D_9_GSBlendMap_ShininessMap_0": TM.Add(new TextureKey("Specular", TexMem, 0x0, false)); break;
                    case "MultiBlend3_et1_snp_Texture2D_10_GSBlendMap_ShininessMap_1": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
                    case "MultiBlend3_et1_snp_Texture2D_11_GSBlendMap_ShininessMap_2": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
                    /* Kill me */
                    case "MultiBlend4_et1_snp_Texture2D_1_GSBlendMap_AlbedoMap_0": TM.Add(new TextureKey("Texture", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_2_GSBlendMap_AlbedoMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_3_GSBlendMap_AlbedoMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_4_GSBlendMap_AlbedoMap_3": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_0_GSBlendMap_BlendEdgeTexture": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_13_GSBlendMap_NormalMap_0": TM.Add(new TextureKey("NormalMap", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_14_GSBlendMap_NormalMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_15_GSBlendMap_NormalMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_16_GSBlendMap_NormalMap_3": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_5_GSBlendMap_ReflectanceMap_0": TM.Add(new TextureKey("Specular", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_6_GSBlendMap_ReflectanceMap_1": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_7_GSBlendMap_ReflectanceMap_2": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_8_GSBlendMap_ReflectanceMap_3": TM.Add(new TextureKey("x", TexMem, 0x1, true)); break;
                    case "MultiBlend4_et1_snp_Texture2D_9_GSBlendMap_ShininessMap_0": TM.Add(new TextureKey("Specular", TexMem, 0x0, false)); break;
                    case "MultiBlend4_et1_snp_Texture2D_10_GSBlendMap_ShininessMap_1": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
                    case "MultiBlend4_et1_snp_Texture2D_11_GSBlendMap_ShininessMap_2": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
                    case "MultiBlend4_et1_snp_Texture2D_12_GSBlendMap_ShininessMap_3": TM.Add(new TextureKey("x", TexMem, 0x0, false)); break;
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
            string jsonString = Utility.GetEmbededResource("PortJob.Resources.DS3_MTD_INFO.json");
            JObject json = JObject.Parse(jsonString);
            MTD_INFO_LIST = (JArray)json["mtds"];
        }

        private static JArray getGXExampleInfo(string MTDName) {
            JObject gx_example_info = GX_INFO_LIST;
            return (JArray)gx_example_info[MTDName.ToLower()] ?? throw new Exception($"JObject returned null for {MTDName}"); //return the GXInfo, or throw
        }

        private static void loadGXInfoList() {
            string jsonString = Utility.GetEmbededResource("PortJob.Resources.DS3_GX_EXAMPLE_INFO.json");
            JObject json = JObject.Parse(jsonString);
            GX_INFO_LIST = json;
        }

        public static byte[] GetSRGBTexture(string imagePath) {

            byte[] tex = imagePath.StartsWith("$PortJob") ? Utility.GetEmbededResourceBytes(imagePath.Replace("\\", ".").Substring(1)) : File.ReadAllBytes(imagePath);

            GCHandle pinnedArray = GCHandle.Alloc(tex, GCHandleType.Pinned);

            ScratchImage sImage = TexHelper.Instance.LoadFromDDSMemory(pinnedArray.AddrOfPinnedObject(), tex.Length, DDS_FLAGS.NONE);
            Image image = sImage.GetImage(0);
            sImage = sImage.Decompress(DXGI_FORMAT.B8G8R8A8_UNORM);

            string newFormat = $"{image.Format}_SRGB";
            DXGI_FORMAT format = (DXGI_FORMAT)Enum.Parse(typeof(DXGI_FORMAT), newFormat);
            TEX_COMPRESS_FLAGS texCompFlag = TEX_COMPRESS_FLAGS.SRGB;

            sImage = sImage.Compress(format, texCompFlag, 0.5f);
            sImage.OverrideFormat(format);

            UnmanagedMemoryStream stream = sImage.SaveToDDSMemory(DDS_FLAGS.FORCE_DX10_EXT);
            byte[] bytes = new byte[stream.Length];
            pinnedArray.Free();
            stream.Read(bytes);
            return bytes;
        }

        public static byte[] GetTexture(string imagePath) {
            if (imagePath.StartsWith("$PortJob"))
                return Utility.GetEmbededResourceBytes(imagePath.Replace("\\", ".").Substring(1));

            return File.ReadAllBytes(imagePath);
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