#nullable disable
using DrHan.Domain.Entities.Users;

namespace DrHan.Domain.Entities.Families;

public class FamilyMember : BaseEntity
{
    public int? FamilyId { get; set; }
    public int? UserId { get; set; }
    public string Role { get; set; }

    public string Relationship { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual Family Family { get; set; }

    public virtual ICollection<FamilyMemberPermission> FamilyMemberPermissions { get; set; } =
        new List<FamilyMemberPermission>();

    public virtual ApplicationUser ApplicationUser { get; set; }
}