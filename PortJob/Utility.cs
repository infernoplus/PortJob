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
        public static void PackTestCol(int area, int block) {

            /* Setup area_block and output path*/
            string area_block = $"{area:D2}_{block:D2}";
            string outputPath = PortJob.OutputPath;
            string mapName = $"m{area_block}_00_00";

            /* Write the high col bhd/bdt pair */
            string hDonorPath = Environment.CurrentDirectory + @"..\..\..\..\TestCol\h30_00_00_00_000000.hkx";
            string hPath = $"{mapName}\\h{area_block}_00_00";
            BXF4 hBXF = new();
            byte[] hBytes = File.ReadAllBytes(hDonorPath);
            hBytes = DCX.Compress(hBytes, DCX.Type.DCX_DFLT_10000_44_9); //File is compressed inside the bdt.
            int hStartId = 0; // h col binder file IDs start here and increment by 1
            BinderFile hBinder = new(Binder.FileFlags.Flag1, hStartId, $"{hPath}_000000.hkx.dcx", hBytes);
            hBXF.Files.Add(hBinder);
            hBXF.Write($"{outputPath}map\\{hPath}.hkxbhd", $"{outputPath}map\\{hPath}.hkxbdt");

            /* Write the low col bhd/bdt pair */
            string lDonorPath = Environment.CurrentDirectory + @"..\..\..\..\TestCol\l30_00_00_00_000000.hkx"; //:fatcat:
            string lPath = $"{mapName}\\l{area_block}_00_00";
            BXF4 lBXF = new();
            byte[] lBytes = File.ReadAllBytes(lDonorPath);
            lBytes = DCX.Compress(lBytes, DCX.Type.DCX_DFLT_10000_44_9);  //File is compressed inside the bdt.
            int lStartId = 0; // l col binder file IDs start here and increment by 1
            BinderFile lBinder = new(Binder.FileFlags.Flag1, lStartId, $"{lPath}_000000.hkx.dcx", lBytes);
            lBXF.Files.Add(lBinder);
            lBXF.Write($"{outputPath}map\\{lPath}.hkxbhd",$"{outputPath}map\\{lPath}.hkxbdt");

            /* Write the nav mesh bnd */
            string nDonorPath = Environment.CurrentDirectory + @"..\..\..\..\TestCol\n30_00_00_00_000000.hkx"; //:fatcat:
            string nName = $"{area_block}_00_00"; //Have to seperate the name here, cause the path is long AF
            string nPath = $"N:\\FDP\\data\\INTERROOT_win64\\map\\{mapName}\\navimesh\\bind6\\n{nName}";
            BND4 nvmBND = new();
            byte[] nBytes = File.ReadAllBytes(nDonorPath);
            int nStartId = 1000; // navmesh binder file IDs start here and increment by 1
            BinderFile nBinder = new(Binder.FileFlags.Flag1, nStartId, $"{nPath}_000000.hkx", nBytes);
            nvmBND.Files.Add(nBinder);
            nvmBND.Write($"{outputPath}map\\{mapName}\\m{nName}.nvmhktbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9); //Whole bnd is compressed. 
        }

        /* If you don't like these summaries, I will replace them with regular comments.
         They show up in the tooltips for the method, though, and are quite helpful at times! */
        /// <summary>
        /// Delete numbers from the end of a number
        /// </summary>
        /// <param name="num">The number to be trimmed</param>
        /// <param name="length">How many numbers to trim off the end of the num</param>
        /// <returns>num after removing length amount of numbers from the end</returns>
        public static int DeleteFromEnd(int num, int length) {
            for (int i = 1; num != 0; i++) {
                num /= 10;

                if (i == length)
                    return num;
            }

            return 0;
        }
        /// <summary>
        /// Gets embedded resource from path Example: "PortJob.Resources.settings.json"
        /// </summary>
        /// <param name="item">ProjectName.FolderName.FileName.ext</param>
        /// <returns>string of the file specified.</returns>
        /// <exception cref="NullReferenceException">Couldn't find the file in the path provided. Make sure you follow the format.</exception>
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
