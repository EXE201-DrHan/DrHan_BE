using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Blogs;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => b.AuthorName);
        builder.Property(b => b.Status)
            .HasConversion(
                v => v.ToString(),
                v => (DrHan.Domain.Constants.Status.BlogStatus)System.Enum.Parse(typeof(DrHan.Domain.Constants.Status.BlogStatus), v)
            )
            .HasDefaultValue(DrHan.Domain.Constants.Status.BlogStatus.Draft);
    }
} 