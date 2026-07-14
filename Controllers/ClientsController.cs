using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public ClientsController(DeliveryDbContext context)
        {
            _context = context;
        }

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

            if (client.Reference == "BANEADO")
            {
                client.Reference = "Cuenta activa de acceso regular."; // Desbanear
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cliente desbaneado con éxito.", isBanned = false });
            }
            else
            {
                client.Reference = "BANEADO"; // Banear
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cliente suspendido y baneado de la plataforma.", isBanned = true });
            }
        }
    }
}