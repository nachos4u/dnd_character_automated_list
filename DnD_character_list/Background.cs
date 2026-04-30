using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Background
{
    public int IdBackground { get; set; }

    public string Possesion { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? Gm { get; set; }

    public string Name { get; set; } = null!;

    public string? Invetary { get; set; }

    public string? Source { get; set; }

    public string? ToolOwnership { get; set; }

    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();
}
