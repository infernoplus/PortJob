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
            //FBXConverter.convert(@"D:\Steam\steamapps\common\Morrowind\Data Files\meshes\f\furn_mist256.fbx", @"C:\Games\steamapps\common\DARK SOULS III\Game\mod\cache\meshes\x\test.flver", @"C:\Games\steamapps\common\DARK SOULS III\Game\mod\cache\textures\");
            /* */
            //return;
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
            JObject jsonObj = JObject.Parse(jsonString);

            foreach (JObject fbxList in jsonObj["FBXList"]) {
                Console.WriteLine($"Converting: {fbxList["FBXPath"]}");
                ModelInfo model = FBXConverter.convert(fbxList["FBXPath"].ToString(), fbxList["FlverPath"].ToString(), fbxList["TpfDir"].ToString());
                cache.models.Add(model);
            }

            string cacheJson = JsonConvert.SerializeObject(cache);
            System.IO.File.WriteAllText(jsonObj["OutputPath"].ToString(), cacheJson);

            //string argString = string.Join("", args);
            //args = argString.Split('|');
            //SetupPaths();
        }
    }
}
