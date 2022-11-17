using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBXConverter {
    internal class ColConverter {

        public static void Run(string objPath) {
            Process converter = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = $@"{Environment.CurrentDirectory}\ColConverter\obj2fsnp.bat",
                    WorkingDirectory = $@"{Environment.CurrentDirectory}\ColConverter\",
                    Arguments = objPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    RedirectStandardError = false, //Cannot re-direct standard output while checking IsDone, or this child process will freeze.  
                    RedirectStandardOutput = false,
                },
            };

            converter.Start();
            converter.WaitForExit();

            string objFolder = Path.GetDirectoryName(objPath);
            string[] files = Directory.GetFiles(objFolder, "*.2013");
            foreach (string file in files) {
                File.Delete(file);
            }
            
            files = Directory.GetFiles(objFolder, "*.o2f");
            foreach (string file in files) {
                File.Delete(file);
            }
        }

    }
}
