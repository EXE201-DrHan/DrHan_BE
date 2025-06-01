#nullable disable
namespace DrHan.Domain.Entities.Families;

public class FamilyMemberPermission : BaseEntity
{
    public int? FamilyMemberId { get; set; }
    public string PermissionType { get; set; }

    public bool? IsGranted { get; set; }

    public DateTime? GrantedAt { get; set; }

    public virtual FamilyMember FamilyMember { get; set; }
}