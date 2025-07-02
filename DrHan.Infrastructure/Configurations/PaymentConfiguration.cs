using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Users;
using DrHan.Domain.Constants.Status;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.PaymentStatus)
            .HasConversion(
                v => v.ToString(),
                v => (PaymentStatus)System.Enum.Parse(typeof(PaymentStatus), v)
            )
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.PaymentMethod)
            .HasConversion(
                v => v.ToString(),
                v => (PaymentMethod)System.Enum.Parse(typeof(PaymentMethod), v)
            )
            .HasDefaultValue(PaymentMethod.PAYOS);

        builder.HasIndex(p => p.TransactionId);
        builder.HasIndex(p => p.UserSubscriptionId);

        // Explicit foreign key configuration
        builder.HasOne(p => p.UserSubscription)
            .WithMany()
            .HasForeignKey(p => p.UserSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
} 