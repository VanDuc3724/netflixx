using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }

        public string? Name { get; set; }
        public DateTime? dateOfBirth { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? Password { get; set; }
  
        public string? Role { get; set; }

        public bool IsActive { get; set; } = true;
        public List<string> AvailableRoles { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Address { get; set; } = string.Empty;

      
    }
}
