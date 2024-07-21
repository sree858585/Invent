using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

public class RegisterEaspSepsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegisterEaspSepsModel> _logger;

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

    [BindProperty]
    public bool IsSyringeExchangeProgram { get; set; }

    [BindProperty]
    public bool IsESAPTier1 { get; set; }

    [BindProperty]
    public bool IsESAPTier2 { get; set; }

    [BindProperty]
    public bool IsOther { get; set; }

    [BindProperty]
    public string OtherClassificationText { get; set; }

    public List<SelectListItem> CountyList { get; set; }
    public List<SelectListItem> SuffixList { get; set; }
    public List<SelectListItem> PrefixList { get; set; }
    public List<SelectListItem> AgencyClassifications { get; set; }

    public string SuccessMessage { get; set; }

    public RegisterEaspSepsModel(ApplicationDbContext context, ILogger<RegisterEaspSepsModel> logger)
    {
        _context = context;
        _logger = logger;
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

        AgencyClassifications = await _context.LkAgencyClassifications
            .Where(c => c.is_active)
            .Select(c => new SelectListItem { Value = c.agency_classification_id.ToString(), Text = c.classifcation_description })
            .ToListAsync();

        CountiesServed = Enumerable.Repeat(0, 10).ToList();
        ShipToSiteCounties = new List<int[]> { new int[5], new int[5] };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            //return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError(string.Empty, "User is not logged in");
            return Page();
        }

        EaspSepsRegistration.UserId = userId;

        var selectedClassifications = new List<string>();
        if (IsSyringeExchangeProgram) selectedClassifications.Add("Syringe exchange program");
        if (IsESAPTier1) selectedClassifications.Add("ESAP Tier 1");
        if (IsESAPTier2) selectedClassifications.Add("ESAP Tier 2");
        if (IsOther) selectedClassifications.Add("Other: " + OtherClassificationText);

        EaspSepsRegistration.RegistrationType = string.Join(", ", selectedClassifications);

        _context.EaspSepsRegistrations.Add(EaspSepsRegistration);
        await _context.SaveChangesAsync();

        var classificationIds = new List<int>();
        if (IsSyringeExchangeProgram) classificationIds.Add(1);
        if (IsESAPTier1) classificationIds.Add(2);
        if (IsESAPTier2) classificationIds.Add(3);
        if (IsOther) classificationIds.Add(4);

        foreach (var id in classificationIds)
        {
            var agentClassificationData = new AgentClassificationData
            {
                Category = id,
                Other = IsOther && id == 4,
                EaspSepsRegistrationID = EaspSepsRegistration.Id,
                OtherClassificationText = IsOther && id == 4 ? OtherClassificationText ?? string.Empty : null
            };
            _context.AgentClassificationData.Add(agentClassificationData);
        }
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

        // Save CountiesServed
        foreach (var countyId in CountiesServed.Distinct())
        {
            _logger.LogInformation($"Processing CountyId: {countyId}");
            if (countyId > 0)
            {
                var entity = new EaspSepsRegistrationCounty
                {
                    EaspSepsRegistrationId = EaspSepsRegistration.Id,
                    CountyId = countyId
                };
                _logger.LogInformation($"Adding EaspSepsRegistrationCounty: {entity.EaspSepsRegistrationId}, {entity.CountyId}");
                _context.EaspSepsRegistrationCounties.Add(entity);
            }
        }

        // Save ShipToSiteCounties
        for (var i = 0; i < AdditionalShipToSites.Count; i++)
        {
            var shipToSite = AdditionalShipToSites[i];
            var counties = ShipToSiteCounties[i];

            foreach (var countyId in counties.Distinct())
            {
                _logger.LogInformation($"Processing ShipToSiteId: {shipToSite.Id}, CountyId: {countyId}");
                if (countyId > 0)
                {
                    var shipToSiteCounty = new ShipToSiteCounty
                    {
                        ShipToSiteId = shipToSite.Id,
                        CountyId = countyId
                    };
                    _logger.LogInformation($"Adding ShipToSiteCounty: {shipToSiteCounty.ShipToSiteId}, {shipToSiteCounty.CountyId}");
                    _context.ShipToSiteCounties.Add(shipToSiteCounty);
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"EaspSepsRegistration Id: {EaspSepsRegistration.Id}");
        _logger.LogInformation($"CountiesServed Count: {CountiesServed.Count}");
        _logger.LogInformation($"ShipToSiteCounties Count: {ShipToSiteCounties.Count}");


        SuccessMessage = "Registration successful!";
        return RedirectToPage("/Index");
    }
}
