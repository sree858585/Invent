using System;
namespace WebApplication1.Models
{
    public class LkAgencyClassification
    {
        public int agency_classification_id { get; set; }
        public string classifcation_description { get; set; }
        public int sort_order { get; set; }
        public bool is_active { get; set; }
    }



}

