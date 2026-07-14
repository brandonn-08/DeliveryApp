using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryMenController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public DeliveryMenController(DeliveryDbContext context)
        {
            _context = context;
        }

        // GET: api/deliverymen/available-orders
        // GET: api/deliverymen/available-orders
        [HttpGet("available-orders")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            // Traemos las órdenes libres incluyendo la navegación de ClientDniNavigation
            var orders = await _context.Orders
                .Include(o => o.ClientDniNavigation)
                .Where(o => o.DeliveryDni == null && o.EndTime == 0)
                .ToListAsync();

            // Mapeamos exactamente igual para que el frontend reciba 'clientData' antes de tomar el pedido
            var result = orders.Select(o => new
            {
                idOrder = o.IdOrder,
                clientDni = o.ClientDni,
                deliveryDni = o.DeliveryDni,
                idMethod = o.IdMethod,
                dateOrder = o.DateOrder,
                total = o.Total,
                startTime = o.StartTime,
                endTime = o.EndTime,
                estimatedTime = o.EstimatedTime,
                realTime = o.RealTime,
                clientData = new
                {
                    name = o.ClientDniNavigation?.Name ?? "Usuario Regular",
                    phone = o.ClientDniNavigation?.Phone ?? "Sin Teléfono",
                    // 🚨 CAMBIAR ESTAS LÍNEAS PARA USAR REPOSITORIO DE COMPOSICIÓN:
                    city = o.ClientDniNavigation?.AddressData?.City ?? "Tabacundo",
                    street1 = o.ClientDniNavigation?.AddressData?.Street1 ?? "Dirección Principal",
                    numberHome = o.ClientDniNavigation?.AddressData?.NumberHome ?? "",
                    reference = o.ClientDniNavigation?.AddressData?.Reference ?? ""
                }
            });

            return Ok(result);
        }

        // PUT: api/deliverymen/accept-order/{idOrder}/{deliveryDni}
        [HttpPut("accept-order/{idOrder}/{deliveryDni}")]
        public async Task<IActionResult> AcceptOrder(string idOrder, string deliveryDni)
        {
            var order = await _context.Orders.FindAsync(idOrder);
            if (order == null) return NotFound("La orden no existe.");
            if (order.DeliveryDni != null) return BadRequest("Esta orden ya fue tomada por otro repartidor.");

            order.DeliveryDni = deliveryDni;
            order.StartTime = 1; // Estado: En Cocina
            order.EstimatedTime = 30;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Orden aceptada con éxito." });
        }

        // PUT: api/deliverymen/dispatch-order/{idOrder}
        [HttpPut("dispatch-order/{idOrder}")]
        public async Task<IActionResult> DispatchOrder(string idOrder)
        {
            var order = await _context.Orders.FindAsync(idOrder);
            if (order == null) return NotFound("La orden no existe.");

            order.StartTime = 2; // Estado: En Camino

            await _context.SaveChangesAsync();
            return Ok(new { message = "El pedido ahora está en camino." });
        }

        // PUT: api/deliverymen/complete-order/{idOrder}
        [HttpPut("complete-order/{idOrder}")]
        public async Task<IActionResult> CompleteOrder(string idOrder, [FromQuery] int minutesElapsed)
        {
            var order = await _context.Orders.FindAsync(idOrder);
            if (order == null) return NotFound("La orden no existe.");

            order.EndTime = 1; // Estado: Finalizado
            order.RealTime = minutesElapsed;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Orden entregada con éxito." });
        }
        [HttpGet]
        public async Task<IActionResult> GetAllDeliveryMen()
        {
            var staff = await _context.DeliveryMen.ToListAsync();
            return Ok(staff);
        }
        // PUT: api/deliverymen/rate/{dni}
        [HttpPut("rate/{dni}")]
        public async Task<IActionResult> RateDeliveryMan(string dni, [FromQuery] decimal newRating)
        {
            if (newRating < 1 || newRating > 5) return BadRequest("La calificación debe estar entre 1 y 5 estrellas.");

            var delivery = await _context.DeliveryMen.FindAsync(dni);
            if (delivery == null) return NotFound("El repartidor no existe.");

            // Lógica matemática para promediar la nueva calificación con la existente
            // Nota: En un entorno de producción usarías un histórico de calificaciones, 
            // aquí hacemos un promedio simple directo para cumplir con el esquema.
            delivery.Rating = (delivery.Rating + newRating) / 2;

            await _context.SaveChangesAsync();
            return Ok(new { message = "¡Gracias por calificar al repartidor!", generalRating = delivery.Rating });
        }
        // Verifica que diga exactamente esto en tu DeliveryMenController.cs
        [HttpPut("toggle-status/{dni}")]
        public async Task<IActionResult> ToggleStatus(string dni)
        {
            var delivery = await _context.DeliveryMen.FindAsync(dni);
            if (delivery == null) return NotFound("El repartidor no existe.");

            delivery.Status = !delivery.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado modificado" });
        }
    }
}