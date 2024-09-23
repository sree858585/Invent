using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.Services;


namespace WebApplication1.Pages.Admin
{
    public class ManageAccountsModel : PageModel
    {
        private readonly UserManager<ApplicationUser>
    _userManager;
        private readonly IEmailService _emailService; // Inject EmailService


        public ManageAccountsModel(UserManager<ApplicationUser>
            userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService; // Assign the email service

        }

        public List<ApplicationUser>
            Users
        { get; set; }

        // Pagination properties
        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalUsers / PageSize);

        [BindProperty]
        public CreateUserModel NewUser { get; set; }

        public class CreateUserModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            public string Role { get; set; }
        }

        public async Task OnGetAsync(string searchTerm, string roleFilter, int pageNumber = 1)
        {
            CurrentPage = pageNumber;
            var usersQuery = _userManager.Users.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchTerm));
            }

            // Apply role filter if provided
            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
            {
                usersQuery = usersQuery.Where(u => u.Role == roleFilter);
            }

            TotalUsers = usersQuery.Count();

            // Apply pagination
            Users = await Task.Run(() => usersQuery
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList());
        }

        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            if (!ModelState.IsValid)
            {
                //return Page();
            }

            string password = string.Empty;

            // If the role is 'Client', generate a unique temporary password.
            if (NewUser.Role == "Client")
            {
                password = GenerateUniquePassword(); // Automatically generate password for Client role
            }
            else
            {
                // For Admin and Distributor, use the password provided by the admin
                password = NewUser.Password;
            }
            // Check if the username already exists
            var existingUser = await _userManager.FindByEmailAsync(NewUser.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Email already exists.");
                return Page();
            }

            // Validate password format
            //if (!ValidatePassword(NewUser.Password))
            //{
            //    ModelState.AddModelError(string.Empty, "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            //    return Page();
            //}
            var user = new ApplicationUser
            {
                UserName = NewUser.Email,
                Email = NewUser.Email,
                Role = NewUser.Role
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, NewUser.Role);

                // Send email notification for Client role if password was auto-generated
                if (NewUser.Role == "Client")
                {
                    string subject = "Your New Account Has Been Created";
                    string message = $@"
                <p>Dear {NewUser.Email},</p>
                <p>Your account has been created. Here is your temporary password:</p>
                <p><strong>{password}</strong></p>
                <p>Please log in and change your password as soon as possible.</p>
                <p>Best regards,<br/>Your Team</p>";

                    await _emailService.SendEmailAsync(NewUser.Email, subject, message);
                }

                TempData["SuccessMessage"] = $"User '{NewUser.Email}' created successfully.";
                return RedirectToPage();
            }
            else
            {
                // Log the errors returned by the user creation process
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

        }
        // Password validation logic
        private bool ValidatePassword(string password)
        {
            // Check password length
            if (password.Length < 8)
                return false;

            // Check for at least one uppercase letter
            if (!password.Any(char.IsUpper))
                return false;

            // Check for at least one lowercase letter
            if (!password.Any(char.IsLower))
                return false;

            // Check for at least one number
            if (!password.Any(char.IsDigit))
                return false;

            // Check for at least one special character
            if (!password.Any(ch => "!@#$%^&*()_-+=[]{}|;:'\",.<>?/".Contains(ch)))
                return false;

            return true;
        }


        public async Task<IActionResult>
                OnPostResetPasswordAsync(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage();
            }

            // Generate a unique temporary password
            string newPassword = GenerateUniquePassword();

            // Generate a reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Reset the password
            var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (resetResult.Succeeded)
            {
                // Send email notification to the user with the new password
                string subject = "Your Password Has Been Reset";
                string message = $@"
                    <p>Dear {user.Email},</p>
                    <p>Your password has been reset. Here is your new temporary password:</p>
                    <p><strong>{newPassword}</strong></p>
                    <p>Please log in and change your password as soon as possible.</p>
                    <p>Best regards,<br />Your Team</p>";

                await _emailService.SendEmailAsync(user.Email, subject, message);

                TempData["SuccessMessage"] = $"Password has been reset and an email has been sent to {user.Email}.";
                return RedirectToPage();
            }

            TempData["ErrorMessage"] = "Error resetting the password.";
            return Page();
        }
        private string GenerateUniquePassword()
        {
            // Ensure password meets the requirements of the UserManager
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


    }
}