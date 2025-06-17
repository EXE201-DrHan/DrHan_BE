using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Domain.Entities.Users
{
    public class SubscriptionUsage : BaseEntity
    {
        public int UserSubscriptionId { get; set; }
        public string FeatureType { get; set; }
        public int UsageCount { get; set; }
        public DateTime UsageDate { get; set; }
        public string ResourceUsed { get; set; } // Recipe ID, Image URL, etc.

        public virtual UserSubscription UserSubscription { get; set; }
    }
}
