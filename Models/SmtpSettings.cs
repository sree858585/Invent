using System.ComponentModel.DataAnnotations;


namespace WebApplication1.Models
{
    public class SmtpSettings
    {
        public bool IsDevelopment { get; set; }
        public SmtpConfig DevSettings { get; set; }
        public SmtpConfig ProdSettings { get; set; }
    }

    public class SmtpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSSL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

}

