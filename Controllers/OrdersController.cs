using System.Collections.Generic;
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
            var clientExists = await _context.Clients.AnyAsync(c => c.Dni == request.ClientDni);
            if (!clientExists) return BadRequest("El DNI del cliente no está registrado.");

            // 🚨 CARRITO: cargamos los productos y validamos la regla de una sola sucursal en el backend
            var carrito = new ShoppingCart();
            var productosCache = new Dictionary<string, Product>();

            foreach (var itemDto in request.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.IdProduct);
                if (product == null) return NotFound($"El producto {itemDto.IdProduct} no existe.");
                if (product.Stock < itemDto.Quantity) return BadRequest($"Stock insuficiente para: {product.Name}");

                productosCache[itemDto.IdProduct] = product;
                carrito.AddItem(product, itemDto.Quantity);
            }

            if (!carrito.EsDeUnaSolaSucursal())
                return BadRequest("Todos los productos de un mismo pedido deben ser de la misma sucursal.");

            var orderItemsToSave = new List<OrderItem>();
            var itemExtrasToSave = new List<ItemExtra>();

            foreach (var itemDto in request.Items)
            {
                var product = productosCache[itemDto.IdProduct];
                product.Stock -= itemDto.Quantity;

                // 🚨 COMPOSITE: el precio del ítem sale de sumar producto + extras
                var productoConExtras = new ProductWithExtras(product);

                var orderItem = new OrderItem
                {
                    IdOrder = request.IdOrder,
                    IdProduct = itemDto.IdProduct,
                    Quantity = itemDto.Quantity,
                    Subtotal = 0
                };

                foreach (var extraId in itemDto.ExtraIngredientIds)
                {
                    var extra = await _context.ExtraIngredients.FindAsync(extraId);
                    if (extra != null)
                    {
                        productoConExtras.AddExtra(extra);
                        itemExtrasToSave.Add(new ItemExtra
                        {
                            IdItemNavigation = orderItem,
                            IdIngredient = extra.IdIngredient,
                            ExtraPrice = extra.Price
                        });
                    }
                }

                orderItem.Subtotal = productoConExtras.GetPrice() * itemDto.Quantity;
                orderItemsToSave.Add(orderItem);
            }

            var newOrder = new Order
            {
                IdOrder = request.IdOrder,
                ClientDni = request.ClientDni,
                IdMethod = request.IdMethod,
                DeliveryDni = null,
                DateOrder = DateTime.Now,
                StartTime = 0,
                EndTime = 0,
                EstimatedTime = 0,
                RealTime = 0,
                IdRestaurant = carrito.GetIdRestaurant(),
                DispatchTime = null,
                OrderItems = orderItemsToSave
            };

            // 🚨 ICalculate: comisión de envío y total final, ya no son variables sueltas
            decimal costoEnvio = newOrder.CalculateTaxes();
            orderItemsToSave.Add(new OrderItem
            {
                IdOrder = request.IdOrder,
                IdProduct = "PROD_DELIVERY",
                Quantity = 1,
                Subtotal = costoEnvio
            });

            newOrder.Total = newOrder.CalculateTotal();

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
            .Include(o => o.IdRestaurantNavigation)
            .Include(o => o.DeliveryDniNavigation)   // 🚨 LÍNEA NUEVA
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
                dispatchTime = o.DispatchTime,
                clientData = new
                {
                    name = o.ClientDniNavigation?.Name ?? "Usuario Regular",
                    phone = o.ClientDniNavigation?.Phone ?? "Sin Teléfono",
                    city = o.ClientDniNavigation?.AddressData?.City ?? "Tabacundo",
                    street1 = o.ClientDniNavigation?.AddressData?.Street1 ?? "Dirección Principal",
                    numberHome = o.ClientDniNavigation?.AddressData?.NumberHome ?? "",
                    reference = o.ClientDniNavigation?.AddressData?.Reference ?? "",
                    latitude = o.ClientDniNavigation?.AddressData?.Latitude,
                    longitude = o.ClientDniNavigation?.AddressData?.Longitude
                },
                restaurantData = o.IdRestaurantNavigation != null ? new
                {
                    name = o.IdRestaurantNavigation.Name,
                    latitude = o.IdRestaurantNavigation.Latitude,
                    longitude = o.IdRestaurantNavigation.Longitude
                } : null,
                deliveryData = o.DeliveryDniNavigation != null ? new
                {
                    name = o.DeliveryDniNavigation.Name,
                    phone = o.DeliveryDniNavigation.Phone,
                    typeVehicle = o.DeliveryDniNavigation.TypeVehicle,
                    licencePlate = o.DeliveryDniNavigation.LicencePlate,
                    rating = o.DeliveryDniNavigation.Rating
                } : null
            });

            return Ok(result);
        }
        // Lógica para asignar un pedido al delivery
        [HttpPut("assign/{idOrder}/{deliveryDni}")]
        public async Task<IActionResult> AssignOrderToDelivery(string idOrder, string deliveryDni)
        {
            // 🚨 REGLA DE ROBUSTEZ: Verificar si el repartidor ya tiene 3 o más pedidos activos
            var activeOrdersCount = await _context.Orders
                .CountAsync(o => o.DeliveryDni == deliveryDni && o.EndTime == 0); // EndTime 0 significa que aún no lo entrega

            if (activeOrdersCount >= 3)
            {
                return BadRequest("⛔ Límite alcanzado: El sistema se saturaría. Debes finalizar la entrega de al menos un pedido antes de aceptar otro.");
            }

            // Buscamos la orden
            var order = await _context.Orders.FindAsync(idOrder);
            if (order == null) return NotFound("La orden no existe.");
            if (order.DeliveryDni != null) return BadRequest("Esta orden ya fue tomada por otro repartidor.");

            // Asignamos el repartidor y cambiamos el estado a "En camino" (StartTime = 2)
            order.DeliveryDni = deliveryDni;
            order.StartTime = 2;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "¡Pedido asignado con éxito! Tienes " + (activeOrdersCount + 1) + " pedidos activos." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error de base de datos.");
            }
        }
    }
}