using System.Runtime.Serialization;

namespace DrHan.Domain.Constants.Status
{
    public enum BlogStatus
    {
        [EnumMember(Value = "Draft")]
        Draft,
        [EnumMember(Value = "Published")]
        Published,
        [EnumMember(Value = "Archived")]
        Archived,
        [EnumMember(Value = "PendingReview")]
        PendingReview
    }
} 