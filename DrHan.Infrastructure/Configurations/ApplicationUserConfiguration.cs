using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrHan.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Indexing
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.FullName);
        builder.Property(u => u.Status)
            .HasConversion(
            convertToProviderExpression: v => v.ToString(),
            convertFromProviderExpression: v => (UserStatus)Enum.Parse(typeof(UserStatus), v))
            .HasDefaultValue(UserStatus.Enabled);
        // Properties
        builder.Property(u => u.CreatedAt).ValueGeneratedOnAdd().HasDefaultValueSql("getdate()");
        builder.Property(u => u.UpdatedAt).ValueGeneratedOnAddOrUpdate().HasDefaultValueSql("getdate()");
        builder.Property(u => u.ProfileImageUrl).HasColumnType("nvarchar(max)");
        builder.Property(u => u.FullName).HasMaxLength(256).IsUnicode();
    }
    
}