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
        private readonly IConfiguration _configuration;

        public GeocodingService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Dictionary<string, decimal> FindLngLat(string p_address = "")
        {
            Dictionary<string, decimal> rtn = new Dictionary<string, decimal>();
            rtn.Clear();

            var requestUri = string.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false&key=AIzaSyCxCh6GeApSMORz4Ecf_BwbPb2HT0Nrc54", Uri.EscapeDataString(p_address));

            try
            {
                if (!String.IsNullOrEmpty(p_address) && p_address.Trim().Length > 1)
                {
                    WebRequest request = WebRequest.Create(requestUri);

                    // Use fully qualified System.Configuration.ConfigurationManager
                    if (System.Configuration.ConfigurationManager.AppSettings["IsDev"] == "true")
                    {
                        if (System.Configuration.ConfigurationManager.AppSettings["UID"] != null && System.Configuration.ConfigurationManager.AppSettings["UPSW"] != null)
                        {
                            request.Proxy = WebRequest.DefaultWebProxy;
                            IWebProxy proxy = new WebProxy(System.Configuration.ConfigurationManager.AppSettings["WEBPROXY"]);
                            proxy.Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["UID"], System.Configuration.ConfigurationManager.AppSettings["UPSW"]);
                            request.Proxy = proxy;
                        }
                    }

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    var xdoc = XDocument.Load(response.GetResponseStream());

                    if (xdoc.Element("GeocodeResponse").Element("status").Value.Trim() != "ZERO_RESULTS")
                    {
                        var result = xdoc.Element("GeocodeResponse").Element("result");
                        var locationElement = result.Element("geometry").Element("location");
                        var lat = locationElement.Element("lat");
                        var lng = locationElement.Element("lng");

                        decimal latitude = decimal.Parse(lat.Value);
                        decimal longitude = decimal.Parse(lng.Value);

                        rtn.Clear();
                        rtn.Add("Lat", latitude);
                        rtn.Add("Lng", longitude);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle logging if required
                throw new Exception("Error while fetching geocoding data", ex);
            }

            return rtn;
        }
    }
}
