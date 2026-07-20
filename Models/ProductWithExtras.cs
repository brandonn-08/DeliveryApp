using System.Collections.Generic;
using System.Linq;

namespace DeliveryApi.Models
{
    // 🚨 Patrón Composite: un producto + sus extras, todos ComponentProduct, se suman igual
    public class ProductWithExtras : ComponentProduct
    {
        private readonly List<ComponentProduct> _components = new();

        public ProductWithExtras(Product product)
        {
            Id = product.IdProduct;
            Name = product.Name;
            Description = product.Description ?? string.Empty;
            _components.Add(new ProductComponent(product));
        }

        public void AddExtra(ExtraIngredient extra)
        {
            _components.Add(new ExtraIngredientComponent(extra));
        }

        public override decimal GetPrice() => _components.Sum(c => c.GetPrice());
    }
}