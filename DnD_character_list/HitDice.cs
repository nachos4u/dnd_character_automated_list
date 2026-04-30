using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class HitDice
{
    public int IdHitDice { get; set; }

    public string HitDice1 { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
