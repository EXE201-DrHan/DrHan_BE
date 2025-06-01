#nullable disable
namespace DrHan.Domain.Entities;

public class SystemConfigDetail : BaseEntity
{
    public int? ConfigId { get; set; }
    public string DetailKey { get; set; }

    public string DetailValue { get; set; }

    public string DetailDataType { get; set; }
    public virtual SystemConfig Config { get; set; }
}