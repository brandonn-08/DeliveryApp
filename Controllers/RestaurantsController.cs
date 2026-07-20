using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public RestaurantsController(DeliveryDbContext context)
        {
            _context = context;
        }

        // GET: api/restaurants
        [HttpGet]
        public async Task<IActionResult> GetRestaurants()
        {
            var stores = await _context.Restaurants.ToListAsync();
            return Ok(stores);
        }

        // POST: api/restaurants (Agregar sucursal)
        [HttpPost]
        public async Task<IActionResult> CreateRestaurant([FromBody] Restaurant newStore)
        {
            try
            {
                _context.Restaurants.Add(newStore);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Sucursal registrada con éxito." });
            }
            catch (Exception ex) { return StatusCode(500, $"Error: {ex.Message}"); }
        }

        // DELETE: api/restaurants/{id} (Eliminar sucursal)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRestaurant(int id)
        {
            var store = await _context.Restaurants.FindAsync(id);
            if (store == null) return NotFound("La sucursal no existe.");

            try
            {
                _context.Restaurants.Remove(store);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Sucursal eliminada correctamente." });
            }
            catch (Exception ex) { return StatusCode(500, $"Error al eliminar: {ex.Message}"); }
        }
        // GET: api/restaurants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRestaurantById(int id)
        {
            var store = await _context.Restaurants.FindAsync(id);
            if (store == null) return NotFound("La sucursal no existe.");
            return Ok(store);
        }
        // PUT: api/restaurants/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRestaurant(int id, [FromBody] Restaurant restaurantActualizado)
        {
            var restaurantDb = await _context.Restaurants.FindAsync(id);
            if (restaurantDb == null) return NotFound("La sucursal no existe.");

            restaurantDb.Name = restaurantActualizado.Name;
            restaurantDb.Address = restaurantActualizado.Address;
            restaurantDb.BankAccountDetails = restaurantActualizado.BankAccountDetails;
            if (restaurantActualizado.Latitude.HasValue) restaurantDb.Latitude = restaurantActualizado.Latitude;
            if (restaurantActualizado.Longitude.HasValue) restaurantDb.Longitude = restaurantActualizado.Longitude;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Sucursal actualizada con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar: {ex.Message}");
            }
        }
    }
}