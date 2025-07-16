using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.Binding
{
    public class SendEmailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; } 
        public string UserId { get; set; }
    }
}
