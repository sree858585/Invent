using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin
{
    public class ManageAccountsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ManageAccountsModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public List<ApplicationUser> Users { get; set; }

        [BindProperty]
        public CreateUserModel NewUser { get; set; }

        [BindProperty]
        public ChangePasswordModel ChangePassword { get; set; }

        public class ChangePasswordModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

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

        public async Task<IActionResult> OnGetAsync()
        {
            LoadUsers();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadUsers();
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = NewUser.Email,
                Email = NewUser.Email
            };

            var result = await _userManager.CreateAsync(user, NewUser.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, NewUser.Role);
                ViewData["SuccessMessage"] = "New user created successfully.";
            }
            else
            {
                ViewData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewData["ErrorMessage"] = "User not found.";
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = "NewPassword@123";  // Generate a default or random password
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                ViewData["SuccessMessage"] = $"Password reset for {email}. New Password: {newPassword}";
            }
            else
            {
                ViewData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        private void LoadUsers()
        {
            Users = _userManager.Users.ToList();
        }
    }

}
