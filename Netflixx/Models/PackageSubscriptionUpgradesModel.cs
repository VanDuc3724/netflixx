using Netflixx.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class PackageSubscriptionUpgradesModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UpgradeID { get; set; }

    [Required]
    [ForeignKey("User")]
    public string UserID { get; set; }
    public AppUserModel User { get; set; }

    [Required]
    [ForeignKey("OldSubscription")]
    public int OldSubscriptionID { get; set; }
    public PackageSubscriptionsModel OldSubscription { get; set; }

    [Required]
    [ForeignKey("NewSubscription")]
    public int NewSubscriptionID { get; set; }
    public PackageSubscriptionsModel NewSubscription { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime UpgradeDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProratedCredit { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalCharge { get; set; }
}