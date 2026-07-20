using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliveryApi.Models;

public partial class Order : ICalculate
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

    public int? IdRestaurant { get; set; }
    public DateTime? DispatchTime { get; set; }
    public virtual Restaurant? IdRestaurantNavigation { get; set; }

    public virtual Client ClientDniNavigation { get; set; } = null!;

    public virtual DeliveryMan? DeliveryDniNavigation { get; set; }

    public virtual PayMethod IdMethodNavigation { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // 🚨 IMPLEMENTACIÓN DE ICalculate
    public decimal CalculateTotal()
    {
        return OrderItems?.Sum(i => i.Subtotal) ?? 0;
    }

    public decimal CalculateTaxes()
    {
        const decimal comisionEnvio = 1.50m;
        return comisionEnvio;
    }
}