using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Product
    {
        [Key]
        public int product_id { get; set; }

        public int? product_agency_type { get; set; }

        [MaxLength(100)]
        public string product_item_num { get; set; }

        [MaxLength(1000)]
        public string product_description { get; set; }

        [MaxLength(100)]
        public string product_pieces_per_case { get; set; }

        public int? product_inventory_level { get; set; }

        public int? sort_order { get; set; }

        public bool is_active { get; set; } = true;
    }
}
