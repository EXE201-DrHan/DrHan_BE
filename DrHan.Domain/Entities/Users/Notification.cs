#nullable disable
namespace DrHan.Domain.Entities.Users;

public class Notification : BaseEntity
{
    public int? UserId { get; set; }
    public string Type { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public bool? SendViaApp { get; set; }

    public bool? SendViaEmail { get; set; }

    public bool? SendViaSms { get; set; }

    public bool? SendViaPush { get; set; }

    public DateTime? ScheduledFor { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string ActionUrl { get; set; }

    public string ActionType { get; set; }
    public int? ActionReferenceId { get; set; }
    public bool? IsRead { get; set; }

    public bool? IsDelivered { get; set; }

    public int? DeliveryAttempts { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}