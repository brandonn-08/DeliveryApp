namespace DeliveryApi.Models
{
    public class ExtraIngredientComponent : ComponentProduct
    {
        public ExtraIngredientComponent(ExtraIngredient extra)
        {
            Id = extra.IdIngredient.ToString();
            Name = extra.Name;
            BasePrice = extra.Price;
        }

        public decimal BasePrice { get; set; }

        public override decimal GetPrice() => BasePrice;
    }
}