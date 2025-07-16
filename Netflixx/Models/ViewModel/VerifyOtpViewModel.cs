using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Hãy nhập email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hãy nhập otp.")]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; }
    }
}
