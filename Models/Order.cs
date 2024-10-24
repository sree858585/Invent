﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? AdditionalUserId { get; set; } // Nullable field for additional user

        [ForeignKey("AdditionalUserId")]
        public AdditionalUser? AdditionalUser { get; set; } // Navigation property made nullable

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string OrderStatus { get; set; } // Can be "ordered", "approved", "cancelled" or "shipped"

        public DateTime? ApprovedDate { get; set; }

        public DateTime? CanceledDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        [Required]
        public string ShipToName { get; set; }

        [Required]
        public string ShipToEmail { get; set; }

        [Required]
        public string ShipToAddress { get; set; }

        public string ShipToAddress2 { get; set; }

        [Required]  
        public string ShipToCity { get; set; }

        [Required]
        public string ShipToState { get; set; }

        [Required]
        public string ShipToZip { get; set; }

        // New fields
        public DateTime? EditedDate { get; set; }
        public string? Note { get; set; } // Nullable string for note

        // New fields for latitude and longitude
        public decimal? Lat { get; set; } // Nullable decimal for latitude
        public decimal? Lng { get; set; } // Nullable decimal for longitude


        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
