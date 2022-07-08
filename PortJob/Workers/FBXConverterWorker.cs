using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoulsFormats;

using Newtonsoft.Json;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace PortJob {
    public class FBXConverterWorker : Worker {
        private Process _pipeClient { get; set; }
        private string _jsonString { get; }
        public FBXConverterWorker(string outputPath, string morrowindPath, float globalScale, List<FBXInfo> fbxList) {
            _jsonString = JsonConvert.SerializeObject(new { OutputPath = outputPath, MorrowindPath = morrowindPath, GLOBAL_SCALE = globalScale, FBXList = fbxList });
            _thread = new Thread(CallFBXConverter) {
                IsBackground = true,
            };
            _thread.Start();
        }

        //Modified Example 1: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-anonymous-pipes-for-local-interprocess-communication
        private void CallFBXConverter() {

            _pipeClient = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = $"{Environment.CurrentDirectory}\\FBXConverter\\FBXConverter.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    //RedirectStandardOutput = true //Cannot re-direct standard output while checking IsDone, or this child process will freeze.  
                }
            };
            using (AnonymousPipeServerStream pipeServer = new(PipeDirection.Out, HandleInheritability.Inheritable)) {
                _pipeClient.StartInfo.Arguments = pipeServer.GetClientHandleAsString();
                _pipeClient.Start();

                pipeServer.DisposeLocalCopyOfClientHandle();

                try {
                    using (StreamWriter sw = new(pipeServer)) {
                        sw.AutoFlush = true;

                        sw.WriteLine("SYNC");
                        pipeServer.WaitForPipeDrain();
                        sw.WriteLine(_jsonString);
                    }

                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e) {
                    Console.WriteLine("[SERVER] Error: {0}", e.Message);
                }
            }
            //Will read the output of the pipeClient Program AFTER it has stopped. For debugging.
            _pipeClient.WaitForExit();
            IsDone = true;
            //Console.WriteLine(pipeClient.StandardOutput.ReadToEnd());
        }
    }

    public class FBXInfo {
        public string FBXPath { get; }
        public string FlverPath { get; }
        public string TpfDir { get; }
        public FBXInfo(string fbxPath, string flverPath, string tpfDir) {
            FBXPath = fbxPath;
            FlverPath = flverPath;
            TpfDir = tpfDir;
        }
    }
}

