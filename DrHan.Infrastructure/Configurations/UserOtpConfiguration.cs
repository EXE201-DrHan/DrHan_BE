using DrHan.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DrHan.Infrastructure.Configurations;

public class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.ToTable("UserOtps");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired();
            
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(10);
            
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(20);
            
        builder.Property(x => x.ExpiresAt)
            .IsRequired();
            
        builder.Property(x => x.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.AttemptsCount)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(x => x.MaxAttempts)
            .IsRequired()
            .HasDefaultValue(3);
        
        // Ignore computed properties as they cannot be mapped to database columns
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsBlocked);
        
        // Configure relationship with ApplicationUser
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Create index for better query performance
        builder.HasIndex(x => new { x.UserId, x.Type, x.IsUsed, x.ExpiresAt })
            .HasDatabaseName("IX_UserOtps_UserId_Type_IsUsed_ExpiresAt");
    }
} 