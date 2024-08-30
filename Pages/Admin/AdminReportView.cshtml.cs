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

        public void OnGet()
        {
            var currentYear = DateTime.Now.Year;
            var currentQuarter = (DateTime.Now.Month - 1) / 3 + 1;

            Years = Enumerable.Range(2024, currentYear - 2024 + 1).ToList();

            Quarters = new List<QuarterViewModel>
            {
                new QuarterViewModel { QuarterName = "Q1", StartMonth = 1, EndMonth = 3, DueDate = new DateTime(currentYear, 4, 15), Year = currentYear },
                new QuarterViewModel { QuarterName = "Q2", StartMonth = 4, EndMonth = 6, DueDate = new DateTime(currentYear, 7, 15), Year = currentYear },
                new QuarterViewModel { QuarterName = "Q3", StartMonth = 7, EndMonth = 9, DueDate = new DateTime(currentYear, 10, 15), Year = currentYear },
                new QuarterViewModel { QuarterName = "Q4", StartMonth = 10, EndMonth = 12, DueDate = new DateTime(currentYear + 1, 1, 15), Year = currentYear }
            };

            // Filter the quarters to only show the current and upcoming ones dynamically
            Quarters = Quarters.Where(q => q.StartMonth >= (currentQuarter - 1) * 3 + 1).ToList();
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
