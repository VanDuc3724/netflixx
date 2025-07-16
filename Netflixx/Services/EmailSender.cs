using System.Net;
using System.Net.Mail;

namespace Netflixx.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlMessageBody) // Đổi tên 'message' thành 'htmlMessageBody' để rõ ràng hơn
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            // Lấy thông tin người gửi từ cấu hình
            string fromEmailAddress = smtpSettings["FromEmail"];
            string fromDisplayName = smtpSettings["FromName"]; // Thêm FromName vào config nếu muốn hiển thị tên

        
            var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"])
            };

            // Tạo MailMessage và set IsBodyHtml = true
            var mailMessage = new MailMessage
            {
                // Sử dụng FromName nếu có, nếu không thì chỉ dùng FromEmail
                From = string.IsNullOrEmpty(fromDisplayName)
                           ? new MailAddress(fromEmailAddress)
                           : new MailAddress(fromEmailAddress, fromDisplayName),
                Subject = subject,
                Body = htmlMessageBody, // Nội dung HTML của bạn
                IsBodyHtml = true      // <<< DÒNG QUAN TRỌNG ĐỂ SỬA LỖI
            };
            mailMessage.To.Add(toEmail); // Thêm người nhận

        
            return client.SendMailAsync(mailMessage);
        }
    }
}
