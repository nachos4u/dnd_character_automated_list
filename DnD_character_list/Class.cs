using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Class
{
    public int IdClass { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Possession { get; set; } = null!;

    public int IdHitDice { get; set; }

    public virtual HitDice IdHitDiceNavigation { get; set; } = null!;

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();
}
