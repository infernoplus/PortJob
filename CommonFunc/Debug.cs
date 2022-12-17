using SoulsFormats;
using System;

namespace CommonFunc {
    public class Debug {
        public static bool FindFlverByMaterial(FLVER2 flver, string target) {
            foreach (FLVER2.Material mat in flver.Materials) {
                if (mat.MTD.ToLower().EndsWith(target))
                    return true;
            }
            return false;
        }
                /* Random testing and debug stuff */
        public static FLVER2 FlverSearch(string[] files, string target ,Func<FLVER2, string, bool> condition) {
            foreach (string file in files) {
                try {
                    BND4 bnd = BND4.Read(file);
                    foreach (BinderFile binderFile in bnd.Files) {
                        if (binderFile.Name.ToLower().Contains("flver")) {
                            FLVER2 flver = FLVER2.Read(binderFile.Bytes);

                            if (condition(flver, target)) {
                                Console.WriteLine($"FLVER found!\ntarget: {target}\nFile: {file}");
                                return flver;
                            }

                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine($"{file} failed. Exception:\n{e}");
                }
                
            }
            
            return null;
        }

    }
}