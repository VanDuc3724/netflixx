using System.Security.Cryptography;
using System.Text;

namespace Netflixx.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _config;

        public VNPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(decimal amount, string orderInfo, string returnUrl)
        {
            var baseUrl = _config["VNPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var tmnCode = _config["VNPay:TmnCode"] ?? "DEMO";
            var hashSecret = _config["VNPay:HashSecret"] ?? "SECRET";
            var dict = new SortedDictionary<string, string>
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", tmnCode},
                {"vnp_Amount", ((int)(amount * 100)).ToString()},
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", "VND"},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_Locale", "vn"},
                {"vnp_OrderInfo", orderInfo},
                {"vnp_OrderType", "other"},
                {"vnp_ReturnUrl", returnUrl},
                {"vnp_TxnRef", DateTime.Now.Ticks.ToString()}
            };

            var query = string.Join("&", dict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var signData = string.Join("&", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signData));
            var signature = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            return $"{baseUrl}?{query}&vnp_SecureHash={signature}";
        }
    }
}
