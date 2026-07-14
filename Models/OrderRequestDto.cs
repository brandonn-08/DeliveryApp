namespace DeliveryApi.Models
{
    public class OrderRequestDto
    {
        public string IdOrder { get; set; } = null!;
        public string ClientDni { get; set; } = null!;
        public string IdMethod { get; set; } = null!;
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public string IdProduct { get; set; } = null!;
        public int Quantity { get; set; }
        // Aquí viene el patrón Decorator: una lista con los IDs de los ingredientes extras elegidos
        public List<int> ExtraIngredientIds { get; set; } = new();
    }
}