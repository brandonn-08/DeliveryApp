using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Restaurant
{
    public int IdRestaurant { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
