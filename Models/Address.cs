using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Models
{
    // Owned significa que no tendrá su propia tabla en la base de datos, 
    // sino que sus propiedades se guardarán en las columnas de la tabla del dueño (clients).
    [Owned]
    public class Address
    {
        public string? City { get; set; }
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? PostalCode { get; set; }
        public string? NumberHome { get; set; }
        public string? Reference { get; set; }
    }
}