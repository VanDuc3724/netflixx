using Microsoft.AspNetCore.Identity;

namespace Netflixx.Models
{
    public class AppUserModel : IdentityUser
    {
        // Các navigation properties
        public virtual UserAccountsModel Account { get; set; }
        public virtual ICollection<ChannelSubscriptionsModel> ChannelSubscriptions { get; set; }
        public virtual ICollection<FilmPurchasesModel> FilmPurchases { get; set; }
        public virtual ICollection<PackageSubscriptionsModel> PackageSubscriptions { get; set; }
        public virtual ICollection<PaymentTransactionsModel> PaymentTransactions { get; set; }
        public virtual ICollection<PointsTransactionsModel> PointsTransactions { get; set; }
        public virtual ICollection<PromotionUsageModel> PromotionUsages { get; set; }
        public virtual ICollection<PackageSubscriptionUpgradesModel> PackageSubscriptionUpgrades { get; set; } // Đổi tên từ PackageUpgrades

        // Các thuộc tính bổ sung
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; } = "/image/logo/avatar_default.jpg";


        public string? FavoriteGenres { get; set; }

        public string? About { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;


    }
}