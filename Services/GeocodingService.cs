using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; 


namespace WebApplication1.Services
{
    public class GeocodingService
    {
        private readonly string _googleApiKey;

        public GeocodingService(IConfiguration configuration)
        {
            _googleApiKey = configuration["GoogleApiKey"];
        }

        public async Task<(decimal? lat, decimal? lng)> GetCoordinatesAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return (null, null);

            var requestUri = $"https://maps.googleapis.com/maps/api/geocode/xml?address={Uri.EscapeDataString(address)}&sensor=false&key={_googleApiKey}";

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var xmlContent = await response.Content.ReadAsStringAsync();
                    var xdoc = XDocument.Parse(xmlContent);

                    if (xdoc.Element("GeocodeResponse").Element("status").Value != "ZERO_RESULTS")
                    {
                        var result = xdoc.Element("GeocodeResponse").Element("result");
                        var locationElement = result.Element("geometry").Element("location");
                        var lat = locationElement.Element("lat");
                        var lng = locationElement.Element("lng");

                        return (decimal.Parse(lat.Value), decimal.Parse(lng.Value));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
              //  _logger.LogError(ex, "HTTP error occurred while contacting the Geocoding API.");
                throw; // Re-throwing to let it bubble up if necessary
            }
            catch (XmlException ex)
            {
               // _logger.LogError(ex, "Failed to parse XML response from Geocoding API.");
                throw; // Re-throwing to ensure proper exception propagation
            }
            catch (FormatException ex)
            {
               // _logger.LogError(ex, "Invalid format found in the Geocoding API response.");
                throw; // Re-throwing to handle this case specifically
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "An unexpected error occurred while fetching geocoding data.");
                // Optionally, you can throw or handle the exception based on your needs.
                throw;
            }


            return (null, null);
        }
    }
}

