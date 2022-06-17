using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob
{
    class Utility
    {

        private static readonly char[] _dirSep = new char[] { '\\', '/' };

        /* Take a full file path and returns just a file name without directory or extensions */
        public static string PathToFileName(string fileName)
        {
            if (fileName.EndsWith("\\") || fileName.EndsWith("/"))
                fileName = fileName.TrimEnd(_dirSep);

            if (fileName.Contains("\\") || fileName.Contains("/"))
                fileName = fileName.Substring(fileName.LastIndexOfAny(_dirSep) + 1);

            if (fileName.Contains("."))
                fileName = fileName.Substring(0, fileName.LastIndexOf('.'));

            return fileName;
        }

        private static readonly char[] CHAR_NUMS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        /* Take a FBX data channel name and return the index of it as an int */
        /* EX: Normals3 returns 3 */
        public static int GetChannelIndex(string s)
        {
            return int.Parse(s.Substring(s.IndexOfAny(CHAR_NUMS)));
        }
    }
}
