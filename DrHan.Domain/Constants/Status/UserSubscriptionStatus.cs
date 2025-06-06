using System.Runtime.Serialization;

namespace DrHan.Domain.Constants.Status
{
    public enum UserSubscriptionStatus
    {
        [EnumMember(Value = "Active")]
        Active,
        [EnumMember(Value = "Inactive")]
        Inactive,
        [EnumMember(Value = "Cancelled")]
        Cancelled,
        [EnumMember(Value = "Expired")]
        Expired,
        [EnumMember(Value = "Pending")]
        Pending
    }
} 