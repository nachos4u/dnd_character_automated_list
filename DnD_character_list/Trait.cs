using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Trait
{
    public int IdTrait { get; set; }

    public string CharTics { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Character> IdCharacters { get; set; } = new List<Character>();
}
