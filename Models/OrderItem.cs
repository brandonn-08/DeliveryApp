using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class OrderItem
{
    public int IdItem { get; set; }

    public string IdOrder { get; set; } = null!;

    public string IdProduct { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Order IdOrderNavigation { get; set; } = null!;

    public virtual Product IdProductNavigation { get; set; } = null!;

    public virtual ICollection<ItemExtra> ItemExtras { get; set; } = new List<ItemExtra>();
}
