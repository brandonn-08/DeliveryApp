namespace DeliveryApi.Models
{
    public abstract class ComponentProduct
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;

        // Método abstracto que cada producto o combo calculará a su manera
        public abstract decimal GetPrice();
    }
}