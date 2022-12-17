using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CommonFunc;
using FBXConverter.Solvers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using Newtonsoft.Json;

namespace FBXConverter {
    internal class Program {
        //Modified Example 2: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-anonymous-pipes-for-local-interprocess-communication
        public static void Main(string[] args) {
            /* test func */
            /*List<int> scales = new();
            scales.Add(100);
            FBXConverter.convert(@"D:\Steam\steamapps\common\Morrowind\Data Files\meshes\l\light_com_chandelier_04.fbx", @"C:\Games\steamapps\common\DARK SOULS III\Game\mod\out.flver", @"C:\Games\steamapps\common\DARK SOULS III\Game\mod\cache\textures\", scales);
            return;*/
            string jsonString = null;
            if (args.Length <= 0) throw new Exception("Did not receive pipe handle");
            using (PipeStream pipeClient =
                   new AnonymousPipeClientStream(PipeDirection.In, args[0])) {

                using (StreamReader sr = new(pipeClient)) {
                    string temp;

                    do {
                        //Console.WriteLine("[CLIENT] Wait for sync...");
                        temp = sr.ReadLine();
                    }
                    while (!temp.StartsWith("SYNC"));

                    //Have to start read from the end of the "SYNC" test. IDK if SYNC is necessary, but it was in the documentation.
                    jsonString = sr.ReadToEnd();
                    //while ((jsonString = sr.ReadLine()) != null) {
                    //    //Console.WriteLine("[CLIENT] Echo: " + temp);
                    //}
                }
            }

            //throw new Exception("Test Error");
            if (jsonString == null) throw new Exception("Did not receive json DATA");
            Settings.InitSettings();

            Cache cache = new();
            FBXConverterJob job = Newtonsoft.Json.JsonConvert.DeserializeObject<FBXConverterJob>(jsonString);
            foreach (FBXInfo fbxInfo in job.FBXList) {
                //Console.WriteLine($"Converting: {fbxList["FBXPath"]}");
                ModelInfo model = FBXConverter.convert(fbxInfo.FBXPath, fbxInfo.FlverPath, job.TPFDir, fbxInfo.Scales);
                cache.models.Add(model);
            }
            string cacheJson = JsonConvert.SerializeObject(cache);
            System.IO.File.WriteAllText(job.OutputPath, cacheJson);

            //string argString = string.Join("", args);
            //args = argString.Split('|');
            //SetupPaths();
        }
    }
}
