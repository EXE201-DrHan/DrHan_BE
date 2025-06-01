#nullable disable
namespace DrHan.Domain.Entities;

public class SystemConfig : BaseEntity
{
    public string ConfigKey { get; set; }

    public string ConfigValue { get; set; }

    public string ConfigDataType { get; set; }

    public string Description { get; set; }

    public bool? IsActive { get; set; }
    public virtual ICollection<SystemConfigDetail> SystemConfigDetails { get; set; } = new List<SystemConfigDetail>();
}