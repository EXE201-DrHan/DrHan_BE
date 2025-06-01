using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Users;

public class UserAllergyConfiguration : IEntityTypeConfiguration<UserAllergy>
{
    public void Configure(EntityTypeBuilder<UserAllergy> builder)
    {
        builder.HasIndex(ua => new { ua.UserId, ua.AllergenId });
    }
} 