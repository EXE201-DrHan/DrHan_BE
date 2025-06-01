#nullable disable
using DrHan.Domain.Entities.FoodProducts;

namespace DrHan.Domain.Entities.Users;

public class UserActivity : BaseEntity
{
    public int? UserId { get; set; }
    public string ActivityType { get; set; }

    public string SessionId { get; set; }

    public string DeviceType { get; set; }

    public string AppVersion { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }
    public int? ProductId { get; set; }
    public string SearchQuery { get; set; }

    public int? ResultCount { get; set; }
    public virtual FoodProduct Product { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}