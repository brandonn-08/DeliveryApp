using System;

namespace DeliveryApi.Models
{
    //clase abstracta implementada
    public abstract class Person
    {
        public string Dni { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Mail { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }
        public DateTime? DateBirth { get; set; }
    }
}