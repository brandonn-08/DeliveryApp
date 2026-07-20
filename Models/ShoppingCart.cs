using System.Collections.Generic;
using System.Linq;

namespace DeliveryApi.Models
{
    // 🚨 Encapsula el carrito y valida sus propias reglas de negocio
    public class ShoppingCart
    {
        private readonly List<Product> _products = new();
        private readonly List<int> _quantities = new();

        public void AddItem(Product product, int quantity)
        {
            _products.Add(product);
            _quantities.Add(quantity);
        }

        // 🚨 Regla de negocio: todo el carrito debe ser de la misma sucursal
        public bool EsDeUnaSolaSucursal()
        {
            if (_products.Count == 0) return true;
            int? primeraSucursal = _products[0].IdRestaurant;
            return _products.All(p => p.IdRestaurant == primeraSucursal);
        }

        public int? GetIdRestaurant() => _products.Count > 0 ? _products[0].IdRestaurant : null;
    }
}