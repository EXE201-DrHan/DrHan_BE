#nullable disable
using DrHan.Domain.Constants.Status;
using DrHan.Domain.Entities.Families;
using DrHan.Domain.Entities.MealPlans;
using Microsoft.AspNetCore.Identity;

namespace DrHan.Domain.Entities.Users;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string ProfileImageUrl { get; set; }
    public string? SubscriptionTier { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string? RefreshToken {  get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public UserStatus Status {  get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual ICollection<Family> Families { get; set; } = new List<Family>();
    public virtual ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<ScanHistory> ScanHistories { get; set; } = new List<ScanHistory>();
    public virtual ICollection<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
    public virtual ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}