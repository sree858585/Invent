using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class QuarterlyReport
    {
        public int Id { get; set; }

        public string UserId { get; set; } // Strings are nullable by default

        public int? Year { get; set; } // Nullable int

        public string Quarter { get; set; } // Strings are nullable by default

        public DateTime? DueDate { get; set; } // Nullable DateTime

        public DateTime? SubmissionDate { get; set; } // Nullable DateTime

        public DateTime? EditedDate { get; set; } // Nullable DateTime

        public string? FacilityName { get; set; }
        public string? CompletedBy { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Status { get; set; }

        
        public int? SyringesProvidedUnits { get; set; } // Nullable int

        public int? SyringesProvidedSessions { get; set; } // Nullable int

        public int? PharmacyVouchersUnits { get; set; } // Nullable int

        public int? PharmacyVouchersSessions { get; set; } // Nullable int

        public int? ReportedVouchersUnits { get; set; } // Nullable int

        public int? ReportedVouchersSessions { get; set; } // Nullable int

        public int? FitpacksProvidedUnits { get; set; } // Nullable int

        public int? FitpacksProvidedSessions { get; set; } // Nullable int

        public int? QuartContainersProvidedUnits { get; set; } // Nullable int

        public int? QuartContainersProvidedSessions { get; set; } // Nullable int

        public int? GallonContainersProvidedUnits { get; set; } // Nullable int

        public int? GallonContainersProvidedSessions { get; set; } // Nullable int

        public string? OtherSuccessesConcernsIssues { get; set; } // Nullable by default


        public List<CollectionDetail>? CollectionDetails { get; set; }
    }
}
