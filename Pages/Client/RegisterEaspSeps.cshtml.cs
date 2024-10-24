using Microsoft.AspNetCore.Authorization;
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
using WebApplication1.Services;
using Microsoft.AspNetCore.Identity;



namespace WebApplication1.Pages.Client
{
    [Authorize(Roles = "Client,AdditionalUser")]

    public class RegisterEaspSepsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterEaspSepsModel> _logger;
        private readonly IEmailService _emailService; // Inject EmailService
        private readonly UserManager<ApplicationUser> _userManager; // Inject UserManager
        private readonly GeocodingService _geocodingService;




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

        public RegisterEaspSepsModel(ApplicationDbContext context, ILogger<RegisterEaspSepsModel> logger, IEmailService emailService, UserManager<ApplicationUser> userManager, GeocodingService geocodingService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService; // Assign the email service
            _userManager = userManager; // Assign UserManager
            _geocodingService = geocodingService;



        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Call your existing PopulateData method to fill the dropdowns
            await PopulateData();

            // Prefill the email in the model (retrieving the current user)
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            LoggedInUserEmail = user?.Email;

            // Initialize `CountiesServed` and `ShipToSiteCounties` for a fresh user or during GET request
            CountiesServed = Enumerable.Repeat(0, 10).ToList();  // Ensuring 10 elements for 10 dropdowns
            ShipToSiteCounties = new List<int[]> { new int[5], new int[5] };  // Initialize counties for each site with 5 items

            // Check if the user is an additional user
            var additionalUser = await _context.AdditionalUsers.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (additionalUser != null)
            {
                // If the logged-in user is an additional user, get the main user's registration form
                var mainUserId = additionalUser.AgencyRegistrationId;

                var existingRegistration = await _context.AgencyRegistrations
                    .Include(a => a.AgencyContacts)
                    .Include(a => a.AdditionalUsers)
                    .Include(a => a.ShipToSites)
                    .Include(a => a.CountiesServed)
                    .Include(a => a.ShipInformations) // Include ShipInformation
                    .FirstOrDefaultAsync(r => r.Id == mainUserId);

                if (existingRegistration != null)
                {
                    // If main user's registration exists, populate the form with existing data
                    EaspSepsRegistration = existingRegistration;
                    AgencyContact = existingRegistration.AgencyContacts.FirstOrDefault();
                    AdditionalUsers = existingRegistration.AdditionalUsers.ToList();
                    AdditionalShipToSites = existingRegistration.ShipToSites.ToList();
                    ShipInformation = existingRegistration.ShipInformations.FirstOrDefault();
                    CountiesServed = existingRegistration.CountiesServed.Select(c => c.CountyId).ToList();
                    ShipToSiteCounties = existingRegistration.ShipToSites
                        .Select(s => _context.ShipToSiteCounties
                            .Where(sc => sc.ShipToSiteId == s.Id)
                            .Select(sc => sc.CountyId)
                            .ToArray())
                        .ToList();

                    for (int i = 0; i < ShipToSiteCounties.Count; i++)
                    {
                        if (ShipToSiteCounties[i].Length < 5)
                        {
                            ShipToSiteCounties[i] = ShipToSiteCounties[i].Concat(new int[5 - ShipToSiteCounties[i].Length]).ToArray();
                        }
                    }

                    // Set ViewData["IsReadOnly"] to true for additional users
                    ViewData["IsReadOnly"] = true;
                }
            }
            else
            {
                // Check if the main user has filled out the registration form (existing registration)
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
                    AgencyContact = existingRegistration.AgencyContacts.FirstOrDefault();
                    AdditionalUsers = existingRegistration.AdditionalUsers.ToList();
                    AdditionalShipToSites = existingRegistration.ShipToSites.ToList();
                    ShipInformation = existingRegistration.ShipInformations.FirstOrDefault();
                    CountiesServed = existingRegistration.CountiesServed.Select(c => c.CountyId).ToList();
                    ShipToSiteCounties = existingRegistration.ShipToSites
                        .Select(s => _context.ShipToSiteCounties
                            .Where(sc => sc.ShipToSiteId == s.Id)
                            .Select(sc => sc.CountyId)
                            .ToArray())
                        .ToList();

                    for (int i = 0; i < ShipToSiteCounties.Count; i++)
                    {
                        if (ShipToSiteCounties[i].Length < 5)
                        {
                            ShipToSiteCounties[i] = ShipToSiteCounties[i].Concat(new int[5 - ShipToSiteCounties[i].Length]).ToArray();
                        }
                    }

                    // Set ViewData["IsReadOnly"] to true if the user has an existing registration
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

                    // Set ViewData["IsReadOnly"] to false for new registrations
                    ViewData["IsReadOnly"] = false;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendEditRequestAsync(string editTo, string editFrom, string editSubject, string editMessage)
        {
            if (!ModelState.IsValid)
            {
               // return Page();
            }

            // Load EaspSepsRegistration and AgencyContact from the database if needed
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user != null)
            {
                // Load the registration and associated data
                EaspSepsRegistration = await _context.AgencyRegistrations
                    .Include(ar => ar.AgencyContacts)
                    .FirstOrDefaultAsync(ar => ar.UserId == user.Id);

                if (EaspSepsRegistration != null)
                {
                    // Load the first AgencyContact
                    AgencyContact = EaspSepsRegistration.AgencyContacts?.FirstOrDefault();

                    // Fetch the UniqueId from the LnkAgencyClassificationData table
                    var agencyClassification = await _context.Lnk_AgencyClassificationData
                        .FirstOrDefaultAsync(acd => acd.AgencyRegistrationId == EaspSepsRegistration.Id);

                    var uniqueId = agencyClassification?.UniqueId ?? "Not Provided"; // Default message if null

                    // Construct the full email message
                    var agencyContactName = AgencyContact?.ProgramDirector ?? "Not Provided"; // Default message if null
                    var agencyContactEmail = AgencyContact?.Email ?? "Not Provided"; // Default message if null

                    var fullMessage = $@"
                <p>Dear Admin,</p>
                <p>The following edit request has been submitted:</p>
                <p><strong>From:</strong> {editFrom}</p>
                <p><strong>Unique ID:</strong> {uniqueId}</p>  <!-- Unique ID from LnkAgencyClassificationData -->
                <p><strong>Agency Contact Name:</strong> {agencyContactName}</p>
                <p><strong>Agency Contact Email:</strong> {agencyContactEmail}</p>
                <p><strong>Message:</strong></p>
                <p>{editMessage}</p>
                <p>Best regards,<br/>Your Application</p>
            ";

                    // Send email using your email service
                    await _emailService.SendEmailAsync(editTo, editSubject, fullMessage);

                    // Display success message and reload the page
                    SuccessMessage = "Your request for edits has been sent successfully.";
                    return RedirectToPage("/Client/RegisterEaspSeps");
                }
            }
            // Display success message and reload the page
            SuccessMessage = "Your request for edits has been sent successfully.";
            return RedirectToPage("/Client/RegisterEaspSeps");
        }



        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateData(); // Repopulate dropdowns but avoid resetting `CountiesServed`
                                      // return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "User is not logged in");
                return Page();
            }

            // Set registration details
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

            // Save agency contact
            AgencyContact.AgencyRegistrationId = EaspSepsRegistration.Id;
            _context.AgencyContacts.Add(AgencyContact);
            await _context.SaveChangesAsync();

            // Save additional users to both AdditionalUsers table and AspNetUsers table
            foreach (var additionalUser in AdditionalUsers)
            {
                // Check if the user already exists in the AspNetUsers table
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == additionalUser.Email);

                if (existingUser == null)
                {
                    // If the user doesn't exist, create a new user in AspNetUsers
                    var newUser = new ApplicationUser
                    {
                        UserName = additionalUser.Email,
                        Email = additionalUser.Email,
                        PhoneNumber = additionalUser.Phone,
                        EmailConfirmed = true, // Adjust this as per your requirements
                        Role = "AdditionalUser" // Assign the role as 'additionaluser'
                    };

                    // Set a default password for the new additional user
                    var password = "TempPassword123!"; // Replace with secure logic to generate or set the password
                    var result = await _userManager.CreateAsync(newUser, password);

                    if (result.Succeeded)
                    {
                        // Optionally, assign a role using the UserManager's AddToRoleAsync method
                        await _userManager.AddToRoleAsync(newUser, "AdditionalUser");

                        // Send an email to the new user with their temporary password
                        await SendAccountCreatedEmail(additionalUser.Email, password);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error creating user for additional user: " + additionalUser.Email);
                        continue; // Skip to the next user
                    }
                }
                else
                {
                    // If the user already exists, ensure that the role is properly set
                    existingUser.Role = "AdditionalUser";
                    _context.Users.Update(existingUser);
                }

                // Save additional user in AdditionalUsers table
                additionalUser.AgencyRegistrationId = EaspSepsRegistration.Id;
                _context.AdditionalUsers.Add(additionalUser);
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

            ////************** Google API's Geocode start here *************** 
            //// Geocode AgencyRegistration address
            //var agencyAddress = $"{EaspSepsRegistration.Address}, {EaspSepsRegistration.City}, {EaspSepsRegistration.State}, {EaspSepsRegistration.Zip}";
            //var (agencyLat, agencyLng) = await _geocodingService.GetCoordinatesAsync(agencyAddress);
            //if (agencyLat == null || agencyLng == null)
            //{
            //    ModelState.AddModelError(string.Empty, "Unable to fetch coordinates for the provided address.");
            //    await PopulateData();
            //    return Page(); // Stop the submission if geocoding fails
            //}
            //EaspSepsRegistration.Lat = agencyLat;
            //EaspSepsRegistration.Lng = agencyLng;

            //// Geocode AgencyContact address
            //var contactAddress = $"{AgencyContact.Address}, {AgencyContact.City}, {AgencyContact.State}, {AgencyContact.Zip}";
            //var (contactLat, contactLng) = await _geocodingService.GetCoordinatesAsync(contactAddress);
            //AgencyContact.Lat = contactLat;
            //AgencyContact.Lng = contactLng;

            //// Geocode ShipInformation address
            //var shipAddress = $"{ShipInformation.ShipToAddress}, {ShipInformation.ShipToCity}, {ShipInformation.ShipToState}, {ShipInformation.ShipToZip}";
            //var (shipLat, shipLng) = await _geocodingService.GetCoordinatesAsync(shipAddress);
            //ShipInformation.Lat = shipLat;
            //ShipInformation.Lng = shipLng;

            //// Geocode Additional ShipToSites
            //foreach (var shipToSite in AdditionalShipToSites)
            //{
            //    var shipToSiteAddress = $"{shipToSite.Address}, {shipToSite.City}, {shipToSite.State}, {shipToSite.Zip}";
            //    var (siteLat, siteLng) = await _geocodingService.GetCoordinatesAsync(shipToSiteAddress);
            //    shipToSite.Lat = siteLat;
            //    shipToSite.Lng = siteLng;
            //}

            //// Geocode AdditionalUsers' addresses
            //foreach (var additionalUser in AdditionalUsers)
            //{
            //    if (!string.IsNullOrEmpty(additionalUser.Address))
            //    {
            //        var additionalUserAddress = $"{additionalUser.Address}, {additionalUser.City}, {additionalUser.State}, {additionalUser.Zip}";
            //        var (additionalUserLat, additionalUserLng) = await _geocodingService.GetCoordinatesAsync(additionalUserAddress);
            //        additionalUser.Lat = additionalUserLat;
            //        additionalUser.Lng = additionalUserLng;
            //    }
            //}


            //_context.AgencyContacts.Add(AgencyContact);
            //_context.ShipInformations.Add(ShipInformation);
            //_context.ShipToSites.AddRange(AdditionalShipToSites);
            //_context.AdditionalUsers.AddRange(AdditionalUsers);
            //_context.AgencyRegistrations.Add(EaspSepsRegistration);

            await _context.SaveChangesAsync();

            // Send confirmation email to the agency contact
            await SendRegistrationConfirmationEmail(AgencyContact.Email, EaspSepsRegistration.AgencyName, AdditionalUsers);

            SuccessMessage = "Your registration has been successfully submitted. Please wait for approval. " +
                    "Your additional users have received a temporary password to access the account and place orders.";
            return RedirectToPage("/Client/Home");
        }

        private async Task SendAccountCreatedEmail(string email, string tempPassword)
        {
            var subject = "Account Created for ESAP/SEPS Registration";
            var message = $@"
        <p>Dear User,</p>
        <p>An account has been created for you as part of the ESAP/SEPS registration.</p>
        <p>Your temporary password is: <strong>{tempPassword}</strong></p>
        <p>Please reachout to your agency to change the password</p>
        <p>Best regards,<br/>Your Team</p>
    ";
            //  await _emailService.SendEmailAsync(email, subject, message);
        }



        private async Task SendRegistrationConfirmationEmail(string recipientEmail, string agencyName, List<AdditionalUser> additionalUsers)
        {
            var subject = "ESAP/SEPS Registration Confirmation";

            // Prepare the message for additional users' passwords
            var additionalUsersMessage = "";
            if (additionalUsers != null && additionalUsers.Count > 0)
            {
                additionalUsersMessage = "<p>Your additional users have received the following temporary passwords:</p><ul>";
                foreach (var user in additionalUsers)
                {
                    additionalUsersMessage += $"<li><strong>{user.Name}</strong> ({user.Email}): Temporary Password: TempPassword123!</li>";
                }
                additionalUsersMessage += "</ul>";
            }

            var message = $@"
        <p>Dear Colleague,</p>
        <p>Your registration for the agency <strong>{agencyName}</strong> has been successfully submitted.</p>
        <p>Please wait for the approval process to complete. Once your registration is approved, you will be notified accordingly.</p>
        {additionalUsersMessage}
<p>If you have any questions, feel free to reply to this email.</p>
        <p>Best regards,<br/>Your Company</p>
    ";

            //   await _emailService.SendEmailAsync(recipientEmail, subject, message);
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
                .ToListAsync();  // Ensure PrefixList is properly populated

            AgencyClassifications = await _context.LkAgencyClassifications
                .Where(c => c.is_active)
                .Select(c => new SelectListItem { Value = c.agency_classification_id.ToString(), Text = c.classifcation_description })
                .ToListAsync();
        }

    }
}