﻿    using CliWrap;
    using CliWrap.Buffered;
    using SoulsFormats;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace CommonFunc {
        public class ColConverter {
            public static void Run(string objFolderPath) {

                MakeHKXFiles(objFolderPath);

                CleanupTempHKXFiles(objFolderPath);
            }
            private static void MakeHKXFiles(string objFolderPath) {

                string[] objFiles = Directory.GetFiles(objFolderPath, "*.obj", SearchOption.AllDirectories);
                string wd = Environment.CurrentDirectory;
                foreach (string objFile in objFiles) {
                    if (File.Exists($@"{Path.GetDirectoryName(objFile)}\{Path.GetFileNameWithoutExtension(objFile)}.hkx.dcx")) continue;
                    try {
                        Process converter = new Process {
                            StartInfo = new ProcessStartInfo {
                                FileName = $@"{wd}\ColConverter\obj2fsnp.bat",
                                WorkingDirectory = $@"{wd}\ColConverter\",
                                Arguments = $"\"{objFile}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardError = false, //Cannot re-direct standard output while checking IsDone, or this child process will freeze.  
                                RedirectStandardOutput = false,
                            },
                        };
                        converter.Start();
                        converter.WaitForExit();
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                        throw;
                    }

                    string hkxPath = $@"{Path.GetDirectoryName(objFile)}\{Path.GetFileNameWithoutExtension(objFile)}.hkx";
                    byte[] compressedHkx = DCX.Compress(File.ReadAllBytes(hkxPath), DCX.Type.DCX_DFLT_11000_44_9);
                    File.WriteAllBytes($"{hkxPath}.dcx", compressedHkx);
                    File.Delete(hkxPath);
                }
            }
            private static void CleanupTempHKXFiles(string objFolderPath) {

                string[] files = Directory.GetFiles(objFolderPath, "*.2013", SearchOption.AllDirectories);
                foreach (string file in files) {
                    File.Delete(file);
                }

                files = Directory.GetFiles(objFolderPath, "*.o2f", SearchOption.AllDirectories);
                foreach (string file in files) {
                    File.Delete(file);
                }
            }
        }
    }
