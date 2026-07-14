using System;
using System.Collections.Generic;

namespace DeliveryApi.Models
{
    // 🚨 HERENCIA: DeliveryMan también hereda de Person
    public partial class DeliveryMan : Person
    {
        public string? TypeVehicle { get; set; }
        public string? LicencePlate { get; set; }
        public bool? Status { get; set; }
        public decimal? Rating { get; set; }
        public decimal? Comission { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}