
// using Microsoft.AspNetCore.Mvc;
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
    public class ManageInventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageInventoryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public List<Product>
    Products
        { get; set; }

        [BindProperty]
        public Product Product { get; set; }

        public string SuccessMessage { get; set; }

        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; }
        public int TotalProducts { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);

        public string SortField { get; set; }
        public string SortOrder { get; set; }

        public async Task OnGetAsync(string searchTerm, int pageNumber = 1, string sortField = "product_item_num", string sortOrder = "asc")
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            SortField = sortField;
            SortOrder = sortOrder;

            var query = _context.Products.Where(p => !p.is_deleted).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.product_item_num.Contains(searchTerm) || p.product_description.Contains(searchTerm) || (searchTerm == "sep" && p.product_agency_type == 1) ||  // Assuming "1" is SEP
            (searchTerm == "esap" && p.product_agency_type == 2));   // Assuming "2" is ESAP
    
            }

            // Apply sorting
            switch (sortField)
            {
                case "product_agency_type":
                    query = sortOrder == "asc" ? query.OrderBy(p => p.product_agency_type) : query.OrderByDescending(p => p.product_agency_type);
                    break;
                default:
                    query = sortOrder == "asc" ? query.OrderBy(p => p.product_item_num) : query.OrderByDescending(p => p.product_item_num);
                    break;
            }

            TotalProducts = await query.CountAsync();

            Products = await query.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
            CurrentPage = pageNumber;
        }

        public async Task<IActionResult>
            OnPostSaveProductAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Product.product_id == 0)
            {
                _context.Products.Add(Product);
                SuccessMessage = "Product added successfully.";
            }
            else
            {
                var existingProduct = await _context.Products.FindAsync(Product.product_id);
                if (existingProduct != null)
                {
                    existingProduct.product_agency_type = Product.product_agency_type;
                    existingProduct.product_item_num = Product.product_item_num;
                    existingProduct.product_description = Product.product_description;
                    existingProduct.product_pieces_per_case = Product.product_pieces_per_case;
                    existingProduct.product_inventory_level = Product.product_inventory_level;
                    existingProduct.sort_order = Product.sort_order;
                    existingProduct.is_active = Product.is_active;
                    SuccessMessage = "Product updated successfully.";
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = SuccessMessage;
            return RedirectToPage(new { searchTerm = Request.Query["searchTerm"], pageNumber = CurrentPage, sortField = SortField, sortOrder = SortOrder });
        }

        public async Task<IActionResult>
            OnPostDeleteProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.is_deleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully.";
            }

            return RedirectToPage(new { searchTerm = Request.Query["searchTerm"], pageNumber = CurrentPage, sortField = SortField, sortOrder = SortOrder });
        }
    }
}
