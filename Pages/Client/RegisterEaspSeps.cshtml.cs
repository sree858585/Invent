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
    public List<SelectListItem> SuffixList { get; set; }

    public string SuccessMessage { get; set; }

    public RegisterEaspSepsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CountyList = await _context.Counties
            .Where(c => c.is_active)
            .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
            .ToListAsync();

        SuffixList = await _context.Suffixes
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.ID.ToString(), Text = s.Sufix })
            .ToListAsync();

        CountiesServed = Enumerable.Repeat(0, 10).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            CountyList = await _context.Counties
                .Where(c => c.is_active)
                .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
                .ToListAsync();

            SuffixList = await _context.Suffixes
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.ID.ToString(), Text = s.Sufix })
                .ToListAsync();

           // return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError(string.Empty, "User is not logged in");
            return Page();
        }

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

        foreach (var countyId in CountiesServed.Distinct())
        {
            if (countyId > 0)
            {
                var entity = new EaspSepsRegistrationCounty
                {
                    EaspSepsRegistrationId = EaspSepsRegistration.Id,
                    CountyId = countyId
                };

                var trackedEntity = _context.EaspSepsRegistrationCounties.Local.FirstOrDefault(e =>
                    e.EaspSepsRegistrationId == entity.EaspSepsRegistrationId && e.CountyId == entity.CountyId);

                if (trackedEntity != null)
                {
                    _context.Entry(trackedEntity).State = EntityState.Detached;
                }

                _context.EaspSepsRegistrationCounties.Add(entity);
            }
        }

        await _context.SaveChangesAsync();

        SuccessMessage = "Registration successful!";
        return RedirectToPage("/Index");
    }
}
