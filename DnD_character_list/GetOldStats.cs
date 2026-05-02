using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnD_character_list
{
    internal class GetOldStats
    {
        public Species getoldspecie(int DataIDCharacter)
        {
            Species prev_specie;
            using (var db = new DDInformationContext())
            {
                var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                prev_specie = db.Species.FirstOrDefault(s => s.IdSpecies == character.IdSpecies);
            }
            return prev_specie;
        }

        public Background getoldbackground(int DataIDCharacter)
        {
            Background prev_background;
            using (var db = new DDInformationContext())
            {
                var character = db.Characters.FirstOrDefault(c => c.IdCharacter == DataIDCharacter);
                prev_background = db.Backgrounds.FirstOrDefault(b => b.IdBackground == character.IdBackground);
            }
            return prev_background;
        }
    }
}
