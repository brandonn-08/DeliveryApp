using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class ItemExtra
{
    public int IdItem { get; set; }

    public int IdIngredient { get; set; }

    public decimal ExtraPrice { get; set; }

    public virtual ExtraIngredient IdIngredientNavigation { get; set; } = null!;

    public virtual OrderItem IdItemNavigation { get; set; } = null!;
}
