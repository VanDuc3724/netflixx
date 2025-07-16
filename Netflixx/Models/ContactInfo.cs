using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class ContactInfo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Business hours are required")]
        public string BusinessHours { get; set; }

        [Required(ErrorMessage = "Map URL is required")]
        [Url(ErrorMessage = "Invalid URL")]
        public string MapEmbedUrl { get; set; }
    }
}