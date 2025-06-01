#nullable disable
namespace DrHan.Domain.Entities.Blogs;

public class BlogImage : BaseEntity
{
    public int? BlogId { get; set; }
    public string ImageUrl { get; set; }

    public string ImageType { get; set; }

    public bool? IsPrimary { get; set; }

    public string AltText { get; set; }
    public virtual Blog Blog { get; set; }
}