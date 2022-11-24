using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    /* Class that takes an MSB and auto generates the resource list for it */
    /* This is technically less efficent than just adding them normally but it takes a lot of repetitive coding out of portjob so we doing this */
    /* Also I'm sure there is a way to do this with generics but i cannot be fucking asked to code that right now. it's 5am and i have beers to drink biiiiitch */
    class AutoResource {
        public static void Generate(int area, int block, MSB3 msb) {
            /* Player */
            MSB3.Model.Player playerRes = new();
            playerRes.Name = "c0000";
            playerRes.SibPath = "N:\\FDP\\data\\Model\\chr\\c0000\\sib\\c0000.SIB";
            msb.Models.Players.Add(playerRes);

            /* Map Pieces */
            foreach (MSB3.Part.MapPiece mp in msb.Parts.MapPieces) {
                bool exists = false;
                foreach(MSB3.Model.MapPiece res in msb.Models.MapPieces) {
                    if(mp.ModelName == res.Name) { exists = true; }
                }
                if(exists) { continue; }

                MSB3.Model.MapPiece nures = new();
                nures.Name = mp.ModelName;
                nures.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\{mp.ModelName}.sib";
                msb.Models.MapPieces.Add(nures);
            }

            /* Collisions */
            foreach (MSB3.Part.Collision col in msb.Parts.Collisions) {
                bool exists = false;
                foreach (MSB3.Model.Collision res in msb.Models.Collisions) {
                    if (col.ModelName == res.Name) { exists = true; }
                }
                if (exists) { continue; }

                MSB3.Model.Collision nures = new();
                nures.Name = col.ModelName;
                nures.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{col.ModelName}.hkt";
                msb.Models.Collisions.Add(nures);
            }

            /* Connect Collision */
            foreach (MSB3.Part.ConnectCollision con in msb.Parts.ConnectCollisions) {
                bool exists = false;
                foreach (MSB3.Model.Collision res in msb.Models.Collisions) {
                    if (con.ModelName == res.Name) { exists = true; }
                }
                if (exists) { continue; }

                MSB3.Model.Collision nures = new();
                nures.Name = con.ModelName;
                nures.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\hkt\\{con.ModelName}.hkt";
                msb.Models.Collisions.Add(nures);
            }

            /* Objects */
            foreach (MSB3.Part.Object obj in msb.Parts.Objects) {
                bool exists = false;
                foreach (MSB3.Model.Object res in msb.Models.Objects) {
                    if (obj.ModelName == res.Name) { exists = true; }
                }
                if (exists) { continue; }

                MSB3.Model.Object nures = new();
                nures.Name = obj.ModelName;
                nures.SibPath = ""; // Unknown atm
                msb.Models.Objects.Add(nures);
            }

            /* Enemy */
            foreach (MSB3.Part.Enemy ene in msb.Parts.Enemies) {
                bool exists = false;
                foreach (MSB3.Model.Enemy res in msb.Models.Enemies) {
                    if (ene.ModelName == res.Name) { exists = true; }
                }
                if (exists) { continue; }

                MSB3.Model.Enemy nures = new();
                nures.Name = ene.ModelName;
                nures.SibPath = "";
                msb.Models.Enemies.Add(nures);
            }
        }
    }
}
