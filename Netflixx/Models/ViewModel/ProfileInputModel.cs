// Models/ViewModel/ProfileInputModel.cs
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class ProfileInputModel
    {
        [Required] public string FullName { get; set; }
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9,10}$",
            ErrorMessage = "Phone number must start with 0 and be 10–11 digits.")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(1000,
            ErrorMessage = "About cannot exceed 1,000 characters.")]
        public string? Address { get; set; }

        // avatar upload
        public IFormFile? AvatarFile { get; set; }
        // existing URL so we can display it if user doesn't upload
        public string? AvatarUrl { get; set; }

        // genres you love
        public List<string>? Genres { get; set; } = new List<string>();

        public string? PackageSubscriptions { get; set; }

        public string? FilmPurchases { get; set; }

        public string? About { get; set; }
    }
}
