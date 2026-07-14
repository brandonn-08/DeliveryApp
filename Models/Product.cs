using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Product
{
    public string IdProduct { get; set; } = null!;

    public string? IdCategory { get; set; }

    public int? IdRestaurant { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public virtual Category? IdCategoryNavigation { get; set; }

    public virtual Restaurant? IdRestaurantNavigation { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
