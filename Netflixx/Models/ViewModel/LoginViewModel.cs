using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Hãy nhập username.")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Hãy nhập password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; } // Cho phép null
    }

}
