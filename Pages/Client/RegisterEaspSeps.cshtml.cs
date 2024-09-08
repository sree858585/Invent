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
    public AgencyRegistration EaspSepsRegistration { get; set; }

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

    public string LoggedInUserEmail { get; set; }

    [BindProperty]
    public AgencyContact Agencycontact { get; set; }

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
        // Call your existing PopulateData method to fill the dropdowns
        await PopulateData();

        // Only initialize `CountiesServed` and `ShipToSiteCounties` during the first GET request, not during POST
        if (!Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            CountiesServed = Enumerable.Repeat(0, 10).ToList();  // Ensuring 10 elements for 10 dropdowns
            ShipToSiteCounties = new List<int[]> { new int[5], new int[5] };  // Initialize counties for each site
        }

        // Prefill the email in the model (retrieving the current user)
        var userEmail = User.Identity.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        LoggedInUserEmail = user?.Email;

        // Check if the user has already filled out the registration form (existing registration)
        var existingRegistration = await _context.AgencyRegistrations
            .Include(a => a.AgencyContacts)
            .Include(a => a.AdditionalUsers)
            .Include(a => a.ShipToSites)
            .Include(a => a.CountiesServed)
            .Include(a => a.ShipInformations) // Include ShipInformation
            .FirstOrDefaultAsync(r => r.UserId == user.Id);

        if (existingRegistration != null)
        {
            // If registration exists, populate the form with existing data
            EaspSepsRegistration = existingRegistration;

            // Populate the AgencyContact with the first contact (assuming only one is the primary)
            AgencyContact = existingRegistration.AgencyContacts.FirstOrDefault();

            // Load the additional users if they exist
            AdditionalUsers = existingRegistration.AdditionalUsers.ToList();

            // Load the ship-to sites if they exist
            AdditionalShipToSites = existingRegistration.ShipToSites.ToList();

            // Load the ShipInformation
            ShipInformation = existingRegistration.ShipInformations.FirstOrDefault();

            // Populate the counties served
            CountiesServed = existingRegistration.CountiesServed.Select(c => c.CountyId).ToList();

            // Populate the counties for each ShipToSite
            ShipToSiteCounties = existingRegistration.ShipToSites
                .Select(s => _context.ShipToSiteCounties
                    .Where(sc => sc.ShipToSiteId == s.Id)
                    .Select(sc => sc.CountyId)
                    .ToArray())
                .ToList();
            // Ensure that every ShipToSite has exactly 5 counties, even if some are missing
            for (int i = 0; i < ShipToSiteCounties.Count; i++)
            {
                if (ShipToSiteCounties[i].Length < 5)
                {
                    // Extend the array to have exactly 5 elements, filling with default value 0
                    ShipToSiteCounties[i] = ShipToSiteCounties[i].Concat(new int[5 - ShipToSiteCounties[i].Length]).ToArray();
                }
            }
            // Optionally, mark the form as readonly based on certain conditions
            ViewData["IsReadOnly"] = true;
        }
        else
        {
            // If no existing registration, prepare for a new submission
            EaspSepsRegistration = new AgencyRegistration();
            AgencyContact = new AgencyContact { Email = LoggedInUserEmail };
            AdditionalUsers = new List<AdditionalUser> { new AdditionalUser() };
            AdditionalShipToSites = new List<ShipToSite>();
            ShipInformation = new ShipInformation(); // Initialize ShipInformation for new registration
            ViewData["IsReadOnly"] = false;
        }

        return Page();
    }


    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateData(); // Repopulate dropdowns but avoid resetting `CountiesServed`
                                  // return Page();
        }

        _logger.LogInformation("CountiesServed count: " + CountiesServed.Count);
        foreach (var county in CountiesServed)
        {
            _logger.LogInformation("CountyId: " + county);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError(string.Empty, "User is not logged in");
            return Page();
        }

        EaspSepsRegistration.UserId = userId;
        EaspSepsRegistration.SubmissionDate = DateTime.Now; // Set the submission date
        EaspSepsRegistration.Status = "Pending"; // Set the status to Pending

        // Save selected classifications
        var selectedClassifications = new List<string>();
        if (IsSyringeExchangeProgram) selectedClassifications.Add("Syringe exchange program");
        if (IsESAPTier1) selectedClassifications.Add("ESAP Tier 1");
        if (IsESAPTier2) selectedClassifications.Add("ESAP Tier 2");
        if (IsOther) selectedClassifications.Add("Other");

        EaspSepsRegistration.RegistrationType = string.Join(", ", selectedClassifications);

        _context.AgencyRegistrations.Add(EaspSepsRegistration);
        await _context.SaveChangesAsync();

        var classificationIds = new List<int>();
        if (IsSyringeExchangeProgram) classificationIds.Add(1);
        if (IsESAPTier1) classificationIds.Add(2);
        if (IsESAPTier2) classificationIds.Add(3);
        if (IsOther) classificationIds.Add(4);

        foreach (var id in classificationIds)
        {
            var uniqueIdPrefix = id switch
            {
                1 => "SP",
                2 => "ET1",
                3 => "ET2",
                4 => "OTH",
                _ => throw new ArgumentOutOfRangeException()
            };
            var uniqueId = $"{uniqueIdPrefix}{EaspSepsRegistration.Id}";

            var lnkAgencyClassificationData = new LnkAgencyClassificationData
            {
                Category = id,
                AgencyRegistrationId = EaspSepsRegistration.Id,
                UniqueId = uniqueId
            };
            _context.Lnk_AgencyClassificationData.Add(lnkAgencyClassificationData);
        }
        await _context.SaveChangesAsync();

        // Save agency contact
        AgencyContact.AgencyRegistrationId = EaspSepsRegistration.Id;
        _context.AgencyContacts.Add(AgencyContact);
        await _context.SaveChangesAsync();

        // Save additional users
        foreach (var user in AdditionalUsers)
        {
            user.AgencyRegistrationId = EaspSepsRegistration.Id;
            _context.AdditionalUsers.Add(user);
        }
        await _context.SaveChangesAsync();

        // Save shipping information
        ShipInformation.AgencyRegistrationId = EaspSepsRegistration.Id;
        _context.ShipInformations.Add(ShipInformation);
        await _context.SaveChangesAsync();

        // Save additional Ship to Sites
        foreach (var shipToSite in AdditionalShipToSites)
        {
            shipToSite.AgencyRegistrationId = EaspSepsRegistration.Id;
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
                    AgencyRegistrationId = EaspSepsRegistration.Id,
                    CountyId = countyId
                };
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

        SuccessMessage = "Your registration has been successfully submitted. Please wait for the approval.";
//        TempData["SuccessMessage"] = "Your registration has been successfully submitted. Please wait for the approval.";

        return RedirectToPage("/Client/Home");
    }

    private async Task PopulateData()
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
    }
}
