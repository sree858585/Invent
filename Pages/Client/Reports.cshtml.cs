using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using System.IO;
using WebApplication1.Services;
using System.Security.Claims;




namespace WebApplication1.Pages.Client
{
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // Inject EmailService
        public string UserEmail { get; set; }


        public ReportsModel(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService; // Assign the email service

            QuarterlyReport = new QuarterlyReport
            {
                CollectionDetails = new List<CollectionDetail>()
            };
        }

        [BindProperty]
        public QuarterlyReport QuarterlyReport { get; set; }

        public List<QuarterViewModel> UpcomingQuarters { get; set; } = new List<QuarterViewModel>();

        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(bool? success = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // return Unauthorized("User is not logged in.");
            }
            UserEmail = User.FindFirstValue(ClaimTypes.Email); // Fetch email in code-behind

            // Fetch the user's registration and classification
            var registration = await _context.AgencyRegistrations
                .Include(ar => ar.LnkAgencyClassificationData)
                .FirstOrDefaultAsync(ar => ar.UserId == userId);

            if (registration == null || !registration.LnkAgencyClassificationData.Any())
            {
                ModelState.AddModelError(string.Empty, "You are not registered or classified.");
                return Page();
            }

            // Fetch the ESAP Tier 2 classification id (adjust according to your data)
            var esapTier2ClassificationId = await _context.LkAgencyClassifications
                .Where(ac => ac.classifcation_description == "ESAP Tier 2")
                .Select(ac => ac.agency_classification_id)
                .FirstOrDefaultAsync();

            // Check if the user has ESAP Tier 2 classification by matching with the Category field
            var hasEsapTier2 = registration.LnkAgencyClassificationData
                .Any(c => c.Category == esapTier2ClassificationId);

            if (!hasEsapTier2)
            {
                // If the user is not classified as ESAP Tier 2, show a message
                ModelState.AddModelError(string.Empty, "Reports are only available for ESAP Tier 2 users.");
                return Page();
            }

            // Proceed with loading the reports for ESAP Tier 2 users
            var currentYear = DateTime.Now.Year;

            var reports = await _context.QuarterlyReports
                            .Where(qr => qr.UserId == userId && qr.Year == currentYear)
                            .Include(qr => qr.CollectionDetails)
                            .Select(qr => new {
                                qr.Id,
                                qr.UserId,
                                qr.Year,
                                qr.Quarter,
                                qr.DueDate,
                                qr.SubmissionDate,
                                qr.EditedDate,
                                qr.FacilityName,
                                qr.CompletedBy,
                                qr.Address,
                                qr.Phone,
                                qr.Fax,
                                qr.Status,
                                qr.CollectionDetails
                            })
                            .ToListAsync();

            var quarterData = new List<QuarterViewModel>
{
    new QuarterViewModel { QuarterName = "Q1", StartMonth = 1, EndMonth = 3, DueDate = new DateTime(currentYear, 4, 15) },
    new QuarterViewModel { QuarterName = "Q2", StartMonth = 4, EndMonth = 6, DueDate = new DateTime(currentYear, 7, 15) },
    new QuarterViewModel { QuarterName = "Q3", StartMonth = 7, EndMonth = 9, DueDate = new DateTime(currentYear, 10, 15) },
    new QuarterViewModel { QuarterName = "Q4", StartMonth = 10, EndMonth = 12, DueDate = new DateTime(currentYear + 1, 1, 15) }
};

            // Assign status to each quarter
            UpcomingQuarters = quarterData
                .Select(q =>
                {
                    var report = reports.FirstOrDefault(r => r.Quarter == q.QuarterName);
                    q.Status = report != null && report.Status == "Submitted" ? "Submitted" : "Pending";
                    q.ReportId = report?.Id ?? 0; // Store the report ID for editing
                    return q;
                })
                .ToList();

            if (success == true)
            {
                SuccessMessage = "Report submitted successfully!";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRaiseEditRequestAsync(int reportId)
        {
            // Get the email of the current user
            var email = User.FindFirstValue(ClaimTypes.Email);

            // Define the subject and body of the email
            var emailSubject = $"Report Edit Request for Report #{reportId}";
            var emailBody = $"The user with email {email} has requested an edit for Report #{reportId}. Please review the request.";

            // Send the email using the injected email service
            await _emailService.SendEmailAsync("hemanthgara.hg@gmail.com", emailSubject, emailBody);

            // Display a success message to the user
            SuccessMessage = "Your edit request has been submitted. You will be notified once it's processed.";

            return RedirectToPage(new { success = true });
        }

        public async Task<IActionResult> OnPostDownloadReportAsync(int reportId)
        {
            // Find the report based on the ID
            var report = await _context.QuarterlyReports
                            .Include(q => q.CollectionDetails)
                            .FirstOrDefaultAsync(q => q.Id == reportId);

            if (report == null)
            {
                return NotFound("Report not found.");
            }

            // Create the CSV file in memory
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField("FacilityName");
                csv.WriteField("CompletedBy");
                csv.WriteField("Address");
                csv.WriteField("Phone");
                csv.WriteField("Fax");
                csv.WriteField("SyringesProvidedUnits");
                csv.WriteField("SyringesProvidedSessions");
                csv.WriteField("PharmacyVouchersUnits");
                csv.WriteField("PharmacyVouchersSessions");
                csv.WriteField("ReportedVouchersUnits");
                csv.WriteField("ReportedVouchersSessions");
                csv.WriteField("FitpacksProvidedUnits");
                csv.WriteField("FitpacksProvidedSessions");
                csv.WriteField("QuartContainersProvidedUnits");
                csv.WriteField("QuartContainersProvidedSessions");
                csv.WriteField("GallonContainersProvidedUnits");
                csv.WriteField("GallonContainersProvidedSessions");
                csv.WriteField("OtherSuccessesConcernsIssues");
                csv.NextRecord();

                // Write the report data to CSV
                csv.WriteField(report.FacilityName);
                csv.WriteField(report.CompletedBy);
                csv.WriteField(report.Address);
                csv.WriteField(report.Phone);
                csv.WriteField(report.Fax);
                csv.WriteField(report.SyringesProvidedUnits);
                csv.WriteField(report.SyringesProvidedSessions);
                csv.WriteField(report.PharmacyVouchersUnits);
                csv.WriteField(report.PharmacyVouchersSessions);
                csv.WriteField(report.ReportedVouchersUnits);
                csv.WriteField(report.ReportedVouchersSessions);
                csv.WriteField(report.FitpacksProvidedUnits);
                csv.WriteField(report.FitpacksProvidedSessions);
                csv.WriteField(report.QuartContainersProvidedUnits);
                csv.WriteField(report.QuartContainersProvidedSessions);
                csv.WriteField(report.GallonContainersProvidedUnits);
                csv.WriteField(report.GallonContainersProvidedSessions);
                csv.WriteField(report.OtherSuccessesConcernsIssues);
                csv.NextRecord();

                // Add the collection details
                csv.WriteField("SharpsCollectionSite");
                csv.WriteField("CollectionDates");
                csv.WriteField("PoundsCollected");
                csv.NextRecord();

                foreach (var detail in report.CollectionDetails)
                {
                    csv.WriteField(detail.SharpsCollectionSite);
                    csv.WriteField(detail.CollectionDates.HasValue ? detail.CollectionDates.Value.ToString("yyyy-MM-dd") : string.Empty);
                    csv.WriteField(detail.PoundsCollected);
                    csv.NextRecord();
                }

                writer.Flush();
                memoryStream.Position = 0;

                // Return the CSV file to the user
                return File(memoryStream.ToArray(), "text/csv", $"QuarterlyReport_{report.Quarter}_{report.Year}.csv");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Handle validation errors
                //  return Page();
            }

            if (QuarterlyReport.CollectionDetails == null)
            {
                QuarterlyReport.CollectionDetails = new List<CollectionDetail>();
            }

            // Check for null or invalid collection dates
            foreach (var detail in QuarterlyReport.CollectionDetails)
            {
                if (detail.CollectionDates == default(DateTime))
                {
                    ModelState.AddModelError(string.Empty, "Please provide a valid Collection Date.");
                    // return Page();
                }
            }

            if (QuarterlyReport.Id > 0)
            {
                // Editing existing report
                var existingReport = await _context.QuarterlyReports.Include(q => q.CollectionDetails).FirstOrDefaultAsync(q => q.Id == QuarterlyReport.Id);
                if (existingReport != null)
                {
                    // Update all fields in the existing report
                    existingReport.FacilityName = QuarterlyReport.FacilityName;
                    existingReport.CompletedBy = QuarterlyReport.CompletedBy;
                    existingReport.Address = QuarterlyReport.Address;
                    existingReport.Phone = QuarterlyReport.Phone;
                    existingReport.Fax = QuarterlyReport.Fax;

                    // Update additional fields
                    existingReport.SyringesProvidedUnits = QuarterlyReport.SyringesProvidedUnits;
                    existingReport.SyringesProvidedSessions = QuarterlyReport.SyringesProvidedSessions;
                    existingReport.PharmacyVouchersUnits = QuarterlyReport.PharmacyVouchersUnits;
                    existingReport.PharmacyVouchersSessions = QuarterlyReport.PharmacyVouchersSessions;
                    existingReport.ReportedVouchersUnits = QuarterlyReport.ReportedVouchersUnits;
                    existingReport.ReportedVouchersSessions = QuarterlyReport.ReportedVouchersSessions;
                    existingReport.FitpacksProvidedUnits = QuarterlyReport.FitpacksProvidedUnits;
                    existingReport.FitpacksProvidedSessions = QuarterlyReport.FitpacksProvidedSessions;
                    existingReport.QuartContainersProvidedUnits = QuarterlyReport.QuartContainersProvidedUnits;
                    existingReport.QuartContainersProvidedSessions = QuarterlyReport.QuartContainersProvidedSessions;
                    existingReport.GallonContainersProvidedUnits = QuarterlyReport.GallonContainersProvidedUnits;
                    existingReport.GallonContainersProvidedSessions = QuarterlyReport.GallonContainersProvidedSessions;
                    existingReport.OtherSuccessesConcernsIssues = QuarterlyReport.OtherSuccessesConcernsIssues;

                    // Clear and update collection details
                    existingReport.CollectionDetails.Clear();
                    foreach (var detail in QuarterlyReport.CollectionDetails)
                    {
                        existingReport.CollectionDetails.Add(new CollectionDetail
                        {
                            SharpsCollectionSite = detail.SharpsCollectionSite,
                            CollectionDates = detail.CollectionDates,
                            PoundsCollected = detail.PoundsCollected
                        });
                    }

                    existingReport.EditedDate = DateTime.Now; // Capture edited date
                }
            }

            else
            {
                // Creating a new report
                QuarterlyReport.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                QuarterlyReport.SubmissionDate = DateTime.Now;
                QuarterlyReport.Status = "Submitted";
                QuarterlyReport.Year = DateTime.Now.Year;

                _context.QuarterlyReports.Add(QuarterlyReport);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = true });
        }

        public async Task<IActionResult> OnGetLoadReportAsync(int id)
        {
            try
            {
                var quarterlyReport = await _context.QuarterlyReports
                                                    .Include(qr => qr.CollectionDetails)
                                                    .FirstOrDefaultAsync(qr => qr.Id == id);

                if (quarterlyReport == null)
                {
                    return NotFound();
                }

                // Log the collection details to ensure they are populated
                Console.WriteLine($"CollectionDetails: {quarterlyReport.CollectionDetails.Count}");

                var response = new
                {
                    Id = quarterlyReport.Id,
                    FacilityName = quarterlyReport.FacilityName,
                    CompletedBy = quarterlyReport.CompletedBy,
                    Address = quarterlyReport.Address,
                    Phone = quarterlyReport.Phone,
                    Fax = quarterlyReport.Fax,
                    SyringesProvidedUnits = quarterlyReport.SyringesProvidedUnits,
                    SyringesProvidedSessions = quarterlyReport.SyringesProvidedSessions,
                    PharmacyVouchersUnits = quarterlyReport.PharmacyVouchersUnits,
                    PharmacyVouchersSessions = quarterlyReport.PharmacyVouchersSessions,
                    ReportedVouchersUnits = quarterlyReport.ReportedVouchersUnits,
                    ReportedVouchersSessions = quarterlyReport.ReportedVouchersSessions,
                    FitpacksProvidedUnits = quarterlyReport.FitpacksProvidedUnits,
                    FitpacksProvidedSessions = quarterlyReport.FitpacksProvidedSessions,
                    QuartContainersProvidedUnits = quarterlyReport.QuartContainersProvidedUnits,
                    QuartContainersProvidedSessions = quarterlyReport.QuartContainersProvidedSessions,
                    GallonContainersProvidedUnits = quarterlyReport.GallonContainersProvidedUnits,
                    GallonContainersProvidedSessions = quarterlyReport.GallonContainersProvidedSessions,
                    OtherSuccessesConcernsIssues = quarterlyReport.OtherSuccessesConcernsIssues,
                    CollectionDetails = quarterlyReport.CollectionDetails.Select(cd => new {
                        SharpsCollectionSite = cd.SharpsCollectionSite,
                        CollectionDates = cd.CollectionDates.HasValue ? cd.CollectionDates.Value.ToString("yyyy-MM-dd") : null,
                        PoundsCollected = cd.PoundsCollected
                    }).ToList(),
                    IsSubmitted = quarterlyReport.Status == "Submitted"
                };

                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading report: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }




        public class QuarterViewModel
        {
            public string QuarterName { get; set; }
            public int StartMonth { get; set; }
            public int EndMonth { get; set; }
            public DateTime DueDate { get; set; }
            public string Status { get; set; }
            public int ReportId { get; set; } // Added ReportId for editing
        }
    }
}