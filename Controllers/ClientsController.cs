using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;
using DeliveryApi.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public ClientsController(DeliveryDbContext context, IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }

        public class VerificationRequest { public string Email { get; set; } = null!; }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] VerificationRequest req)
        {
            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set(req.Email, code, TimeSpan.FromMinutes(5));
            await _emailService.SendVerificationEmailAsync(req.Email, code);
            return Ok(new { message = "Código de verificación enviado al correo." });
        }

        [HttpPost]
        public async Task<IActionResult> PostClient([FromBody] Client client, [FromQuery] string code)
        {
            if (!_cache.TryGetValue(client.Mail, out string? savedCode) || savedCode != code)
            {
                return BadRequest("El código de verificación es incorrecto o ha expirado.");
            }
            if (!PasswordValidator.EsValida(client.Password, out string errorPass))
            {
                return BadRequest(errorPass);
            }
            // 🚨 RESPALDO EN BACKEND: la edad no debe confiarse solo al frontend
            if (client.DateBirth.HasValue)
            {
                var fechaNacimiento = new Fecha(client.DateBirth.Value);
                if (!fechaNacimiento.EsMayorDeEdad(18))
                    return BadRequest("Debes ser mayor de 18 años para registrarte.");
            }

            try
            {
                // ... el resto del método sigue exactamente igual
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                _cache.Remove(client.Mail);
                return Ok(new { message = "¡Cliente registrado con éxito!", dni = client.Dni });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error en BD: {inner}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClients()
        {
            var users = await _context.Clients.ToListAsync();
            return Ok(users);
        }

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
                client.AddressData.Reference = "BANEADO";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cliente suspendido y baneado de la plataforma.", isBanned = true });
            }
        }

        // 🚨 1. CLASES DTO PARA FILTRAR LOS DATOS DE ENTRADA
        public class PerfilUpdateDto
        {
            public string Name { get; set; } = null!;
            public string? Phone { get; set; }
            public DireccionUpdateDto? AddressData { get; set; }
        }

        public class DireccionUpdateDto
        {
            public string? Street1 { get; set; }
            public string? Reference { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
        }

        // 🚨 2. EL MÉTODO PUT QUE ESTABA FALTANDO
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(string id, [FromBody] PerfilUpdateDto perfilActualizado)
        {
            // 1. Buscamos al cliente original
            var clienteDb = await _context.Clients.FindAsync(id);
            if (clienteDb == null)
            {
                return NotFound("Cliente no encontrado en la base de datos.");
            }

            // 2. Actualizamos solo los campos permitidos
            clienteDb.Name = perfilActualizado.Name;
            clienteDb.Phone = perfilActualizado.Phone;

            if (perfilActualizado.AddressData != null && clienteDb.AddressData != null)
            {
                clienteDb.AddressData.Street1 = perfilActualizado.AddressData.Street1;
                clienteDb.AddressData.Reference = perfilActualizado.AddressData.Reference;
                if (perfilActualizado.AddressData.Latitude.HasValue)
                    clienteDb.AddressData.Latitude = perfilActualizado.AddressData.Latitude;
                if (perfilActualizado.AddressData.Longitude.HasValue)
                    clienteDb.AddressData.Longitude = perfilActualizado.AddressData.Longitude;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "¡Perfil actualizado exitosamente!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error en el servidor al intentar guardar: {ex.Message}");
            }
        }
        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientById(string id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound("El cliente no existe.");
            return Ok(client);
        }   
    }
}