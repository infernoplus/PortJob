using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class Log {
        public static int last = 0;
        private static readonly string EXT = "log";
        private static StreamWriter _writer { get; set; }
        public static void SetupLogStream() {
            if (!Directory.Exists($"{PortJob.OutputPath}port_logs\\"))
                Directory.CreateDirectory($"{PortJob.OutputPath}port_logs\\");

            if (Directory.Exists($"{PortJob.OutputPath}port_logs\\sub_logs\\"))
                Directory.Delete($"{PortJob.OutputPath}port_logs\\sub_logs\\", true);

            _writer = new($"{PortJob.OutputPath}port_logs\\main_log.{EXT}", false) {
                AutoFlush = true
            };
        }

        public static void CloseWriter() {
            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
        }

        public static void Info(int lvl, string msg, string fileName = null) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            LogToFile(msg, fileName);
        }

        public static void Error(int lvl, string msg, string fileName = null) {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); } else { last = lvl; }
            msg = "!!! " + msg + " !!!";
            for (int i = 0; i < lvl; i++) {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
            LogToFile(msg, fileName);
        }

        private static void LogToFile(string msg, string fileName)
        {
            _writer.WriteLine(msg);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (!msg.EndsWith('\n'))
                msg += '\n';

            string subPath = Path.GetDirectoryName(fileName);
            if (!Directory.Exists($"{PortJob.OutputPath}port_logs\\sub_logs\\{subPath}\\"))
                Directory.CreateDirectory($"{PortJob.OutputPath}port_logs\\sub_logs\\{subPath}\\");

            File.AppendAllText($"{PortJob.OutputPath}port_logs\\sub_logs\\{fileName}.{EXT}", msg);
        }
    }
}
