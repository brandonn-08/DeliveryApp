using System;
using System.Collections.Generic;

namespace DeliveryApi.Models
{
    // 🚨 HERENCIA: Client hereda de Person
    public partial class Client : Person
    {
        // 🚨 OBJETO DE VALOR: Agrupamos las direcciones en un solo objeto estructurado
        public Address AddressData { get; set; } = new Address();

        public bool? Genere { get; set; }
        public DateTime? RegisterData { get; set; }
        public DateTime? DateDelivery { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}