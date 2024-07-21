using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages.Admin
{
    //[Authorize(Roles = "Admin")]
    public class AdminHomeModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
