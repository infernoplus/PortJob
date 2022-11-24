using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonFunc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Override {
    public class Override {
        public static void load() {
            Log.Info(0, "Loading overrides...");
            DebugCells.load();
        }
    }

    public class DebugCells {
        public static List<DebugCellAvoid> avoids;
        public static List<DebugCellPrefer> prefers;

        public static bool IsPrefer(string name) {
            foreach(DebugCellPrefer prefer in prefers) {
                if(prefer.name == name) { return true; }
            }
            return false;
        }

        public static bool IsAvoid(string name) {
            foreach (DebugCellAvoid avoid in avoids) {
                if (avoid.name == name) { return true; }
            }
            return false;
        }

        public static void load() {
            avoids = new();
            prefers = new();

            JsonSerializer serializer = new();
            JObject debug_cells = null;
            using (FileStream s = File.Open("Overrides/debug_cells.json", FileMode.Open))
            using (StreamReader sr = new(s))
            using (JsonReader reader = new JsonTextReader(sr)) {
                while (!sr.EndOfStream) {
                    debug_cells = serializer.Deserialize<JObject>(reader);
                }
            }

            JArray avoid = (JArray)(debug_cells["avoid"]);
            JArray prefer = (JArray)(debug_cells["prefer"]);

            for (int j = 0; j < avoid.Count; j++) {
                avoids.Add(new DebugCellAvoid((JObject)(avoid[j])));
            }

            for (int j = 0; j < prefer.Count; j++) {
                prefers.Add(new DebugCellPrefer((JObject)(prefer[j])));
            }
        }
    }

    public class DebugCellPrefer {
        public string name;
        public Int2 grid;  // Position on cell grid
        public DebugCellPrefer(JObject rec) {
            name = rec["name"] != null ? rec["name"].ToString() : null;
            if (rec["grid"] != null) {
                int x = int.Parse(rec["grid"][0].ToString());
                int y = int.Parse(rec["grid"][1].ToString());
                grid = new Int2(x, y);
            } else {
                grid = null;
            }
        }
    }

    public class DebugCellAvoid {
        public string name;
        public Int2 grid;    // Position on cell grid
        public DebugCellAvoid(JObject rec) {
            name = rec["name"] != null ? rec["name"].ToString() : null;
            if (rec["grid"] != null) {
                int x = int.Parse(rec["grid"][0].ToString());
                int y = int.Parse(rec["grid"][1].ToString());
                grid = new Int2(x, y);
            } else {
                grid = null;
            }
        }
    }
}