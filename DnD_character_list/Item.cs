using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Item
{
    public int IdItem { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Source { get; set; }

    public string? Price { get; set; }

    public float? Weight { get; set; }

    public bool IsMagic { get; set; } = false;

    public string? Rarity { get; set; }

    public string? ItemType { get; set; }

    public virtual ICollection<ItemInventory> ItemInventories { get; set; } = new List<ItemInventory>();
}
