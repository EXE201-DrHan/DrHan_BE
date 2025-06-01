#nullable disable
namespace DrHan.Domain.Entities.Blogs;

public class BlogTag : BaseEntity
{
    public int? BlogId { get; set; }
    public string TagName { get; set; }
    public virtual Blog Blog { get; set; }
}