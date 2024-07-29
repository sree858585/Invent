using WebApplication1.Models;

public class RegistrationApprovalViewModel
{
    public int Id { get; set; }
    public string AgencyName { get; set; }
    public string AlternateName { get; set; }
    public string County { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string RegistrationType { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; }
    public string UniqueId { get; set; }  // Add this line
    public AgencyContact AgencyContact { get; set; }
    public List<AdditionalUser> AdditionalUsers { get; set; }
    public ShipInformation ShipInformation { get; set; }
    public List<ShipToSite> AdditionalShipToSites { get; set; }
    public List<string> CountiesServed { get; set; }
}
