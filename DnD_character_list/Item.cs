using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class Item
{
    public int IdItem { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<ItemInventory> ItemInventories { get; set; } = new List<ItemInventory>();
}
