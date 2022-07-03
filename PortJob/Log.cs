using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class Log {
        public static int last = 0;
        private static StreamWriter _writer { get; set; }
        public static void SetupLogStream(string outputPath) {
            if (!Directory.Exists($"{outputPath}port_logs\\"))
                Directory.CreateDirectory($"{outputPath}port_logs\\");

            _writer = new StreamWriter($"{outputPath}port_logs\\main_log.log", true);
            _writer.WriteLine($"");
            _writer.WriteLine($"=========={DateTime.Now}==========");
        }

        public static void Info(int lvl, string msg) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            _writer.WriteLine(msg);
        }
        public static void Info(int lvl, string msg, string fileName) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            File.AppendAllText($"{PortJob.OutputPath}port_logs\\{fileName}", msg);
            _writer.WriteLine(msg);
        }

        public static void Error(int lvl, string msg) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            msg = "!!! " + msg + " !!!";
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            _writer.WriteLine(msg);
        }

        public static void Error(int lvl, string msg, string fileName) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            msg = "!!! " + msg + " !!!";
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            File.AppendAllText($"{PortJob.OutputPath}{fileName}", msg);
            _writer.WriteLine(msg);
        }

    }
}
