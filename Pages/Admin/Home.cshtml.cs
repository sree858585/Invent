using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages.Admin
{
    public class HomeModel : PageModel
    {
        [TempData]
        public string Successmessage { get; set; }
        public void OnGet()
        {
        }
    }
}
