using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DrHan.Domain.Entities.Users;

public class ScanHistoryConfiguration : IEntityTypeConfiguration<ScanHistory>
{
    public void Configure(EntityTypeBuilder<ScanHistory> builder)
    {
        builder.HasIndex(sh => sh.UserId);
        builder.HasIndex(sh => sh.ProductId);
    }
} 