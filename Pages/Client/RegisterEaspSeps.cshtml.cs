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
    public List<AdditionalUser> AdditionalUsers { get; set; } = new List<AdditionalUser> { new AdditionalUser() };

    [BindProperty]
    public ShipInformation ShipInformation { get; set; }

    [BindProperty]
    public List<ShipToSite> AdditionalShipToSites { get; set; } = new List<ShipToSite>();

    [BindProperty]
    public List<int> CountiesServed { get; set; } = new List<int>();

    [BindProperty]
    public List<int[]> ShipToSiteCounties { get; set; } = new List<int[]>();

    public List<SelectListItem> CountyList { get; set; }
    public List<SelectListItem> SuffixList { get; set; }
    public List<SelectListItem> PrefixList { get; set; }

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

        PrefixList = await _context.Prefixes
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.ID.ToString(), Text = s.Prefx })
            .ToListAsync();

        CountiesServed = Enumerable.Repeat(0, 10).ToList();
        ShipToSiteCounties = new List<int[]> { new int[5], new int[5] };

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

            PrefixList = await _context.Prefixes
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.ID.ToString(), Text = s.Prefx })
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

        foreach (var shipToSite in AdditionalShipToSites)
        {
            shipToSite.EaspSepsRegistrationId = EaspSepsRegistration.Id;
            _context.ShipToSites.Add(shipToSite);
        }
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

        foreach (var shipToSite in AdditionalShipToSites)
        {
            var shipToSiteIndex = AdditionalShipToSites.IndexOf(shipToSite);
            var counties = ShipToSiteCounties[shipToSiteIndex];

            foreach (var countyId in counties.Distinct())
            {
                if (countyId > 0)
                {
                    var shipToSiteCounty = new ShipToSiteCounty
                    {
                        ShipToSiteId = shipToSite.Id,
                        CountyId = countyId
                    };
                    _context.ShipToSiteCounties.Add(shipToSiteCounty);
                }
            }
        }

        await _context.SaveChangesAsync();

        SuccessMessage = "Registration successful!";
        return RedirectToPage("/Index");
    }

}

