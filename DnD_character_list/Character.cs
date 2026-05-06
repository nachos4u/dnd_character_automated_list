using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Character
{
    public int IdCharacter { get; set; }

    public int IdSpecies { get; set; }

    public int IdBackground { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Possession { get; set; }

    public int? Kd { get; set; }

    public string? Worldview { get; set; }

    public string? Notes { get; set; }

    public int? Hitpoints { get; set; }

    public int? TimeHitpoints { get; set; }

    public string? Characteristiks { get; set; }

    public int? Speed { get; set; }

    public int? Exp { get; set; }

    public int? SpasWin { get; set; }

    public int? CurHp { get; set; }

    public int? SpasLose { get; set; }

    public string? PossesionNew { get; set; }

    public int? Gm { get; set; }

    public int? Sm { get; set; }

    public int? Mm { get; set; }

    public int? Em { get; set; }

    public int? Pm { get; set; }

    public virtual Background IdBackgroundNavigation { get; set; } = null!;

    public virtual Species IdSpeciesNavigation { get; set; } = null!;

    public virtual ICollection<ItemInventory> ItemInventories { get; set; } = new List<ItemInventory>();

    public virtual ICollection<Spell> IdSpells { get; set; } = new List<Spell>();

    public virtual ICollection<Trait> IdTraits { get; set; } = new List<Trait>();

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    // Отслеживание выбора навыков класса
    public int? PrimaryClassId { get; set; }
    public bool SkillsPending { get; set; }
    public string? PendingSkillChoices { get; set; }
    public int? PendingSkillCount { get; set; }
}
