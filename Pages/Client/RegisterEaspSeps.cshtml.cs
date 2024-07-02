using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

public class RegisterEaspSepsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    [BindProperty]
    public EaspSepsRegistration EaspSepsRegistration { get; set; }

    [BindProperty]
    public AgencyContact AgencyContact { get; set; }

    [BindProperty]
    public List<AdditionalUser> AdditionalUsers { get; set; } = new List<AdditionalUser>();

    [BindProperty]
    public ShipInformation ShipInformation { get; set; }

    [BindProperty]
    public List<int> CountiesServed { get; set; } = new List<int>();

    public List<SelectListItem> CountyList { get; set; }

    public string SuccessMessage { get; set; }

    public RegisterEaspSepsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CountyList = await _context.Counties
            .Where(c => c.Is_Active)
            .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            CountyList = await _context.Counties
                .Where(c => c.Is_Active)
                .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
                .ToListAsync();
            //return Page();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
        EaspSepsRegistration.UserId = userId;

        _context.EaspSepsRegistrations.Add(EaspSepsRegistration);
        await _context.SaveChangesAsync();

        AgencyContact.EaspSepsRegistrationId = EaspSepsRegistration.Id;
        _context.AgencyContacts.Add(AgencyContact);
        await _context.SaveChangesAsync();

        foreach (var user in AdditionalUsers)
        {
            user.EaspSepsRegistrationId = EaspSepsRegistration.Id;
            _context.AdditionalUsers.Add(user);
        }
        await _context.SaveChangesAsync();

        ShipInformation.EaspSepsRegistrationId = EaspSepsRegistration.Id;
        _context.ShipInformations.Add(ShipInformation);
        await _context.SaveChangesAsync();

        foreach (var countyId in CountiesServed)
        {
            _context.EaspSepsRegistrationCounties.Add(new EaspSepsRegistrationCounty
            {
                EaspSepsRegistrationId = EaspSepsRegistration.Id,
                CountyId = countyId
            });
        }

        await _context.SaveChangesAsync();

        SuccessMessage = "Registration successful!";
        return RedirectToPage();
    }
}
