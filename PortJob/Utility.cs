using DirectXTexNet;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Binder = SoulsFormats.Binder;

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
        public static void PackTestCol(string outputPath, int area, int block) {


            string pathH = Environment.CurrentDirectory + @"..\..\..\..\TestCol\h30_00_00_00_000000.hkx";
            string pathL = Environment.CurrentDirectory + @"..\..\..\..\TestCol\l30_00_00_00_000000.hkx";

            string area_block = $"{area:D2}_{block:D2}";

            BXF4 bxfH = new();
            BXF4 bxfL = new();

            bxfH.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 0, $"m{area_block}_00_00\\h{area_block}_00_00_000000.hkx.dcx", DCX.Compress(File.ReadAllBytes(pathH), DCX.Type.DCX_DFLT_10000_44_9) ) { CompressionType = DCX.Type.Zlib });
            bxfH.Write($"{outputPath}map\\m{area_block}_00_00\\h{area_block}_00_00.hkxbhd", //this is a unreadable huge meme right now
                $"{outputPath}map\\m{area_block}_00_00\\h{area_block}_00_00.hkxbdt"); //but this isn't really the proper place to do this.

            bxfL.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 0, $"m{area_block}_00_00\\l{area_block}_00_00_000000.hkx.dcx", DCX.Compress(File.ReadAllBytes(pathL), DCX.Type.DCX_DFLT_10000_44_9) ) { CompressionType = DCX.Type.Zlib });
            bxfL.Write($"{outputPath}map\\m{area_block}_00_00\\l{area_block}_00_00.hkxbhd", //this is a unreadable huge meme right now
                $"{outputPath}map\\m{area_block}_00_00\\l{area_block}_00_00.hkxbdt");//but this isn't really the proper place to do this.
        }
        public static int DeleteFromEnd(int num, int n) {
            for (int i = 1; num != 0; i++) {
                num = num / 10;

                if (i == n)
                    return num;
            }

            return 0;
        }

        public static string GetEmbededResource(string item) {
            Assembly assembly = Assembly.GetCallingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(item)) {
                if (stream == null)
                    throw new NullReferenceException($"Could not find embedded resource: {item} in the {Assembly.GetCallingAssembly().GetName()} assembly");

                using (StreamReader reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }

        }

    }
}
