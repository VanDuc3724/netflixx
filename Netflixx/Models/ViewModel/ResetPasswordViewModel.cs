using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Hãy nhập email.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }
    }
}
