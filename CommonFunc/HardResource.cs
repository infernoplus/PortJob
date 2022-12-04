using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {
    /* A list of hardcoded resources that need to be copied into the mod. */
    public class HardResource {
        public static Dictionary<string, string> LIST = new() {
            { "CommonFunc.Resources.common.emevd.dcx", @"event\common.emevd.dcx" },
            { "CommonFunc.Resources.common_func.emevd.dcx", @"event\common_func.emevd.dcx" },
            { "CommonFunc.Resources.mtdbnd.dcx", @"mtd\allmaterialbnd.mtdbnd.dcx" },
            { "CommonFunc.Resources.test_sky.objbnd.dcx", @"obj\o004900.objbnd.dcx" }
        };


        /* Gotta write em all */
        public static void Write(string dir) {
            foreach (KeyValuePair<string, string> item in LIST) {
                string itemPath = $"{dir}{item.Value}";
                Directory.CreateDirectory(Path.GetDirectoryName(itemPath));
                File.WriteAllBytes(itemPath, Utility.GetEmbededResourceBytes(item.Key));
            }
        }

        /* Write generic Exterior GParam to specified directory */
        public static void WriteExtGP(string dir) {

        }

        /* Write generic Interior GParam to specified directory */
        public static void WriteIntGP(string dir) {

        }
    }
}
