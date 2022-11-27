using CommonFunc;
using SoulsFormats;
using SoulsIds;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class Script {
        private static int nextEntID = 1000;
        public static int NewEntID() { return nextEntID++; }

        private static int nextEvtID = 1000;
        public static int NewEvtID() { return nextEvtID++; }

        private static int nextFlag = 13000000;   // Testing
        public static int NewFlag() { return nextFlag++; }

        public static Events AUTO = new Events(@"C:\Games\steamapps\common\DARK SOULS III\DarkScript\Resources\ds3-common.emedf.json", true, true);

        public static readonly int EVT_LOAD_DOOR = 100;
        public static Dictionary<int, int> COMMON_EVENT_SLOTS = new() {
            { EVT_LOAD_DOOR, 0 }
        };

        public readonly int area, block;

        public EMEVD emevd;
        public EMEVD.Event init;
        public Script(int area, int block) {
            this.area = area;
            this.block = block;

            emevd = EMEVD.Read(Utility.GetEmbededResourceBytes("CommonFunc.Resources.template.emevd"));
            init = emevd.Events[0];
        }

        public void RegisterLoadDoor(DoorContent door) {
            int actionParam = 9340;
            int area, block;
            if (door.marker.exit.layout != null) { area = 54; block = door.marker.exit.layout.id; } // Hacky and bad
            else { area = 30; block = door.marker.exit.layint.id; }

            int SLOT = COMMON_EVENT_SLOTS[EVT_LOAD_DOOR]++;
            init.Instructions.Add(AUTO.ParseAdd($"InitializeEvent({SLOT}, {EVT_LOAD_DOOR}, {area}, {block}, {actionParam}, {door.entityID}, {door.marker.entityID});"));
        }

        public void Write(string dir) {
            emevd.Write($"{dir}\\m{area:D2}_{block:D2}_00_00.emevd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }
}
