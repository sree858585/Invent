using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Prefix
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(200)]
        public string Prefx { get; set; }

        public bool IsActive { get; set; }
    }
}
    