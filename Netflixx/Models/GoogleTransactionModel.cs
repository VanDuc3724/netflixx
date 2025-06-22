using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Netflixx.Models
{
    public class GoogleTransactionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [JsonPropertyName("Mã GD")]
        public long TransactionCode { get; set; }

        [JsonPropertyName("Mô tả")]
        public string Description { get; set; }

        [JsonPropertyName("Giá trị")]
        public decimal Amount { get; set; }

        [JsonPropertyName("Ngày diễn ra")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("Số tài khoản")]
        public string AccountNumber { get; set; }
    }
}
