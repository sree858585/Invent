using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Client
{
    public class CheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CheckoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ShipInformation ShipInformation { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.Identity.Name; // Assuming user ID is the username

            // Get the shipping information related to the user's registration
            ShipInformation = await _context.ShipInformations
                .Include(s => s.AgencyRegistration)
                .FirstOrDefaultAsync(s => s.AgencyRegistration.UserId == userId);

            if (ShipInformation == null)
            {
                return NotFound("Shipping information not found.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(ShipInformation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShipInformationExists(ShipInformation.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Client/OrderConfirmation");
        }

        private bool ShipInformationExists(int id)
        {
            return _context.ShipInformations.Any(e => e.Id == id);
        }
    }
}
