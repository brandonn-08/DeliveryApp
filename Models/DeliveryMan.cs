using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class DeliveryMan
{
    public string Dni { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly? DateBirth { get; set; }

    public string Mail { get; set; } = null!;

    public string? Phone { get; set; }

    public string Password { get; set; } = null!;

    public string? TypeVehicle { get; set; }

    public string? LicencePlate { get; set; }

    public bool? Status { get; set; }

    public decimal? Rating { get; set; }

    public decimal? Comission { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
