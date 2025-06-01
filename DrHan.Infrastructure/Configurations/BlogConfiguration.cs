using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Blogs;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => b.AuthorName);
        builder.HasIndex(b => b.IsActive);
    }
} 