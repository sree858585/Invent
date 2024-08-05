using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin
{
    public class DeletedProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeletedProductsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Product> DeletedProducts { get; set; }

        public async Task OnGetAsync()
        {
            DeletedProducts = await _context.Products
                                            .IgnoreQueryFilters()
                                            .Where(p => p.is_deleted)
                                            .ToListAsync();
        }

        public async Task<IActionResult> OnPostActivateProductAsync(int productId)
        {
            var product = await _context.Products
                                        .IgnoreQueryFilters()
                                        .FirstOrDefaultAsync(p => p.product_id == productId);

            if (product != null)
            {
                product.is_deleted = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product activated successfully.";
            }

            return RedirectToPage();
        }
    }
}
