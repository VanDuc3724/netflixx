using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.Binding
{
    public class EmailRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email phải có định dạng example@domain.com")]
        public string Email { get; set; }
    }
}
