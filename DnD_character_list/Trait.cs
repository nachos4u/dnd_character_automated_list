using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Trait
{
    public int IdTrait { get; set; }

    // Legacy column (nullable after migration)
    public string? CharTics { get; set; }

    public string Description { get; set; } = null!;

    public string? Name { get; set; }

    public string? Requirements { get; set; }

    public string? Source { get; set; }

    public virtual ICollection<Character> IdCharacters { get; set; } = new List<Character>();
}
