using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Suffix
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(200)]
        public string Sufix { get; set; }

        public bool IsActive { get; set; }
    }
}
