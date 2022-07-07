using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBXConverter {
    internal class Program {
        public static string MorrowindPath { get; set; }
        public static string OutputPath { get; set; }
        public static void Main(string[] args) {
            SetupPaths();
        }
        private static void SetupPaths() {
            string jsonString = Utility.GetEmbededResource("PortJob.Resources.settings.json");
            JObject settings = JObject.Parse(jsonString);
            MorrowindPath = settings["morrowind"].ToString();
            OutputPath = settings["output"].ToString();
            if (!MorrowindPath.EndsWith("\\"))
                MorrowindPath += "\\";

            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";
        }
    }
}
