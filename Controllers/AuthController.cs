using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public AuthController(DeliveryDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // 1. Buscar en la tabla de Administradores
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Mail == request.Email && a.Password == request.Password);

            if (admin != null)
            {
                return Ok(new LoginResponseDto
                {
                    Token = "Simulated-JWT-Token-Admin",
                    Role = "admin",
                    Name = admin.Name,
                    Identifier = admin.Dni
                });
            }

            // 2. Buscar en la tabla de Repartidores (DeliveryMen)
            var delivery = await _context.DeliveryMen
                .FirstOrDefaultAsync(d => d.Mail == request.Email && d.Password == request.Password);

            if (delivery != null)
            {
                // 🚨 CORRECCIÓN CS0266: Comprobación segura para booleanos mapeados como Nullables (bool?)
                if (delivery.Status == false)
                {
                    return BadRequest("Your delivery driver account has been suspended by the administration.");

                }

                return Ok(new LoginResponseDto
                {
                    Token = "Simulated-JWT-Token-Delivery",
                    Role = "delivery",
                    Name = delivery.Name,
                    Identifier = delivery.Dni // 🚨 CORRECCIÓN CS0103: Cambiado 'd.Dni' por 'delivery.Dni'
                });
            }

            // 3. Buscar en la tabla de Clientes
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Mail == request.Email && c.Password == request.Password);

            if (client != null)
            {
                if (client.AddressData.Reference == "BANEADO")
                {
                    return BadRequest("Your account has been temporarily suspended by the administration.");
                }

                return Ok(new LoginResponseDto
                {
                    Token = "Simulated-JWT-Token-Client",
                    Role = "client",
                    Name = client.Name,
                    Identifier = client.Dni,
                    Street1 = client.AddressData.Street1,
                    Street2 = client.AddressData.Street2,
                    Reference = client.AddressData.Reference,
                    Latitude = client.AddressData.Latitude,
                    Longitude = client.AddressData.Longitude
                });
            }

            return Unauthorized(new { message = "Credenciales incorrectas o usuario no registrado." });
        }
    }
}