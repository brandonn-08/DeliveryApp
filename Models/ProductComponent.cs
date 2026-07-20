namespace DeliveryApi.Models
{
    public class ProductComponent : ComponentProduct
    {
        public ProductComponent(Product product)
        {
            Id = product.IdProduct;
            Name = product.Name;
            Description = product.Description ?? string.Empty;
            BasePrice = product.Price;
        }

        public decimal BasePrice { get; set; }

        public override decimal GetPrice() => BasePrice;
    }
}