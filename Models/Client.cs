using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Client
{
    public string Dni { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly? DateBirth { get; set; }

    public string? Street1 { get; set; }

    public string? Street2 { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public string? NumberHome { get; set; }

    public string? Reference { get; set; }

    public string Mail { get; set; } = null!;

    public string? Phone { get; set; }

    public bool? Genere { get; set; }

    public string Password { get; set; } = null!;

    public DateOnly? RegisterData { get; set; }

    public DateOnly? DateDelivery { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
