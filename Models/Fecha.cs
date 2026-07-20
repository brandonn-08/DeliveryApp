using System;

namespace DeliveryApi.Models
{
    // 🚨 Objeto de valor: encapsula una fecha y las reglas de negocio relacionadas
    public class Fecha
    {
        public DateTime Valor { get; }

        public Fecha(DateTime valor)
        {
            Valor = valor;
        }

        public int CalcularEdad()
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - Valor.Year;
            if (Valor.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        public bool EsMayorDeEdad(int edadMinima = 18) => CalcularEdad() >= edadMinima;

        public string ToStringFormatted() => Valor.ToString("dd/MM/yyyy");
    }
}