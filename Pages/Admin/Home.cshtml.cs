using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize(Roles = "Admin")]
public class AdminIndexModel : PageModel
{
    public void OnGet()
    {
    }
}
