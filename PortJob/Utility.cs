using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    static class Utility {

        private static readonly char[] _dirSep = { '\\', '/' };

        /* Take a full file path and returns just a file name without directory or extensions */
        public static string PathToFileName(string fileName) {
            if (fileName.EndsWith("\\") || fileName.EndsWith("/"))
                fileName = fileName.TrimEnd(_dirSep);

            if (fileName.Contains("\\") || fileName.Contains("/"))
                fileName = fileName.Substring(fileName.LastIndexOfAny(_dirSep) + 1);

            if (fileName.Contains("."))
                fileName = fileName.Substring(0, fileName.LastIndexOf('.'));

            return fileName;
        }

        private static readonly char[] CHAR_NUMS = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        /* Take a FBX data channel name and return the index of it as an int */
        /* EX: Normals3 returns 3 */
        public static int GetChannelIndex(string s) {
            return int.Parse(s.Substring(s.IndexOfAny(CHAR_NUMS)));
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize) {
            while (source.Any()) {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        /* Temporary code for packing up hkxs */
        public static void PackTestCol(string outputPath) { 
            string pathH = outputPath + @"col\h30_00_00_00_000000.hkx";
            string pathL = outputPath + @"col\l30_00_00_00_000000.hkx";
            BXF4 bxfH = new();
            BXF4 bxfL = new();

            bxfH.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 0, "m30_00_00_00\\" + Path.GetFileName(pathH) + ".dcx", File.ReadAllBytes(pathH)) { CompressionType = DCX.Type.Zlib });
            bxfH.Write(pathH.Replace("col\\", "map\\") + "bhd", pathH.Replace("col\\", "map\\") + "bdt");

            bxfL.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 0, "m30_00_00_00\\" + Path.GetFileName(pathL) + ".dcx", File.ReadAllBytes(pathL)) { CompressionType = DCX.Type.Zlib });
            bxfL.Write(pathL.Replace("col\\", "map\\") + "bhd", pathL.Replace("col\\", "map\\") + "bdt");
        }
    }
}
