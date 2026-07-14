namespace DeliveryApi.Models
{
    public interface ICalculate
    {
        decimal CalculateTotal();
        decimal CalculateTaxes(); // Para el IVA o comisiones
    }
}