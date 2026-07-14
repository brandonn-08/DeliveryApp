using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;
using DeliveryApi.Services; // Importamos el servicio de correos
using Microsoft.Extensions.Caching.Memory; // Importamos la memoria RAM

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        // 🚨 Inyectamos la Base de Datos, la Memoria Caché y el Servicio de Correo
        public ClientsController(DeliveryDbContext context, IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }

        // DTO temporal para recibir solo el correo al pedir el código
        public class VerificationRequest { public string Email { get; set; } = null!; }

        // 🚨 NUEVO ENDPOINT: Genera y envía el código al Gmail
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] VerificationRequest req)
        {
            // Generamos un código aleatorio de 6 dígitos
            var code = new Random().Next(100000, 999999).ToString();

            // Guardamos el código en la memoria RAM asociado a ese correo, expira en 5 minutos
            _cache.Set(req.Email, code, TimeSpan.FromMinutes(5));

            // Enviamos el correo usando el EmailService que creamos
            await _emailService.SendVerificationEmailAsync(req.Email, code);

            return Ok(new { message = "Código de verificación enviado al correo." });
        }

        // 🚨 ENDPOINT ACTUALIZADO: Registra al cliente validando el código
        // Nota: Añadimos un parámetro [FromQuery] para recibir el código en la URL
        [HttpPost]
        public async Task<IActionResult> PostClient([FromBody] Client client, [FromQuery] string code)
        {
            // 1. Verificamos si el código existe en la memoria y coincide
            if (!_cache.TryGetValue(client.Mail, out string? savedCode) || savedCode != code)
            {
                return BadRequest("El código de verificación es incorrecto o ha expirado.");
            }

            // 2. Guardamos al cliente en PostgreSQL si el código fue correcto
            try
            {
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                // Borramos el código de la memoria por seguridad
                _cache.Remove(client.Mail);

                return Ok(new { message = "¡Cliente registrado con éxito!", dni = client.Dni });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error en BD: {inner}");
            }
        }

        // ... Aquí debajo mantienes tu GetClients, PutClient, etc.
        // GET: api/clients (Listar todos los clientes en el panel Admin)
        [HttpGet]
        public async Task<IActionResult> GetAllClients()
        {
            var users = await _context.Clients.ToListAsync();
            return Ok(users);
        }

        // PUT: api/clients/toggle-ban/{dni} (Banear / Desbanear usando la columna reference)
        [HttpPut("toggle-ban/{dni}")]
        public async Task<IActionResult> ToggleBanClient(string dni)
        {
            var client = await _context.Clients.FindAsync(dni);
            if (client == null) return NotFound("El cliente no existe.");

            if (client.AddressData.Reference == "BANEADO")
            {
                client.AddressData.Reference = "Cuenta activa de acceso regular.";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cliente desbaneado con éxito.", isBanned = false });
            }
            else
            {
                client.AddressData.Reference = "BANEADO";// Banear
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cliente suspendido y baneado de la plataforma.", isBanned = true });
            }
        }
    }
}