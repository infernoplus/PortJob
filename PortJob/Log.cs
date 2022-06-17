using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob
{
    class Log
    {
        public static int last = 0;

        public static void Info(int lvl, string msg)
        {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); }
            else { last = lvl; }
            for (int i = 0; i < lvl; i++)
            {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
        }

        public static void Error(int lvl, string msg)
        {
            if (lvl < 0) { lvl = last + ((-lvl) - 1); }
            else { last = lvl; }
            msg = "!!! " + msg + " !!!";
            for (int i = 0; i < lvl; i++)
            {
                msg = "  " + msg;
            }
            Console.WriteLine(msg);
        }
    }
}
