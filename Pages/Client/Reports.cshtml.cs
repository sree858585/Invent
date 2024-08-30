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

namespace WebApplication1.Pages.Client
{
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
            QuarterlyReport = new QuarterlyReport
            {
                CollectionDetails = new List<CollectionDetail>()
            };
        }

        [BindProperty]
        public QuarterlyReport QuarterlyReport { get; set; }

        public List<QuarterViewModel> UpcomingQuarters { get; set; } = new List<QuarterViewModel>();

        public string SuccessMessage { get; set; }

        public async Task OnGetAsync(bool? success = null)
        {
            var currentYear = DateTime.Now.Year;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

                return new JsonResult(quarterlyReport);
            }
            catch (Exception ex)
            {
                // Log the exception details (e.g., to a file or console)
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
