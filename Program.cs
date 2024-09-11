using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Serilog;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

builder.Services.Configure<IdentityOptions>(options =>
{
    // Default Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Default User settings.
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;

    // Default Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Default SignIn settings.
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
});

// Inject the SMTP settings from configuration
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();  // Register email service

builder.Services.AddAuthorization();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await CreateRolesAndAdminUser(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating roles and admin user.");
    }
}

app.Run();

static async Task CreateRolesAndAdminUser(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    string[] roleNames = { "Client", "Admin", "Distributor" };
    IdentityResult roleResult;

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
            {
                throw new Exception($"Error creating role {roleName}: {roleResult.Errors.FirstOrDefault()?.Description}");
            }
        }
    }

    // Admin User creation
    var adminConfig = configuration.GetSection("AdminUser");
    var adminUser = new ApplicationUser
    {
        UserName = adminConfig["UserName"],
        Email = adminConfig["Email"],
        Role = adminConfig["Role"]
    };

    var _adminUser = await userManager.FindByEmailAsync(adminConfig["Email"]);
    if (_adminUser == null)
    {
        var createAdminUser = await userManager.CreateAsync(adminUser, adminConfig["Password"]);
        if (createAdminUser.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminConfig["Role"]);
        }
        else
        {
            throw new Exception($"Error creating admin user: {createAdminUser.Errors.FirstOrDefault()?.Description}");
        }
    }

    // Distributor User creation
    var distributorConfig = configuration.GetSection("DistributorUser");
    var distributorUser = new ApplicationUser
    {
        UserName = distributorConfig["UserName"],
        Email = distributorConfig["Email"],
        Role = distributorConfig["Role"]
    };

    var _distributorUser = await userManager.FindByEmailAsync(distributorConfig["Email"]);
    if (_distributorUser == null)
    {
        var createDistributorUser = await userManager.CreateAsync(distributorUser, distributorConfig["Password"]);
        if (createDistributorUser.Succeeded)
        {
            await userManager.AddToRoleAsync(distributorUser, distributorConfig["Role"]);
        }
        else
        {
            throw new Exception($"Error creating distributor user: {createDistributorUser.Errors.FirstOrDefault()?.Description}");
        }
    }
}

// Email service logic
public class SmtpSettings
{
    public bool IsDevelopment { get; set; }
    public SmtpConfig DevSettings { get; set; }
    public SmtpConfig ProdSettings { get; set; }
}

public class SmtpConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool EnableSSL { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpConfig = _smtpSettings.IsDevelopment ? _smtpSettings.DevSettings : _smtpSettings.ProdSettings;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpConfig.UserName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        using var smtpClient = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
        {
            Credentials = new NetworkCredential(smtpConfig.UserName, smtpConfig.Password),
            EnableSsl = smtpConfig.EnableSSL
        };
        await smtpClient.SendMailAsync(mailMessage);
    }
}
