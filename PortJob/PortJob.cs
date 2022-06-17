using System;
using System.IO;
using System.Text;

using SoulsFormats;

namespace PortJob
{
    class PortJob
    {
        static void Main(string[] args)
        {
            FBXConverter.convert("D:\\Steam\\steamapps\\common\\Morrowind\\Data Files\\meshes\\x\\ex_dae_azura.fbx");
        }
    }
}
