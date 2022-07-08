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

namespace FBXConverter {
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

        /* Temporary code for packing up hkxs */
        public static void PackTestCol(int area, int block) {

            /* Setup area_block and output path*/
            string outputPath = Program.OutputPath;
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";

            /* Write the high col bhd/bdt pair */
            int hModelID = 0;
            string hDonorPath = $"FBXConverter.TestCol.h30_00_00_00_{hModelID:D6}.hkx";
            string hPath = $"{mapName}\\h{area_block}_00_00";
            BXF4 hBXF = new();
            byte[] hBytes = GetEmbededResourceBytes(hDonorPath);
            hBytes = DCX.Compress(hBytes, DCX.Type.DCX_DFLT_10000_44_9); //File is compressed inside the bdt.
            int hStartId = 0; // h col binder file IDs start here and increment by 1
            BinderFile hBinder = new(flags: Binder.FileFlags.Flag1, id: hStartId, name: $"{hPath}_{hModelID:D6}.hkx.dcx", bytes: hBytes); //in-line parameter names help here to tell what is going on, but are not necessary.
            hBXF.Files.Add(hBinder);
            hBXF.Write($"{outputPath}map\\{hPath}.hkxbhd", $"{outputPath}map\\{hPath}.hkxbdt");

            /* Write the low col bhd/bdt pair */
            int lModelID = 0;
            string lDonorPath = $"FBXConverter.TestCol.l30_00_00_00_{lModelID:D6}.hkx"; //:fatcat:
            string lPath = $"{mapName}\\l{area_block}_00_00";
            BXF4 lBXF = new();
            byte[] lBytes = GetEmbededResourceBytes(lDonorPath);
            lBytes = DCX.Compress(lBytes, DCX.Type.DCX_DFLT_10000_44_9);  //File is compressed inside the bdt.
            int lStartId = 0; // l col binder file IDs start here and increment by 1
            BinderFile lBinder = new(flags: Binder.FileFlags.Flag1, id: lStartId, name: $"{lPath}_{lModelID:D6}.hkx.dcx", bytes: lBytes);
            lBXF.Files.Add(lBinder);
            lBXF.Write($"{outputPath}map\\{lPath}.hkxbhd", $"{outputPath}map\\{lPath}.hkxbdt");

            /* Write the nav mesh bnd */
            int nModelID = 1;
            string nDonorPath = $"FBXConverter.TestCol.n30_00_00_00_{nModelID:D6}.hkx"; //:fatcat:
            string nName = $"{area_block}_00_00"; //Have to seperate the name here, cause the path is long AF
            string nPath = $"N:\\FDP\\data\\INTERROOT_win64\\map\\{mapName}\\navimesh\\bind6\\n{nName}";
            BND4 nvmBND = new();
            byte[] nBytes = GetEmbededResourceBytes(nDonorPath);
            int nStartId = 1000; // navmesh binder file IDs start here and increment by 1
            BinderFile nBinder = new(flags: Binder.FileFlags.Flag1, id: nStartId, name: $"{nPath}_{nModelID:D6}.hkx", bytes: nBytes);
            nvmBND.Files.Add(nBinder);
            nvmBND.Write($"{outputPath}map\\{mapName}\\m{nName}.nvmhktbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9); //Whole bnd is compressed. 
        }

        public static byte[] GetEmbededResourceBytes(string item) {
            Assembly assembly = Assembly.GetCallingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(item)) {
                if (stream == null)
                    throw new NullReferenceException($"Could not find embedded resource: {item} in the {Assembly.GetCallingAssembly().GetName()} assembly");
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
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

                using (StreamReader reader = new(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

    }
}
