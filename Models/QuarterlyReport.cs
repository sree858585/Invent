using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class QuarterlyReport
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? Year { get; set; }  // Nullable int

        public string? Quarter { get; set; }  // Nullable string

        public DateTime? DueDate { get; set; }  // Nullable DateTime

        public DateTime? SubmissionDate { get; set; }  // Nullable DateTime

        public DateTime? EditedDate { get; set; }  // Nullable DateTime

        [Required]
        public string FacilityName { get; set; }

        [Required]
        public string CompletedBy { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Phone { get; set; }

        public string? Fax { get; set; }  // Nullable string

        public string? SharpsCollectionSite { get; set; }  // Nullable string

        public DateTime? CollectionDates { get; set; }  // Nullable DateTime

        public double? PoundsCollected { get; set; }  // Nullable double

        public int? SyringesProvidedUnits { get; set; }  // Nullable int

        public int? SyringesProvidedSessions { get; set; }  // Nullable int

        public int? PharmacyVouchersUnits { get; set; }  // Nullable int

        public int? PharmacyVouchersSessions { get; set; }  // Nullable int

        public int? ReportedVouchersUnits { get; set; }  // Nullable int

        public int? ReportedVouchersSessions { get; set; }  // Nullable int

        public int? FitpacksProvidedUnits { get; set; }  // Nullable int

        public int? FitpacksProvidedSessions { get; set; }  // Nullable int

        public int? QuartContainersProvidedUnits { get; set; }  // Nullable int

        public int? QuartContainersProvidedSessions { get; set; }  // Nullable int

        public int? GallonContainersProvidedUnits { get; set; }  // Nullable int

        public int? GallonContainersProvidedSessions { get; set; }  // Nullable int

        public string? OtherSuccessesConcernsIssues { get; set; }  // Nullable string

        [Required]
        public string Status { get; set; }

        public List<CollectionDetail>? CollectionDetails { get; set; } = new List<CollectionDetail>();
    }

}
