using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Spell
{
    public int IdSpell { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Peculiarities { get; set; } = null!;

    public int CellLevel { get; set; }

    public string Source { get; set; } = null!;

    public string School { get; set; } = null!;

    public string Components { get; set; } = null!;

    public string? Range { get; set; }

    public string? Duration { get; set; }

    public string? Time { get; set; }

    public string? MaterialComponent { get; set; }

    public string? Upper { get; set; }

    public virtual ICollection<Character> IdCharacters { get; set; } = new List<Character>();
}
