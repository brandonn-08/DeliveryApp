using System;
using System.Collections.Generic;

namespace DeliveryApi.Models;

// 🚨 HERENCIA: Admin también hereda de Person (cierra la Fase 1)
public partial class Admin : Person
{
    public string? Sucursal { get; set; }
    public string? Rol { get; set; }
    public decimal? Salary { get; set; }
    public bool? Status { get; set; }
}