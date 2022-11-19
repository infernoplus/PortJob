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

namespace CommonFunc {
    public static class Utility {

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
        public static void PackAreaCol(int area, int block) {
            /* Setup area_block and output path*/
            string outputPath = Settings.OutputPath;
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";
            string hPath = $"{mapName}\\h{area_block}_00_00";
            string lPath = $"{mapName}\\l{area_block}_00_00";

            string[] colFiles = Directory.GetFiles($"{outputPath}\\map\\{mapName}", "*.hkx.dcx");
            int startId = 0; // h col binder file IDs start here and increment by 1
            BXF4 lBXF = new();
            BXF4 hBXF = new();
            foreach (string col in colFiles) {
                byte[] bytes = File.ReadAllBytes(col);
                string name = Path.GetFileName(col).Substring(1);

                BinderFile hBinder = new(flags: Binder.FileFlags.Flag1, id: startId, name: $"{mapName}\\h{name}", bytes: bytes); //in-line parameter names help here to tell what is going on, but are not necessary.
                hBXF.Files.Add(hBinder);

                BinderFile lBinder = new(flags: Binder.FileFlags.Flag1, id: startId, name: $"{mapName}\\l{name}", bytes: bytes);
                lBXF.Files.Add(lBinder);
                startId++;
            }

            hBXF.Write($"{outputPath}map\\{hPath}.hkxbhd", $"{outputPath}map\\{hPath}.hkxbdt");
            lBXF.Write($"{outputPath}map\\{lPath}.hkxbhd", $"{outputPath}map\\{lPath}.hkxbdt");

            // /* Write the nav mesh bnd */
            // string[] navFiles = Directory.GetFiles($"{outputPath}\\map\\{mapName}\\hkx\\nav", "*.hkx");
            // BND4 nvmBND = new();
            // int nStartId = 1000; // navmesh binder file IDs start here and increment by 1
            // foreach (string nav in navFiles) {
            //     byte[] bytes = File.ReadAllBytes(nav);
            //     string name = Path.GetFileName(nav).Replace("-", "\\"); //Have to seperate the name here, cause the path is long AF
            //     string path = $"N:\\FDP\\data\\INTERROOT_win64\\map\\{mapName}\\navimesh\\bind6\\{name}";
            //     BinderFile nBinder = new(flags: Binder.FileFlags.Flag1, id: nStartId, name: path, bytes: bytes);
            //     nvmBND.Files.Add(nBinder);
            //     nStartId++;
            //
            // }
            // nvmBND.Write($"{outputPath}map\\{mapName}\\m{area_block}_00_00.nvmhktbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9); //Whole bnd is compressed. 
        }

        /* Temporary code for packing up hkx and navmesh BNDss */
        public static void PackTestColAndNavMeshes(int area, int block) {
            /* Setup area_block and output path*/
            string outputPath = Settings.OutputPath;
            string area_block = $"{area:D2}_{block:D2}";
            string mapName = $"m{area_block}_00_00";
            string hPath = $"{mapName}\\h{area_block}_00_00";
            string lPath = $"{mapName}\\l{area_block}_00_00";

            string[] colFiles = Directory.GetFiles($"{outputPath}\\map\\{mapName}\\hkx\\col", "*.hkx.dcx");
            int startId = 0; // h col binder file IDs start here and increment by 1
            BXF4 lBXF = new();
            BXF4 hBXF = new();
            foreach (string col in colFiles) {
                byte[] bytes = File.ReadAllBytes(col);
                string name = Path.GetFileName(col).Replace("-", "\\");

                BinderFile hBinder = new(flags: Binder.FileFlags.Flag1, id: startId, name: name, bytes: bytes); //in-line parameter names help here to tell what is going on, but are not necessary.
                hBXF.Files.Add(hBinder);

                BinderFile lBinder = new(flags: Binder.FileFlags.Flag1, id: startId, name: name.Replace("\\h", "\\l"), bytes: bytes);
                lBXF.Files.Add(lBinder);
                startId++;
            }

            hBXF.Write($"{outputPath}map\\{hPath}.hkxbhd", $"{outputPath}map\\{hPath}.hkxbdt");
            lBXF.Write($"{outputPath}map\\{lPath}.hkxbhd", $"{outputPath}map\\{lPath}.hkxbdt");

            /* Write the nav mesh bnd */
            string[] navFiles = Directory.GetFiles($"{outputPath}\\map\\{mapName}\\hkx\\nav", "*.hkx");
            BND4 nvmBND = new();
            int nStartId = 1000; // navmesh binder file IDs start here and increment by 1
            foreach (string nav in navFiles) {
                byte[] bytes = File.ReadAllBytes(nav);
                string name = Path.GetFileName(nav).Replace("-", "\\"); //Have to seperate the name here, cause the path is long AF
                string path = $"N:\\FDP\\data\\INTERROOT_win64\\map\\{mapName}\\navimesh\\bind6\\{name}";
                BinderFile nBinder = new(flags: Binder.FileFlags.Flag1, id: nStartId, name: path, bytes: bytes);
                nvmBND.Files.Add(nBinder);
                nStartId++;

            }
            nvmBND.Write($"{outputPath}map\\{mapName}\\m{area_block}_00_00.nvmhktbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9); //Whole bnd is compressed. 
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
        /// Get Embedded Resource as string. Format is AssemblyName.FolderName.FileName Example: "CommonFunc.Resources.settings.json"
        /// </summary>
        /// <param name="item">ProjectName.FolderName.FileName.ext</param>
        /// <returns>string of the file specified.</returns>
        /// <exception cref="NullReferenceException">Couldn't find the file in the path provided. Make sure you follow the format.</exception>
        public static string GetEmbededResource(string item) {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblyByName(item.Substring(0, item.IndexOf(".")));
            using (Stream? stream = assembly.GetManifestResourceStream(item)) {
                if (stream == null)
                    throw new NullReferenceException($"Could not find embedded resource: {item} in the {Assembly.GetCallingAssembly().GetName()} assembly");

                using (StreamReader reader = new(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }
        /// <summary>
        /// Get Embedded Resource as bytes. Format is AssemblyName.FolderName.FileName
        /// </summary>
        /// <param name="item"></param>
        /// <returns>byte array of the embedded resource</returns>
        /// <exception cref="NullReferenceException"></exception>
        public static byte[] GetEmbededResourceBytes(string item) {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblyByName(item.Substring(0, item.IndexOf(".")));
            using (Stream? stream = assembly.GetManifestResourceStream(item)) {
                if (stream == null)
                    throw new NullReferenceException($"Could not find embedded resource: {item} in the {Assembly.GetCallingAssembly().GetName()} assembly");
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }

        /// <summary>
        /// Get Assembly by name
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static Assembly GetAssemblyByName(this AppDomain domain, string assemblyName) {
            return domain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
        }

        public static bool IsEmbeddedResource(this string s) {
            Assembly executingAssembly = Assembly.GetEntryAssembly();
            if (s.StartsWith(executingAssembly.GetName().Name))
                return true;

            AssemblyName[] assemblies = executingAssembly.GetReferencedAssemblies();
            foreach (var assembly in assemblies) {
                if (s.StartsWith(assembly.Name))
                    return true;
            }

            return false;
        }

        public static string PathToEmbeddedPath(this string path) {
            return path.Replace("\\", ".");
        }

    }
}
