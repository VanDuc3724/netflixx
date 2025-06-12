using Netflixx.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductionManagerApp.Models;

namespace Netflixx.Repositories
{
    public class DBContext : IdentityDbContext<AppUserModel>
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }
        public virtual DbSet<BlogPost> BlogPosts { get; set; }
        public virtual DbSet<PaymentProvidersModel> PaymentProviders { get; set; }
        public virtual DbSet<PaymentEnvironmentsModel> PaymentEnvironments { get; set; }
        public virtual DbSet<PaymentTransactionsModel> PaymentTransactions { get; set; }
        public virtual DbSet<PackagesModel> Packages { get; set; }
        public virtual DbSet<ChannelsModel> Channels { get; set; }
        public virtual DbSet<PackageChannelsModel> PackageChannels { get; set; }
        public virtual DbSet<FilmsModel> Films { get; set; }
        public virtual DbSet<PackageFilmsModel> PackageFilms { get; set; }
        public virtual DbSet<PromotionsModel> Promotions { get; set; }
        public virtual DbSet<PromotionPackagesModel> PromotionPackages { get; set; }
        public virtual DbSet<PromotionFilmsModel> PromotionFilms { get; set; }
        public virtual DbSet<PromotionChannelsModel> PromotionChannels { get; set; }
        public virtual DbSet<PromotionUsageModel> PromotionUsages { get; set; }
        public virtual DbSet<FilmPurchasesModel> FilmPurchases { get; set; }
        public virtual DbSet<ChannelSubscriptionsModel> ChannelSubscriptions { get; set; }
        public virtual DbSet<PackageSubscriptionsModel> PackageSubscriptions { get; set; }
        public virtual DbSet<PackageSubscriptionUpgradesModel> PackageSubscriptionUpgrades { get; set; }
        public virtual DbSet<UserAccountsModel> UserAccounts { get; set; }
        public virtual DbSet<PointsTransactionsModel> PointsTransactions { get; set; }
        public virtual DbSet<DailyRevenueModel> DailyRevenue { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<ProductionManager> ProductionManagers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductionManager>()
                   .ToTable("ProductionManagers");
            modelBuilder.Entity<Country>()
                   .ToTable("Countries");
            modelBuilder.Entity<FilmsModel>()
       .HasOne(f => f.ProductionManager)           
       .WithMany(p => p.Films)                     
       .HasForeignKey(f => f.ProductionManagerId)  
       .OnDelete(DeleteBehavior.SetNull);         

            modelBuilder.Entity<FilmsModel>()
                .Property(f => f.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentTransactionsModel>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChannelSubscriptionsModel>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PackageSubscriptionsModel>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<UserAccountsModel>()
                .Property(u => u.Balance)
                .HasPrecision(18, 2);

            // Configure many-to-many relationships
            modelBuilder.Entity<PackageChannelsModel>()
                .HasKey(pc => new { pc.PackageID, pc.ChannelID });

            modelBuilder.Entity<PackageChannelsModel>()
                .HasOne(pc => pc.Package)
                .WithMany(p => p.PackageChannels)
                .HasForeignKey(pc => pc.PackageID);

            modelBuilder.Entity<PackageChannelsModel>()
                .HasOne(pc => pc.Channel)
                .WithMany(c => c.PackageChannels)
                .HasForeignKey(pc => pc.ChannelID);

            modelBuilder.Entity<PackageFilmsModel>()
                .HasKey(pf => new { pf.PackageID, pf.FilmID });

            modelBuilder.Entity<PackageFilmsModel>()
                .HasOne(pf => pf.Package)
                .WithMany(p => p.PackageFilms)
                .HasForeignKey(pf => pf.PackageID);

            modelBuilder.Entity<PackageFilmsModel>()
                .HasOne(pf => pf.Film)
                .WithMany(f => f.PackageFilms)
                .HasForeignKey(pf => pf.FilmID);

            modelBuilder.Entity<PromotionPackagesModel>()
                .HasKey(pp => new { pp.PromotionID, pp.PackageID });

            modelBuilder.Entity<PromotionFilmsModel>()
                .HasKey(pf => new { pf.PromotionID, pf.FilmID });

            modelBuilder.Entity<PromotionChannelsModel>()
                .HasKey(pc => new { pc.PromotionID, pc.ChannelID });

            // Configure relationships with AppUserModel
            modelBuilder.Entity<PaymentTransactionsModel>()
                .HasOne(pt => pt.User)
                .WithMany(u => u.PaymentTransactions)
                .HasForeignKey(pt => pt.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FilmPurchasesModel>()
                .HasOne(fp => fp.User)
                .WithMany(u => u.FilmPurchases)
                .HasForeignKey(fp => fp.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChannelSubscriptionsModel>()
                .HasOne(cs => cs.User)
                .WithMany(u => u.ChannelSubscriptions)
                .HasForeignKey(cs => cs.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageSubscriptionsModel>()
                .HasOne(ps => ps.User)
                .WithMany(u => u.PackageSubscriptions)
                .HasForeignKey(ps => ps.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PromotionUsageModel>()
                .HasOne(pu => pu.User)
                .WithMany(u => u.PromotionUsages)
                .HasForeignKey(pu => pu.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PointsTransactionsModel>()
                .HasOne(pt => pt.User)
                .WithMany(u => u.PointsTransactions)
                .HasForeignKey(pt => pt.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure 1-1 relationship between AppUserModel and UserAccounts
            modelBuilder.Entity<UserAccountsModel>()
                .HasOne(ua => ua.User)
                .WithOne(u => u.Account)
                .HasForeignKey<UserAccountsModel>(ua => ua.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure PackageSubscriptionUpgrades
            modelBuilder.Entity<PackageSubscriptionUpgradesModel>()
                .HasOne(pu => pu.OldSubscription)
                .WithMany(ps => ps.UpgradesFrom)
                .HasForeignKey(pu => pu.OldSubscriptionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageSubscriptionUpgradesModel>()
                .HasOne(pu => pu.NewSubscription)
                .WithMany(ps => ps.UpgradesTo)
                .HasForeignKey(pu => pu.NewSubscriptionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageSubscriptionUpgradesModel>()
                .HasOne(pu => pu.User)
                .WithMany(u => u.PackageSubscriptionUpgrades)
                .HasForeignKey(pu => pu.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Indexes
            modelBuilder.Entity<FilmPurchasesModel>()
                .HasIndex(fp => new { fp.UserID, fp.PurchaseDate });

            modelBuilder.Entity<PointsTransactionsModel>()
                .HasIndex(pt => new { pt.UserID, pt.TransactionDate });

            // Configure Check Constraints
            modelBuilder.Entity<ChannelSubscriptionsModel>()
                .HasCheckConstraint("CK_ChannelSubscriptions_Dates", "EndDate >= StartDate");

            modelBuilder.Entity<PackageSubscriptionsModel>()
                .HasCheckConstraint("CK_PackageSubscriptions_Dates", "EndDate >= StartDate");
        }
    }
}