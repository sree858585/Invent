using System;

namespace WebApplication1.Models
{
    public class CollectionDetail
    {
        public int Id { get; set; }
        public string SharpsCollectionSite { get; set; }
        public DateTime? CollectionDates { get; set; } // Nullable DateTime
        public double? PoundsCollected { get; set; } // Nullable double

        public int QuarterlyReportId { get; set; }
        public QuarterlyReport QuarterlyReport { get; set; }
    }
}
