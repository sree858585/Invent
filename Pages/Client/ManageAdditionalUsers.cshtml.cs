using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Pages.Client
{
    [Authorize(Roles = "Client")]

    public class ManageAdditionalUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService; // Inject EmailService


        public ManageAdditionalUsersModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService; // Assign the email service

        }

        [BindProperty]
        public List<AdditionalUser> AdditionalUsers { get; set; }

        [BindProperty]
        public List<Prefix> Prefixes { get; set; }

        [BindProperty]
        public List<Suffix> Suffixes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Fetch the additional users associated with this user's agency
            var agency = await _context.AgencyRegistrations
                .Include(a => a.AdditionalUsers)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (agency == null)
            {
                return NotFound("Agency not found.");
            }

            AdditionalUsers = agency.AdditionalUsers.ToList();
            Prefixes = await _context.Prefixes.Where(p => p.IsActive).ToListAsync();
            Suffixes = await _context.Suffixes.Where(s => s.IsActive).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetAdditionalUserDetailsAsync(int id)
        {
            var additionalUser = await _context.AdditionalUsers
                .Include(u => u.Prefix)
                .Include(u => u.Suffix)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (additionalUser == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            return new JsonResult(new { success = true, additionalUser });
        }


        public async Task<IActionResult> OnPostSaveAdditionalUserAsync([FromBody] AdditionalUser user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    //return new JsonResult(new { success = false, message = "Invalid data." });
                }

                // Retrieve the current logged-in user's AgencyRegistrationId
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var agency = await _context.AgencyRegistrations
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (agency == null)
                {
                    return new JsonResult(new { success = false, message = "Agency not found." });
                }

                if (user.Id == 0)
                {
                    // New additional user
                    user.AgencyRegistrationId = agency.Id;  // Map AdditionalUser to the correct AgencyRegistration
                    _context.AdditionalUsers.Add(user);

                    // Now create the Identity user for the additional user
                    var identityUser = new ApplicationUser
                    {
                        UserName = user.Email,
                        Email = user.Email,
                        EmailConfirmed = true,
                        Role = "AdditionalUser"  // Explicitly set the Role field if required
                    };

                    // Generate a temporary password
                    string tempPassword = "TempPassword123!"; // You can replace this with any secure password generation logic
                    var result = await _userManager.CreateAsync(identityUser, tempPassword);

                    if (result.Succeeded)
                    {
                        // Optionally, assign a specific role to the additional user
                        await _userManager.AddToRoleAsync(identityUser, "AdditionalUser");

                        // Send the email to the new user with their temporary password
                        await SendAccountCreatedEmail(user.Email, tempPassword);
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = result.Errors.FirstOrDefault()?.Description });
                    }
                }
                else
                {
                    // Update existing additional user
                    var existingUser = await _context.AdditionalUsers.FindAsync(user.Id);
                    if (existingUser == null)
                    {
                        return new JsonResult(new { success = false, message = "User not found." });
                    }

                    // Update all fields for the existing user
                    existingUser.Name = user.Name;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;
                    existingUser.Address = user.Address;
                    existingUser.Address2 = user.Address2;
                    existingUser.City = user.City;
                    existingUser.State = user.State;
                    existingUser.Zip = user.Zip;
                    existingUser.PrefixId = user.PrefixId;
                    existingUser.SuffixId = user.SuffixId;
                    existingUser.Title = user.Title;
                    existingUser.SameAddressAsAgency = user.SameAddressAsAgency;
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new JsonResult(new { success = false, message = "Server error: " + innerException });
            }
        }

        public async Task<IActionResult> OnPostDeleteAdditionalUserAsync(int id)
        {
            try
            {
                var additionalUser = await _context.AdditionalUsers.FindAsync(id);

                if (additionalUser == null)
                {
                    return new JsonResult(new { success = false, message = "Additional user not found." });
                }

                // Optionally remove the identity user
                var identityUser = await _userManager.FindByEmailAsync(additionalUser.Email);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                // Remove the additional user from the database
                _context.AdditionalUsers.Remove(additionalUser);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }


        private async Task SendAccountCreatedEmail(string email, string tempPassword)
        {
            var subject = "Account Created for ESAP/SEPS Registration";
            var message = $@"
        <p>Dear User,</p>
        <p>An account has been created for you as part of the ESAP/SEPS registration.</p>
        <p>Your temporary password is: <strong>{tempPassword}</strong></p>
        <p>Please log in and change your password as soon as possible.</p>
        <p>Best regards,<br/>Your Team</p>
    ";
            await _emailService.SendEmailAsync(email, subject, message);
        }



        public async Task<IActionResult> OnPostSetPasswordAsync([FromBody] SetPasswordModel passwordModel)
        {
            var additionalUser = await _context.AdditionalUsers.FindAsync(passwordModel.Id);

            if (additionalUser == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            // Retrieve the associated identity user
            var identityUser = await _userManager.FindByEmailAsync(additionalUser.Email);

            if (identityUser == null)
            {
                return new JsonResult(new { success = false, message = "User not found in identity." });
            }

            // Remove the old password and set a new one
            var result = await _userManager.RemovePasswordAsync(identityUser);
            if (result.Succeeded)
            {
                result = await _userManager.AddPasswordAsync(identityUser, passwordModel.Password);
                if (result.Succeeded)
                {
                    return new JsonResult(new { success = true });
                }
            }

            return new JsonResult(new { success = false, message = "Error updating password." });
        }

    }

    public class SetPasswordModel
    {
        public int Id { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
