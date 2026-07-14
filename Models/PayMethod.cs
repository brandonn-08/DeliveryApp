using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class PayMethod
{
    public string IdMethod { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Provider { get; set; }

    public string? Details { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
