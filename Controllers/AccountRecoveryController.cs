using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DeliveryApi.Models;
using DeliveryApi.Services;

namespace DeliveryApi.Controllers
{
    // 🚨 Controlador único para Recover Username / Recover Password / Unlock Account
    // Sirve a los 3 roles: client, delivery, admin
    [Route("api/[controller]")]
    [ApiController]
    public class AccountRecoveryController : ControllerBase
    {
        private readonly DeliveryDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AccountRecoveryController(DeliveryDbContext context, IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }

        public class EmailRequest
        {
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!; // "client", "delivery", "admin"
        }

        public class ResetPasswordRequest
        {
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
            public string Code { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
        }

        public class UnlockRequest
        {
            public string Email { get; set; } = null!;
            public string Role { get; set; } = null!;
            public string Code { get; set; } = null!;
        }

        // 🚨 Busca el "Person" según el rol. Devuelve null si no existe.
        private async Task<(string Dni, string Name, string Password)?> BuscarUsuario(string email, string role)
        {
            switch (role)
            {
                case "client":
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.Mail == email);
                    return client == null ? null : (client.Dni, client.Name, client.Password);

                case "delivery":
                    var delivery = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.Mail == email);
                    return delivery == null ? null : (delivery.Dni, delivery.Name, delivery.Password);

                case "admin":
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Mail == email);
                    return admin == null ? null : (admin.Dni, admin.Name, admin.Password);

                default:
                    return null;
            }
        }

        // POST: api/accountrecovery/recover-username
        [HttpPost("recover-username")]
        public async Task<IActionResult> RecoverUsername([FromBody] EmailRequest req)
        {
            var usuario = await BuscarUsuario(req.Email, req.Role);
            if (usuario == null)
                return NotFound("No se encontró ninguna cuenta con ese correo en el rol indicado.");

            await _emailService.SendUsernameRecoveryEmailAsync(req.Email, usuario.Value.Name, usuario.Value.Dni);
            return Ok(new { message = "Te hemos enviado tu identificador (DNI) al correo registrado." });
        }

        // POST: api/accountrecovery/send-reset-code
        [HttpPost("send-reset-code")]
        public async Task<IActionResult> SendResetCode([FromBody] EmailRequest req)
        {
            var usuario = await BuscarUsuario(req.Email, req.Role);
            if (usuario == null)
                return NotFound("No se encontró ninguna cuenta con ese correo en el rol indicado.");

            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"reset_{req.Role}_{req.Email}", code, TimeSpan.FromMinutes(5));
            await _emailService.SendVerificationEmailAsync(req.Email, code);

            return Ok(new { message = "Código de verificación enviado al correo." });
        }

        // POST: api/accountrecovery/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var cacheKey = $"reset_{req.Role}_{req.Email}";
            if (!_cache.TryGetValue(cacheKey, out string? savedCode) || savedCode != req.Code)
                return BadRequest("El código de verificación es incorrecto o ha expirado.");

            if (!PasswordValidator.EsValida(req.NewPassword, out string errorPass))
                return BadRequest(errorPass);

            switch (req.Role)
            {
                case "client":
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.Mail == req.Email);
                    if (client == null) return NotFound("Cuenta no encontrada.");
                    client.Password = req.NewPassword;
                    break;

                case "delivery":
                    var delivery = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.Mail == req.Email);
                    if (delivery == null) return NotFound("Cuenta no encontrada.");
                    delivery.Password = req.NewPassword;
                    break;

                case "admin":
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Mail == req.Email);
                    if (admin == null) return NotFound("Cuenta no encontrada.");
                    admin.Password = req.NewPassword;
                    break;

                default:
                    return BadRequest("Rol inválido.");
            }

            _cache.Remove(cacheKey);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Contraseña actualizada con éxito. Ya puedes iniciar sesión." });
        }

        // POST: api/accountrecovery/send-unlock-code
        [HttpPost("send-unlock-code")]
        public async Task<IActionResult> SendUnlockCode([FromBody] EmailRequest req)
        {
            var usuario = await BuscarUsuario(req.Email, req.Role);
            if (usuario == null)
                return NotFound("No se encontró ninguna cuenta con ese correo en el rol indicado.");

            var code = new Random().Next(100000, 999999).ToString();
            _cache.Set($"unlock_{req.Role}_{req.Email}", code, TimeSpan.FromMinutes(5));
            await _emailService.SendVerificationEmailAsync(req.Email, code);

            return Ok(new { message = "Código de desbloqueo enviado al correo." });
        }

        // POST: api/accountrecovery/unlock-account
        [HttpPost("unlock-account")]
        public async Task<IActionResult> UnlockAccount([FromBody] UnlockRequest req)
        {
            var cacheKey = $"unlock_{req.Role}_{req.Email}";
            if (!_cache.TryGetValue(cacheKey, out string? savedCode) || savedCode != req.Code)
                return BadRequest("El código de verificación es incorrecto o ha expirado.");

            switch (req.Role)
            {
                case "client":
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.Mail == req.Email);
                    if (client == null) return NotFound("Cuenta no encontrada.");
                    if (client.AddressData.Reference == "BANEADO")
                        client.AddressData.Reference = "Cuenta activa de acceso regular.";
                    break;

                case "delivery":
                    var delivery = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.Mail == req.Email);
                    if (delivery == null) return NotFound("Cuenta no encontrada.");
                    delivery.Status = true;
                    break;

                case "admin":
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Mail == req.Email);
                    if (admin == null) return NotFound("Cuenta no encontrada.");
                    admin.Status = true;
                    break;

                default:
                    return BadRequest("Rol inválido.");
            }

            _cache.Remove(cacheKey);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cuenta desbloqueada con éxito. Ya puedes iniciar sesión." });
        }
    }
}