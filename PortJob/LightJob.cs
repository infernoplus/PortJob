using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    /* Manages the btl light file for an msb */
    class LightJob {
        public int area, block;
        public BTL btl;
        public BTAB btab;
        public LightJob(int area, int block) {
            this.area = area; this.block = block;
            btl = new();
            btl.Version = 6;
            btl.Compression = SoulsFormats.DCX.Type.DCX_DFLT_10000_44_9;

            btab = new();
            btab.Entries = new();
            btab.BigEndian = false;
            btab.LongFormat = true;
            btab.Compression = SoulsFormats.DCX.Type.DCX_DFLT_10000_44_9;
        }

        /* Converts morrowind light reference into btl light. Only used for non physical lights (see Light vs LightContent) */
        public void CreateLight(Light mwl) {
            BTL.Light dsl = new();
            dsl.Name = mwl.id;

            dsl.Type = BTL.LightType.Point;
            dsl.Position = mwl.position;
            dsl.Radius = mwl.radius;
            dsl.Rotation = Vector3.Zero;

            dsl.DiffuseColor = System.Drawing.Color.FromArgb(mwl.color.x, mwl.color.y, mwl.color.z, 255);
            dsl.DiffusePower = 2;

            dsl.SpecularColor = System.Drawing.Color.FromArgb(mwl.color.x, mwl.color.y, mwl.color.z, 255);
            dsl.SpecularPower = 2;

            dsl.ShadowColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);

            dsl.FlickerBrightnessMult = 1;
            dsl.FlickerIntervalMax = 0;
            dsl.FlickerIntervalMin = 0;

            dsl.Sharpness = 1;
            dsl.NearClip = 1;
            dsl.CastShadows = false;
            dsl.ConeAngle = 0;

            /* Hardcoded unknown janko, copied from: m38_00.btl -> "動的_冒頭空洞_009" */
            //BTL test = BTL.Read(@"C:\Games\steamapps\common\DARK SOULS III\Game\map\m38_00_00_00\m38_00_00_00_0000.btl.dcx");
            dsl.Unk00 = 2551745110;
            dsl.Unk04 = 142197828;
            dsl.Unk08 = 3897043334;
            dsl.Unk0C = 4216194306;
            dsl.Unk1C = true;
            dsl.Unk30 = 0;
            dsl.Unk34 = 0;
            dsl.Unk50 = 4;
            dsl.Unk54 = 2;
            dsl.Unk5C = -1;
            dsl.Unk64 = new byte[] { 0, 0, 0, 1 };
            dsl.Unk68 = 0;
            dsl.Unk70 = 0;
            dsl.Unk80 = -1;
            dsl.Unk84 = new byte[] { 0, 0, 0, 0 };
            dsl.Unk88 = 0;
            dsl.Unk90 = 0;
            dsl.Unk98 = 1;
            dsl.UnkA0 = new byte[] { 1, 0, 2, 1 };
            dsl.UnkAC = 0;
            dsl.UnkBC = 0;
            dsl.UnkC0 = new byte[] { 0, 0, 0, 0 };
            dsl.UnkC4 = 0;
            dsl.UnkC8 = 0;
            dsl.UnkCC = 0;
            dsl.UnkD0 = 0;
            dsl.UnkD4 = 0;
            dsl.UnkD8 = 0;
            dsl.UnkDC = 0;
            dsl.UnkE0 = 0;
            dsl.Width = 0;

            btl.Lights.Add(dsl);
        }

        public void Write(string dir) {
            string path = $"{dir}map\\m{area:D2}_{block:D2}_00_00\\m{area:D2}_{block:D2}_00_ 00_0000";
            btl.Write($"{path}.btl.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            btab.Write($"{path}.btab.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }
}
