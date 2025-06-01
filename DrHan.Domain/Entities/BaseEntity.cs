namespace DrHan.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }
}