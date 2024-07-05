using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class EaspSepsRegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EaspSepsRegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var registration = await _context.EaspSepsRegistrations
                .Include(r => r.ShipToSites)
                .ThenInclude(s => s.PrimaryCountiesServed)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration != null)
            {
                foreach (var site in registration.ShipToSites)
                {
                    _context.ShipToSiteCounties.RemoveRange(site.PrimaryCountiesServed);
                    _context.ShipToSites.Remove(site);
                }

                _context.EaspSepsRegistrations.Remove(registration);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index"); // or wherever you want to redirect after deletion
        }
    }
}
