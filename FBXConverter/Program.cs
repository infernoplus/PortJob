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

        public static float GLOBAL_SCALE { get; set; }
        public static void Main(string[] args) {
            string argString = string.Join("", args);
            args = argString.Split('|');
            //SetupPaths();
            OutputPath = args[3].Replace("%%"," ");
            MorrowindPath = args[4].Replace("%%", " ");

            GLOBAL_SCALE = Convert.ToSingle(args[5]);

            FBXConverter.convert(args[0].Replace("%%", " "), args[1].Replace("%%", " "), args[2].Replace("%%", " "));
        }
    }
}
