using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class ExtraIngredient
{
    public int IdIngredient { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual ICollection<ItemExtra> ItemExtras { get; set; } = new List<ItemExtra>();
}
