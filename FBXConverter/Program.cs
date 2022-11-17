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

namespace FBXConverter {
    internal class Program {
        //Modified Example 2: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-anonymous-pipes-for-local-interprocess-communication
        public static void Main(string[] args) {
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

            JObject jsonObj = JObject.Parse(jsonString);

            foreach (JObject fbxList in jsonObj["FBXList"]) {
                Console.WriteLine($"Converting: {fbxList["FBXPath"]}");
                FBXConverter.convert(fbxList["FBXPath"].ToString(), fbxList["FlverPath"].ToString(), fbxList["TpfDir"].ToString());
            }

            //string argString = string.Join("", args);
            //args = argString.Split('|');
            //SetupPaths();
        }
    }
}
