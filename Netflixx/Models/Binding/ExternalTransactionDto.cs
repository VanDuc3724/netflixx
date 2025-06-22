using System.Text.Json.Serialization;

namespace Netflixx.Models.Binding
{
    public class ExternalTransactionDto
    {
        [JsonPropertyName("Mã GD")]
        public int TransactionCode { get; set; }

        [JsonPropertyName("Mô tả")]
        public string? Description { get; set; }

        [JsonPropertyName("Giá trị")]
        public decimal Amount { get; set; }

        [JsonPropertyName("Ngày diễn ra")]
        public string? TransactionDate { get; set; }

        [JsonPropertyName("Số tài khoản")]
        public string? AccountNumber { get; set; }
    }

    public class ExternalTransactionResponse
    {
        [JsonPropertyName("data")]
        public List<ExternalTransactionDto>? Data { get; set; }

        [JsonPropertyName("error")]
        public bool Error { get; set; }
    }
}