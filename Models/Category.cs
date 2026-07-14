using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Category
{
    public string IdCategory { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
