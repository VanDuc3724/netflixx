using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "Hãy nhập username.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Hãy nhập họ tên.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Hãy nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hãy nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
        public string Password { get; set; }

        [Required(ErrorMessage = "OTP là bắt buộc")]
        public string Otp { get; set; }
    }

}
