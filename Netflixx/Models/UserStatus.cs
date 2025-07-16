using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Netflixx.Models
{
    public enum UserStatusCode
    {
        Active,
        Locked,
        PermanentLocked,
        OffLine
    }
    public class UserStatus
    {
        public string UserID { get; set; }
        public  UserStatusCode Code { get; set; }
        public string Text { get; set; }
        public DateTime? LastOnTime { get; set; }
        public DateTimeOffset? LockEndDate { get; set; }

      

    }
}
