using CommonFunc;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    /* Class for managing FXR files that will be built into an msbs fxr/resource bnds */
    public class FXRManager {
        public static string TEST = "CommonFunc.Resources.test_light.fxr";

        int area;
        public Dictionary<ObjectInfo, FXRInfo> fxrObjs;

        public FXRManager(int area) {
            this.area = area;
            fxrObjs = new();
        }

        public void CreateLightSFX(Paramanger paramanger, ObjectInfo objectInfo) {
            if(fxrObjs.ContainsKey(objectInfo)) { return; }

            FXRInfo fxrInfo = new FXRInfo((objectInfo.id * 1000) + 000);
            fxrObjs.Add(objectInfo, fxrInfo);

            paramanger.CreateLightModelSFXParam(objectInfo, fxrInfo);
        }

        public void Write(string dir) {
            BND4 sfxBnd = new BND4();
            string sfxPath = $"sfx\\effect\\";
            int i=0;
            foreach (KeyValuePair<ObjectInfo, FXRInfo> fxrInfo in fxrObjs) {
                FXR3 fxr = FXR3.Read(Utility.GetEmbededResourceBytes(TEST));
                sfxBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, i++, $"{sfxPath}f{fxrInfo.Value.id:D9}.fxr", fxr.Write()));
            }
            sfxBnd.Write($"{dir}sfx\\frpg_sfxbnd_m{area:D2}_effect.ffxbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            File.WriteAllBytes($"{dir}sfx\\frpg_sfxbnd_m{area:D2}_resource.ffxbnd.dcx", Utility.GetEmbededResourceBytes("CommonFunc.Resources.test_resource.ffxbnd.dcx"));
        }
    }

    public class FXRInfo {
        public int id;
        public FXRInfo(int id) {
            this.id = id;
        }
    }
}
