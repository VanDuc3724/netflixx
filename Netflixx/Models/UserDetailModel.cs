using AspNetCoreGeneratedDocument;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Netflixx.Models
{
 
    public class UserDetailModel

    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime RegisterAt { get; set; }
        public int MovieRateCount { get; set; }
        public UserStatus UserStatus { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public List<BlogPost>? Blogs { get; set; }
        public List<FavoriteFilmsModel>? FavoriteFilms { get; set; }

        public List<string> AvailableRoles { get; set; } = new List<string> { "User", "Manager","Admin" };

    }
}
