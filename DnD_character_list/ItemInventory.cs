using System;
using System.Collections.Generic;

namespace DnD_character_list;

public partial class ItemInventory
{
    public int IdItem { get; set; }

    public int IdCharacter { get; set; }

    public int Quantity { get; set; }

    public virtual Character IdCharacterNavigation { get; set; } = null!;

    public virtual Item IdItemNavigation { get; set; } = null!;
}
