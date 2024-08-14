using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; }

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

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
