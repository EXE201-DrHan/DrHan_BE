#nullable disable
using DrHan.Domain.Entities.FoodProducts;

namespace DrHan.Domain.Entities.Users;

public class ScanHistory : BaseEntity
{
    public int? UserId { get; set; }
    public string ScanType { get; set; }
    public int? ProductId { get; set; }
    public decimal? LocationLatitude { get; set; }

    public decimal? LocationLongitude { get; set; }

    public string LocationName { get; set; }

    public string ScanContext { get; set; }

    public string SafetyResult { get; set; }

    public string UserAction { get; set; }

    public DateTime? ScannedAt { get; set; }

    public virtual FoodProduct Product { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}