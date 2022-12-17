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
        public static readonly List<FXRTemplate> TEMPLATES = new() { // Probably should move this into overrides
            new("CommonFunc.Resources.light-none.fxr", new() { "blue" }, new() { "light" }),
            new("CommonFunc.Resources.light-candle.fxr", new() { "candle", "chandelier" }, new() { "emitter" }),
            new("CommonFunc.Resources.light-lantern.fxr", new() { "lantern", "sconce", "lamp" }, new() { "emitter" }),
            new("CommonFunc.Resources.light-torch.fxr", new() { "torch", "brazier" }, new() { "fire" }),
            new("CommonFunc.Resources.light-fire.fxr", new() { "fire", "log" }, new() { "emitter" }),
        };

        readonly int area;
        public Dictionary<ObjectInfo, FXRInfo> fxrObjs;

        public FXRManager(int area) {
            this.area = area;
            fxrObjs = new();
        }

        public void CreateLightSFX(Paramanger paramanger, ObjectInfo objectInfo) {
            if(fxrObjs.ContainsKey(objectInfo)) { return; }

            FXRTemplate template = null;
            foreach(FXRTemplate tmp in TEMPLATES) {
                foreach (string id in tmp.fxrIdentifiers) {
                    if(objectInfo.name.ToLower().Contains(id)) {
                        template = tmp; break;
                    }
                }
                if (template != null) { break; }
            }
            
            if(template == null) { Console.WriteLine("OH NO!: " + objectInfo.name); template = TEMPLATES[0]; }

            FXRInfo fxrInfo = new((objectInfo.id * 1000) + 000, template);
            fxrObjs.Add(objectInfo, fxrInfo);

            paramanger.CreateLightModelSFXParam(objectInfo, fxrInfo);
        }

        public void Write(string dir) {
            BND4 sfxBnd = new BND4();
            string sfxPath = $"sfx\\effect\\";
            int i=0;
            foreach (KeyValuePair<ObjectInfo, FXRInfo> fxrInfo in fxrObjs) {
                FXR3 fxr = FXR3.Read(Utility.GetEmbededResourceBytes(fxrInfo.Value.template.path));
                sfxBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, i++, $"{sfxPath}f{fxrInfo.Value.id:D9}.fxr", fxr.Write()));
            }
            sfxBnd.Write($"{dir}sfx\\frpg_sfxbnd_m{area:D2}_effect.ffxbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            File.WriteAllBytes($"{dir}sfx\\frpg_sfxbnd_m{area:D2}_resource.ffxbnd.dcx", Utility.GetEmbededResourceBytes("CommonFunc.Resources.test_resource.ffxbnd.dcx"));
        }
    }

    /* Contains some info about an FXR template file. */
    public class FXRTemplate {
        public string path;
        public List<string> fxrIdentifiers;  // We are using the filename of a nif to guess what kind of light it is.
        public List<string> nodeIdentifiers; // Similar to above we have to guess the name of the dummy to attach this effect to based on what we think it is
        public FXRTemplate(string path, List<string> fxrIdentifiers, List<string> nodeIdentifiers) {
            this.path = path;
            this.fxrIdentifiers = fxrIdentifiers;
            this.nodeIdentifiers = nodeIdentifiers;
        }
    }

    public class FXRInfo {
        public readonly int id;
        public readonly FXRTemplate template;
        public FXRInfo(int id, FXRTemplate template) {
            this.id = id;
            this.template = template;
        }
    }
}
