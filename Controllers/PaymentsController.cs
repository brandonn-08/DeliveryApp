using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly DeliveryDbContext _context;

        public PaymentsController(DeliveryDbContext context)
        {
            _context = context;
        }

        // GET: api/payments/methods
        [HttpGet("methods")]
        public async Task<IActionResult> GetPayMethods()
        {
            var methods = await _context.PayMethods.ToListAsync();
            return Ok(methods);
        }

        // GET: api/payments/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetPaymentsSummary()
        {
            var orders = await _context.Orders
                .Include(o => o.IdMethodNavigation)
                .ToListAsync();

            var resumen = orders
                .GroupBy(o => o.IdMethodNavigation != null ? o.IdMethodNavigation.Type : "Desconocido")
                .Select(g => new
                {
                    method = g.Key,
                    totalOrders = g.Count(),
                    totalCollected = g.Sum(o => o.Total)
                });

            return Ok(resumen);
        }
    }
}