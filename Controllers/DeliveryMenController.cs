using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;
using DeliveryApi.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryMenController : ControllerBase
    {
        private readonly DeliveryDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public DeliveryMenController(DeliveryDbContext context, IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }

        public class VerificationRequest { public string Email { get; set; } = null!; }

        public class VehicleUpdateDto
        {
            public string? TypeVehicle { get; set; }
            public string? LicencePlate { get; set; }
        }

        // POST: api/deliverymen/send-code
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] VerificationRequest req)
        {
            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set(req.Email, code, TimeSpan.FromMinutes(5));
            await _emailService.SendVerificationEmailAsync(req.Email, code);
            return Ok(new { message = "Código de verificación enviado al correo." });
        }

        // POST: api/deliverymen
        [HttpPost]
        public async Task<IActionResult> PostDeliveryMan([FromBody] DeliveryMan deliveryMan, [FromQuery] string code)
        {
            if (!_cache.TryGetValue(deliveryMan.Mail, out string? savedCode) || savedCode != code)
            {
                return BadRequest("El código de verificación es incorrecto o ha expirado.");
            }
            if (!PasswordValidator.EsValida(deliveryMan.Password, out string errorPass))
            {
                return BadRequest(errorPass);
            }
            if (deliveryMan.DateBirth.HasValue)
            {
                var fechaNacimiento = new Fecha(deliveryMan.DateBirth.Value);
                if (!fechaNacimiento.EsMayorDeEdad(18))
                    return BadRequest("Debes ser mayor de 18 años para registrarte como repartidor.");
            }

            deliveryMan.Status = true;
            deliveryMan.Rating = 5.0m;

            try
            {
                _context.DeliveryMen.Add(deliveryMan);
                await _context.SaveChangesAsync();
                _cache.Remove(deliveryMan.Mail);
                return Ok(new { message = "¡Repartidor registrado con éxito!", dni = deliveryMan.Dni });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error en BD: {inner}");
            }
        }

        // 👇 a partir de aquí sigue tu código existente sin ningún cambio:

        // GET: api/deliverymen/available-orders
        [HttpGet("available-orders")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            // Traemos las órdenes libres incluyendo la navegación de Client y de Restaurant
            var orders = await _context.Orders
                .Include(o => o.ClientDniNavigation)
                .Include(o => o.IdRestaurantNavigation) // 🚨 1. AGREGAMOS ESTE INCLUDE AQUÍ
                .Where(o => o.DeliveryDni == null && o.EndTime == 0)
                .ToListAsync();

            // Mapeamos exactamente igual para que el frontend reciba la info antes de tomar el pedido
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
                },
                // 🚨 2. AGREGAMOS EL BLOQUE DEL RESTAURANTE AQUÍ AL FINAL DEL SELECT
                restaurantData = new
                {
                    name = o.IdRestaurantNavigation?.Name ?? "Restaurante",
                    latitude = o.IdRestaurantNavigation?.Latitude,
                    longitude = o.IdRestaurantNavigation?.Longitude
                }
            });

            return Ok(result);
        }

        // PUT: api/deliverymen/accept-order/{idOrder}/{deliveryDni}
        // 🚨 1. MÉTODO PARA TOMAR EL PEDIDO (CON BLINDAJE DE 3 ÓRDENES)
        [HttpPut("accept-order/{idOrder}/{deliveryDni}")]
        public async Task<IActionResult> AcceptOrder(string idOrder, string deliveryDni)
        {
            var activeOrdersCount = await _context.Orders
                .CountAsync(o => o.DeliveryDni == deliveryDni && o.EndTime == 0);

            if (activeOrdersCount >= 3)
                return BadRequest("⛔ Límite alcanzado: Tienes 3 pedidos activos. Debes entregar al menos uno antes de aceptar otro.");

            var order = await _context.Orders.FindAsync(idOrder);
            if (order == null) return NotFound("La orden no existe.");
            if (order.DeliveryDni != null) return BadRequest("Esta orden ya fue tomada por otro repartidor.");

            order.DeliveryDni = deliveryDni;
            order.StartTime = 1; // 🚨 FIX: antes se quedaba en 0 y el pedido no tenía botón de acción

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "¡Pedido asignado con éxito!" });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno de base de datos.");
            }
        }

        // 🚨 2. MÉTODO PARA EL BOTÓN "INICIAR RUTA" (En Camino)
        [HttpPut("dispatch-order/{idOrder}")]
        public async Task<IActionResult> DispatchOrder(string idOrder)
        {
            var order = await _context.Orders
                .Include(o => o.IdRestaurantNavigation)
                .Include(o => o.ClientDniNavigation)
                .FirstOrDefaultAsync(o => o.IdOrder == idOrder);

            if (order == null) return NotFound("La orden no existe.");

            order.StartTime = 2;
            order.DispatchTime = DateTime.Now;
            order.EstimatedTime = CalcularTiempoEstimado(order);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Estado actualizado a En Camino.", estimatedTime = order.EstimatedTime });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar estado.");
            }
        }

        // 🚨 Calcula minutos estimados según distancia real entre restaurante y cliente
        private static int CalcularTiempoEstimado(Order order)
        {
            var tiempoPreparacion = new Time(3);
            const double velocidadPromedioKmH = 22.0;

            var lat1 = order.IdRestaurantNavigation?.Latitude;
            var lon1 = order.IdRestaurantNavigation?.Longitude;
            var lat2 = order.ClientDniNavigation?.AddressData?.Latitude;
            var lon2 = order.ClientDniNavigation?.AddressData?.Longitude;

            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return 15;

            double distanciaKm = DistanciaHaversine((double)lat1, (double)lon1, (double)lat2, (double)lon2);
            var tiempoViaje = Time.FromDistance(distanciaKm, velocidadPromedioKmH);

            var tiempoTotal = tiempoPreparacion.Add(tiempoViaje);
            return tiempoTotal.Minutes;
        }

        private static double DistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double radioTierraKm = 6371;
            double dLat = ToRadianes(lat2 - lat1);
            double dLon = ToRadianes(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadianes(lat1)) * Math.Cos(ToRadianes(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radioTierraKm * c;
        }

        private static double ToRadianes(double grados) => grados * Math.PI / 180;

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
        // GET: api/deliverymen/earnings/{dni}
        [HttpGet("earnings/{dni}")]
        public async Task<IActionResult> GetEarnings(string dni)
        {
            const decimal comisionPorEntrega = 1.50m;

            var entregasCompletadas = await _context.Orders
                .CountAsync(o => o.DeliveryDni == dni && o.EndTime == 1);

            var totalGanado = entregasCompletadas * comisionPorEntrega;

            return Ok(new
            {
                completedDeliveries = entregasCompletadas,
                commissionPerDelivery = comisionPorEntrega,
                totalEarnings = totalGanado
            });
        }

        // PUT: api/deliverymen/update-vehicle/{dni}
        [HttpPut("update-vehicle/{dni}")]
        public async Task<IActionResult> UpdateVehicle(string dni, [FromBody] VehicleUpdateDto data)
        {
            var delivery = await _context.DeliveryMen.FindAsync(dni);
            if (delivery == null) return NotFound("El repartidor no existe.");

            delivery.TypeVehicle = data.TypeVehicle;
            delivery.LicencePlate = data.LicencePlate;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Datos del vehículo actualizados con éxito." });
        }
    }
}