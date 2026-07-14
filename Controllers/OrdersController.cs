using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public OrdersController(DeliveryDbContext context)
        {
            _context = context;
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request)
        {
            // 1. Validar que el cliente exista
            var clientExists = await _context.Clients.AnyAsync(c => c.Dni == request.ClientDni);
            if (!clientExists) return BadRequest("El DNI del cliente no está registrado.");

            // 2. Inicializar variables usando DECIMAL para precisión monetaria
            decimal orderTotal = 0;
            var orderItemsToSave = new List<OrderItem>();
            var itemExtrasToSave = new List<ItemExtra>();

            // 3. Procesar cada producto del carrito
            foreach (var itemDto in request.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.IdProduct);
                if (product == null) return NotFound($"El producto {itemDto.IdProduct} no existe.");
                if (product.Stock < itemDto.Quantity) return BadRequest($"Stock insuficiente para: {product.Name}");

                // Restar stock
                product.Stock -= itemDto.Quantity;

                // Calcular Subtotal Base (Cantidad * Precio Producto)
                decimal itemSubtotal = product.Price * itemDto.Quantity;

                // Instanciar el ítem del detalle
                var orderItem = new OrderItem
                {
                    IdOrder = request.IdOrder,
                    IdProduct = itemDto.IdProduct,
                    Quantity = itemDto.Quantity,
                    Subtotal = itemSubtotal // Se actualizará si tiene extras
                };

                orderItemsToSave.Add(orderItem);

                // === LOGICA DECORATOR: Procesar ingredientes extras ===
                foreach (var extraId in itemDto.ExtraIngredientIds)
                {
                    var extra = await _context.ExtraIngredients.FindAsync(extraId);
                    if (extra != null)
                    {
                        // Multiplicamos el precio del extra por la cantidad de productos pedidos
                        decimal extraCost = extra.Price * itemDto.Quantity;
                        itemSubtotal += extraCost;

                        // Guardamos la relación en la tabla intermedia
                        var itemExtra = new ItemExtra
                        {
                            IdItemNavigation = orderItem,
                            IdIngredient = extra.IdIngredient,
                            ExtraPrice = extra.Price
                        };
                        itemExtrasToSave.Add(itemExtra);
                    }
                }

                // Actualizar el subtotal final del ítem ya decorado
                orderItem.Subtotal = itemSubtotal;
                orderTotal += itemSubtotal;
            }

            // =========================================================================
            // 🚨 LOGICA DE COMISIÓN SEGURA: AGREGAR COSTO DE ENVÍO AUTOMÁTICO (DELIVERY)
            // =========================================================================
            decimal costoEnvio = 1.50m;

            var deliveryItem = new OrderItem
            {
                IdOrder = request.IdOrder,
                IdProduct = "PROD_DELIVERY", // Busca el ID del servicio de envío insertado por SQL
                Quantity = 1,
                Subtotal = costoEnvio
            };

            // Añadimos el envío a la lista de detalles y sumamos al total del pedido
            orderItemsToSave.Add(deliveryItem);
            orderTotal += costoEnvio;
            // =========================================================================

            // 4. ASIGNACIÓN AUTOMÁTICA DE REPARTIDOR Y CABECERA DE ORDEN
            var availableDelivery = await _context.DeliveryMen.FirstOrDefaultAsync();
            string assignedDeliveryDni = availableDelivery != null ? availableDelivery.Dni : "1798765432";

            var newOrder = new Order
            {
                IdOrder = request.IdOrder,
                ClientDni = request.ClientDni,
                IdMethod = request.IdMethod,
                DeliveryDni = null, // Nace libre para el pool de transportistas
                Total = orderTotal, // El total ya incluye los $1.50 del envío
                DateOrder = DateTime.Now,
                StartTime = 0,      // 0 significa: Registrada / Disponible en el pool
                EndTime = 0,
                EstimatedTime = 0,
                RealTime = 0
            };

            // 5. Guardar todo transaccionalmente
            try
            {
                _context.Orders.Add(newOrder);
                _context.OrderItems.AddRange(orderItemsToSave);
                _context.ItemExtras.AddRange(itemExtrasToSave);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al procesar el pedido: {ex.Message}");
            }

            return Ok(new { message = "¡Pedido creado con éxito!", orderId = newOrder.IdOrder, total = newOrder.Total });
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            // Traemos las órdenes incluyendo la navegación del Cliente
            var orders = await _context.Orders
                .Include(o => o.ClientDniNavigation)
                .ToListAsync();

            // Mapeamos a un objeto plano (DTO anónimo) para enviar solo lo que el frontend necesita
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
                // Inyectamos los datos del cliente de forma plana para JavaScript
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
    }
}