using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

public partial class Admin
{
    public string Dni { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Mail { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Sucursal { get; set; }

    public string? Rol { get; set; }

    public decimal? Salary { get; set; }

    public bool? Status { get; set; }
}
