namespace WebApplication1.Models
{
    public class ShipToSiteCounty
    {
        public int ShipToSiteId { get; set; }
        public ShipToSite ShipToSite { get; set; }

        public int CountyId { get; set; }
        public County County { get; set; }
    }
}
