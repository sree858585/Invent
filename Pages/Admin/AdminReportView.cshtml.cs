using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CsvHelper;
using System.Globalization;
using System.IO;

namespace WebApplication1.Pages.Admin
{
    public class AdminReportViewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminReportViewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<int> Years { get; set; }
        public List<QuarterViewModel> Quarters { get; set; }
        public List<QuarterlyReport> Reports { get; set; } // Store search results
        public bool SearchPerformed { get; set; } = false;

        public void OnGet()
        {
            // Initialize Years for the view
            var currentYear = DateTime.Now.Year;
            var startYear = 2024;

            Years = Enumerable.Range(startYear, currentYear - startYear + 1).ToList();

            // Initialize Quarters for the view
            Quarters = new List<QuarterViewModel>
    {
        new QuarterViewModel { QuarterName = "Q1", StartMonth = 1, EndMonth = 3, DueDate = new DateTime(currentYear, 4, 15), Year = currentYear },
        new QuarterViewModel { QuarterName = "Q2", StartMonth = 4, EndMonth = 6, DueDate = new DateTime(currentYear, 7, 15), Year = currentYear },
        new QuarterViewModel { QuarterName = "Q3", StartMonth = 7, EndMonth = 9, DueDate = new DateTime(currentYear, 10, 15), Year = currentYear },
        new QuarterViewModel { QuarterName = "Q4", StartMonth = 10, EndMonth = 12, DueDate = new DateTime(currentYear + 1, 1, 15), Year = currentYear }
    };

            // Filter the quarters to show current and upcoming quarters
            var currentQuarter = (DateTime.Now.Month - 1) / 3 + 1;
            Quarters = Quarters.Where(q => q.StartMonth >= (currentQuarter - 1) * 3 + 1).ToList();

            // Set Reports to null initially so that nothing is displayed before search
            Reports = null;
        }



        public async Task<IActionResult> OnPostDownloadReportAsync(string quarter)
        {
            var currentYear = DateTime.Now.Year;

            var reports = await _context.QuarterlyReports
                .Where(q => q.Year == currentYear && q.Quarter == quarter)
                .Include(q => q.CollectionDetails)
                .ToListAsync();

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

                foreach (var report in reports)
                {
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

                    csv.NextRecord();
                }

                writer.Flush();
                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "text/csv", $"QuarterlyReport_{quarter}_{currentYear}.csv");
            }
        }

        // New POST method to handle search
        public async Task<IActionResult> OnPostAsync(string searchTerm)
        {
            SearchPerformed = true;

            if (string.IsNullOrEmpty(searchTerm))
            {
                ModelState.AddModelError(string.Empty, "Please provide a search term.");
                return Page();
            }

            // Search by Report ID (numeric) or user email (from ApplicationUser)
            if (int.TryParse(searchTerm, out int reportId))
            {
                // Search by report ID, include CollectionDetails
                Reports = await _context.QuarterlyReports
                    .Include(r => r.CollectionDetails) // Include CollectionDetails
                    .Include(r => r.User) // Include user email
                    .Where(r => r.Id == reportId)
                    .ToListAsync();
            }
            else
            {
                // Search by user email (case-insensitive), include CollectionDetails
                Reports = await _context.QuarterlyReports
                    .Include(r => r.CollectionDetails) // Include CollectionDetails
                    .Include(r => r.User) // Include user email
                    .Where(r => r.User.Email.ToLower() == searchTerm.ToLower())
                    .ToListAsync();
            }

            return Page();
        }



        public async Task<IActionResult> OnPostSearchAsync(string searchTerm)
        {
            SearchPerformed = true;

            if (string.IsNullOrEmpty(searchTerm))
            {
                ModelState.AddModelError(string.Empty, "Please provide a search term.");
                return Page();
            }
            // Initialize the Years to avoid null reference errors
            var currentYear = DateTime.Now.Year;
            var startYear = 2024; // Change this if needed
            Years = Enumerable.Range(startYear, currentYear - startYear + 1).ToList();


            // Search by Report ID (numeric) or by user email (fetching from ApplicationUser)
            if (int.TryParse(searchTerm, out int reportId))
            {
                // Search by report ID
                Reports = await _context.QuarterlyReports
                    .Include(qr => qr.User) // Include the User to access email
                    .Where(qr => qr.Id == reportId)
                    .ToListAsync();
            }
            else
            {
                // Search by user email (case-insensitive)
                Reports = await _context.QuarterlyReports
                    .Include(qr => qr.User) // Include the User to access email
                    .Where(qr => qr.User.Email.ToLower() == searchTerm.ToLower()) // Filter by email
                    .ToListAsync();
            }

            return Page();
        }


        public async Task<IActionResult> OnGetLoadReportAsync(int id)
        {
            var report = await _context.QuarterlyReports
                .Include(r => r.CollectionDetails) // Include CollectionDetails
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                id = report.Id,
                facilityName = report.FacilityName,
                completedBy = report.CompletedBy,
                address = report.Address,
                phone = report.Phone,
                fax = report.Fax,
                status = report.Status,
                syringesProvidedUnits = report.SyringesProvidedUnits,
                syringesProvidedSessions = report.SyringesProvidedSessions,
                pharmacyVouchersUnits = report.PharmacyVouchersUnits,
                pharmacyVouchersSessions = report.PharmacyVouchersSessions,
                reportedVouchersUnits = report.ReportedVouchersUnits,
                reportedVouchersSessions = report.ReportedVouchersSessions,
                fitpacksProvidedUnits = report.FitpacksProvidedUnits,
                fitpacksProvidedSessions = report.FitpacksProvidedSessions,
                quartContainersProvidedUnits = report.QuartContainersProvidedUnits,
                quartContainersProvidedSessions = report.QuartContainersProvidedSessions,
                gallonContainersProvidedUnits = report.GallonContainersProvidedUnits,
                gallonContainersProvidedSessions = report.GallonContainersProvidedSessions,
                otherSuccessesConcernsIssues = report.OtherSuccessesConcernsIssues,
                // Add Collection Details
                collectionDetails = report.CollectionDetails.Select(cd => new
                {
                    cd.SharpsCollectionSite,
                    cd.CollectionDates,
                    cd.PoundsCollected
                }).ToList()
            });
        }




        public async Task<IActionResult> OnPostEditReportAsync(int reportId, QuarterlyReport updatedReport)
        {
            var report = await _context.QuarterlyReports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound("Report not found.");
            }

            // Update the report with the new values
            report.FacilityName = updatedReport.FacilityName;
            report.CompletedBy = updatedReport.CompletedBy;
            report.Address = updatedReport.Address;
            report.Phone = updatedReport.Phone;
            report.Fax = updatedReport.Fax;
            report.Status = updatedReport.Status;
            report.SyringesProvidedUnits = updatedReport.SyringesProvidedUnits;
            report.SyringesProvidedSessions = updatedReport.SyringesProvidedSessions;
            report.PharmacyVouchersUnits = updatedReport.PharmacyVouchersUnits;
            report.PharmacyVouchersSessions = updatedReport.PharmacyVouchersSessions;
            report.ReportedVouchersUnits = updatedReport.ReportedVouchersUnits;
            report.ReportedVouchersSessions = updatedReport.ReportedVouchersSessions;
            report.FitpacksProvidedUnits = updatedReport.FitpacksProvidedUnits;
            report.FitpacksProvidedSessions = updatedReport.FitpacksProvidedSessions;
            report.QuartContainersProvidedUnits = updatedReport.QuartContainersProvidedUnits;
            report.QuartContainersProvidedSessions = updatedReport.QuartContainersProvidedSessions;
            report.GallonContainersProvidedUnits = updatedReport.GallonContainersProvidedUnits;
            report.GallonContainersProvidedSessions = updatedReport.GallonContainersProvidedSessions;
            report.OtherSuccessesConcernsIssues = updatedReport.OtherSuccessesConcernsIssues;

            report.EditedDate = DateTime.Now;

            //report.CollectionDetails.Clear();  // Remove existing details
            //foreach (var detail in updatedReport.CollectionDetails)
            //{
            //    report.CollectionDetails.Add(new CollectionDetail
            //    {
            //        SharpsCollectionSite = detail.SharpsCollectionSite,
            //        CollectionDates = detail.CollectionDates,
            //        PoundsCollected = detail.PoundsCollected
            //    });
            //}
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/AdminReportView", new { success = true });
        }



        public class QuarterViewModel
        {
            public string QuarterName { get; set; }
            public int StartMonth { get; set; }
            public int EndMonth { get; set; }
            public DateTime DueDate { get; set; }
            public int Year { get; set; }
        }
    }
}
