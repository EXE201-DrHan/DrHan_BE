#nullable disable
namespace DrHan.Domain.Entities.Blogs;
using DrHan.Domain.Constants.Status;

public class Blog : BaseEntity
{
    public string Title { get; set; }

    public string Content { get; set; }

    public string Summary { get; set; }

    public string Slug { get; set; }

    public string AuthorName { get; set; }

    public BlogStatus Status { get; set; }

    public bool? IsFeatured { get; set; }

    public int? ViewCount { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<BlogImage> BlogImages { get; set; } = new List<BlogImage>();

    public virtual ICollection<BlogTag> BlogTags { get; set; } = new List<BlogTag>();
}