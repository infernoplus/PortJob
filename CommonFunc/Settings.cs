using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {
    public static class Settings {
        public static string OutputPath { get; set; }
        public static string MorrowindPath { get; set; }
        public static bool GENERATE_NICE_TERRAIN { get; private set; }
        public static void InitSettings() {


            string jsonString = Utility.GetEmbededResource("CommonFunc.Resources.settings.json");
            JObject settings = JObject.Parse(jsonString);
            MorrowindPath = settings["morrowind"].ToString();
            OutputPath = settings["output"].ToString();
            if (!MorrowindPath.EndsWith("\\"))
                MorrowindPath += "\\";

            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";

            GENERATE_NICE_TERRAIN = bool.Parse(settings["generate_nice_terrain"].ToString());

        }
    }
}
