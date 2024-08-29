using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class CollectionDetail
    {
        public int Id { get; set; }
        public string SharpsCollectionSite { get; set; }
        public DateTime CollectionDates { get; set; }
        public double PoundsCollected { get; set; }

        public int QuarterlyReportId { get; set; }
        public QuarterlyReport QuarterlyReport { get; set; }
    }
}
