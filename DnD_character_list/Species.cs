using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Species
{
    public int IdSpecies { get; set; }

    public string Name { get; set; } = null!;

    public string SpeciesSkills { get; set; } = null!;

    public string SpeciesChaTics { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string Speed { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();
}
