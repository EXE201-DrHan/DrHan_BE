using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Users;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.HasIndex(us => us.UserId);
        builder.HasIndex(us => us.PlanId);
        builder.Property(us => us.Status)
            .HasConversion(
                v => v.ToString(),
                v => (DrHan.Domain.Constants.Status.UserSubscriptionStatus)System.Enum.Parse(typeof(DrHan.Domain.Constants.Status.UserSubscriptionStatus), v)
            )
            .HasDefaultValue(DrHan.Domain.Constants.Status.UserSubscriptionStatus.Active);
    }
} 