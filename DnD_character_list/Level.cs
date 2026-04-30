using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Level
{
    public int Level1 { get; set; }

    public int IdClass { get; set; }

    public string? Cells { get; set; }

    public string? Skills { get; set; }

    public string? ClassTableFeatures { get; set; }

    public virtual Class IdClassNavigation { get; set; } = null!;

    public virtual ICollection<Character> IdCharacters { get; set; } = new List<Character>();
}
