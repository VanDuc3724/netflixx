namespace Netflixx.Models.ViewModel
{
    public class BillHistoryItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AmountText { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}
