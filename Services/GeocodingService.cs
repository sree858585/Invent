// Explicitly refer to System.Configuration.ConfigurationManager
using System;
using System.Net;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration; // For IConfiguration

namespace WebApplication1.Services
{
    public class GeocodingService
{
    private readonly string _googleApiKey;

    public GeocodingService(string googleApiKey)
    {
        _googleApiKey = googleApiKey;
    }

    public async Task<Dictionary<string, decimal>> FindLngLatAsync(string address)
    {
        Dictionary<string, decimal> rtn = new Dictionary<string, decimal>();
        rtn.Clear();

        // Ensure TLS 1.2 is used
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        // Disable SSL validation (for development environments only)
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        var requestUri = $"https://maps.googleapis.com/maps/api/geocode/xml?address={Uri.EscapeDataString(address)}&sensor=false&key={_googleApiKey}";

        try
        {
            using (HttpClient client = new HttpClient(handler))
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var xmlContent = await response.Content.ReadAsStringAsync();
                    var xdoc = XDocument.Parse(xmlContent);

                    if (xdoc.Element("GeocodeResponse").Element("status").Value.ToString().Trim() != "ZERO_RESULTS")
                    {
                        var result = xdoc.Element("GeocodeResponse").Element("result");
                        var locationElement = result.Element("geometry").Element("location");
                        var lat = locationElement.Element("lat");
                        var lng = locationElement.Element("lng");

                        rtn.Add("Lat", decimal.Parse(lat.Value));
                        rtn.Add("Lng", decimal.Parse(lng.Value));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (Elmah or other logging system)
            Console.WriteLine("Error: " + ex.Message);
        }

        return rtn;
    }
}
}
