using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Order
{
    public string IdOrder { get; set; } = null!;

    public string ClientDni { get; set; } = null!;

    public string? DeliveryDni { get; set; }

    public string IdMethod { get; set; } = null!;

    public DateTime? DateOrder { get; set; }

    public decimal Total { get; set; }

    public int? StartTime { get; set; }

    public int? EndTime { get; set; }

    public int? EstimatedTime { get; set; }

    public int? RealTime { get; set; }

    public virtual Client ClientDniNavigation { get; set; } = null!;

    public virtual DeliveryMan? DeliveryDniNavigation { get; set; }

    public virtual PayMethod IdMethodNavigation { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
