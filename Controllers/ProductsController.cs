using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public ProductsController(DeliveryDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.IdRestaurantNavigation)
                .Where(p => p.IdProduct != "PROD_DELIVERY")
                .Select(p => new {
                    p.IdProduct,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Stock,
                    p.IdCategory,
                    RestaurantName = p.IdRestaurantNavigation != null ? p.IdRestaurantNavigation.Name : "No Asignado",
                    p.IdRestaurant
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/products/search?query=hamburguesa
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("El término de búsqueda no puede estar vacío.");
            }

            var lowercaseQuery = query.ToLower();

            // Se incluye explícitamente la navegación para evitar excepciones de referencia nula
            var results = await _context.Products
                .Include(p => p.IdRestaurantNavigation)
                .Where(p => p.IdProduct != "PROD_DELIVERY" &&
                    (p.Name.ToLower().Contains(lowercaseQuery) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowercaseQuery)) ||
                    (p.IdRestaurantNavigation != null && p.IdRestaurantNavigation.Name.ToLower().Contains(lowercaseQuery))))
                .Select(p => new {
                    p.IdProduct,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Stock,
                    p.IdCategory,
                    RestaurantName = p.IdRestaurantNavigation != null ? p.IdRestaurantNavigation.Name : "No Asignado",
                    p.IdRestaurant
                })
                .ToListAsync();

            return Ok(results);
        }
        // PUT: api/products/update-stock/{idProduct}
        [HttpPut("update-stock/{idProduct}")]
        public async Task<IActionResult> UpdateStock(string idProduct, [FromQuery] int newStock)
        {
            var product = await _context.Products.FindAsync(idProduct);
            if (product == null) return NotFound("El producto especificado no existe.");

            product.Stock = newStock;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock actualizado correctamente en la matriz." });
        }
        // POST: api/products (Crear nuevo producto)
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product newProduct)
        {
            if (await _context.Products.AnyAsync(p => p.IdProduct == newProduct.IdProduct))
                return BadRequest("El ID del producto ya existe.");

            try
            {
                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Producto registrado exitosamente." });
            }
            catch (Exception ex) { return StatusCode(500, $"Error: {ex.Message}"); }
        }

        // DELETE: api/products/{idProduct} (Eliminar producto)
        [HttpDelete("{idProduct}")]
        public async Task<IActionResult> DeleteProduct(string idProduct)
        {
            var product = await _context.Products.FindAsync(idProduct);
            if (product == null) return NotFound("El producto no existe.");

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Producto eliminado correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, $"Error al eliminar: {ex.Message}"); }
        }
    } // Cierre correcto de la clase ProductsController
} // Cierre correcto del namespace