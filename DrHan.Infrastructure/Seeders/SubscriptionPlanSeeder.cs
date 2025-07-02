using DrHan.Domain.Entities.Users;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Seeders
{
    public static class SubscriptionPlanSeeder
    {
        public static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting subscription plan seeding...");

                // Check if plans already exist
                if (await context.SubscriptionPlans.AnyAsync())
                {
                    logger?.LogInformation("Subscription plans already exist, skipping seeding.");
                    return;
                }

                // Define subscription plans with their features
                var subscriptionPlans = new List<(string Name, string Description, decimal Price, string Currency, string BillingCycle, int? UsageQuota, bool IsActive, List<(string FeatureName, string Description, bool IsEnabled)> Features)>
                {
                    // Free Plan
                    ("Free", "Basic features for getting started", 0m, "USD", "Monthly", 50, true, new List<(string, string, bool)>
                    {
                        ("Recipe Search", "Search through basic recipe database", true),
                        ("Basic Meal Planning", "Create simple meal plans", true),
                        ("Allergen Alerts", "Basic allergen detection", true),
                        ("Profile Management", "Manage user profile and preferences", true),
                        ("Community Access", "Access to community features", false),
                        ("Advanced Analytics", "Detailed nutrition analytics", false),
                        ("Premium Support", "Priority customer support", false),
                        ("Custom Recipes", "Create and save custom recipes", false)
                    }),

                    // Basic Plan
                    ("Basic", "Enhanced features for regular users", 9.99m, "USD", "Monthly", 200, true, new List<(string, string, bool)>
                    {
                        ("Recipe Search", "Search through comprehensive recipe database", true),
                        ("Basic Meal Planning", "Create and customize meal plans", true),
                        ("Allergen Alerts", "Advanced allergen detection and warnings", true),
                        ("Profile Management", "Manage user profile and preferences", true),
                        ("Community Access", "Access to community features and forums", true),
                        ("Shopping Lists", "Generate shopping lists from meal plans", true),
                        ("Nutrition Tracking", "Track daily nutrition intake", true),
                        ("Advanced Analytics", "Detailed nutrition analytics", false),
                        ("Premium Support", "Priority customer support", false),
                        ("Custom Recipes", "Create and save unlimited custom recipes", true)
                    }),

                    // Premium Plan
                    ("Premium", "Full access to all features", 19.99m, "USD", "Monthly", 1000, true, new List<(string, string, bool)>
                    {
                        ("Recipe Search", "Search through comprehensive recipe database with AI recommendations", true),
                        ("Basic Meal Planning", "Create, customize and share meal plans", true),
                        ("Allergen Alerts", "Advanced allergen detection with severity levels", true),
                        ("Profile Management", "Comprehensive profile with health tracking", true),
                        ("Community Access", "Premium community features and expert access", true),
                        ("Shopping Lists", "Smart shopping lists with price tracking", true),
                        ("Nutrition Tracking", "Advanced nutrition tracking with goals", true),
                        ("Advanced Analytics", "Detailed nutrition analytics and reports", true),
                        ("Premium Support", "24/7 priority customer support", true),
                        ("Custom Recipes", "Create, save and monetize custom recipes", true),
                        ("Family Sharing", "Share plans with family members", true),
                        ("Offline Access", "Access recipes and plans offline", true),
                        ("Export Data", "Export all personal data and recipes", true)
                    }),

                    // Annual Basic Plan
                    ("Basic Annual", "Enhanced features with annual savings", 99.99m, "USD", "Annual", 2500, true, new List<(string, string, bool)>
                    {
                        ("Recipe Search", "Search through comprehensive recipe database", true),
                        ("Basic Meal Planning", "Create and customize meal plans", true),
                        ("Allergen Alerts", "Advanced allergen detection and warnings", true),
                        ("Profile Management", "Manage user profile and preferences", true),
                        ("Community Access", "Access to community features and forums", true),
                        ("Shopping Lists", "Generate shopping lists from meal plans", true),
                        ("Nutrition Tracking", "Track daily nutrition intake", true),
                        ("Advanced Analytics", "Basic nutrition analytics", true),
                        ("Premium Support", "Email customer support", true),
                        ("Custom Recipes", "Create and save unlimited custom recipes", true)
                    }),

                    // Annual Premium Plan
                    ("Premium Annual", "Full access with maximum savings", 199.99m, "USD", "Annual", 12000, true, new List<(string, string, bool)>
                    {
                        ("Recipe Search", "Search through comprehensive recipe database with AI recommendations", true),
                        ("Basic Meal Planning", "Create, customize and share meal plans", true),
                        ("Allergen Alerts", "Advanced allergen detection with severity levels", true),
                        ("Profile Management", "Comprehensive profile with health tracking", true),
                        ("Community Access", "Premium community features and expert access", true),
                        ("Shopping Lists", "Smart shopping lists with price tracking", true),
                        ("Nutrition Tracking", "Advanced nutrition tracking with goals", true),
                        ("Advanced Analytics", "Detailed nutrition analytics and reports", true),
                        ("Premium Support", "24/7 priority customer support", true),
                        ("Custom Recipes", "Create, save and monetize custom recipes", true),
                        ("Family Sharing", "Share plans with family members", true),
                        ("Offline Access", "Access recipes and plans offline", true),
                        ("Export Data", "Export all personal data and recipes", true),
                        ("API Access", "Access to developer API", true),
                        ("White Label", "Brand customization options", true)
                    })
                };

                // Create and save subscription plans
                foreach (var (name, description, price, currency, billingCycle, usageQuota, isActive, features) in subscriptionPlans)
                {
                    var plan = new SubscriptionPlan
                    {
                        Name = name,
                        Description = description,
                        Price = price,
                        Currency = currency,
                        BillingCycle = billingCycle,
                        UsageQuota = usageQuota,
                        IsActive = isActive,
                        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
                    };

                    context.SubscriptionPlans.Add(plan);
                    await context.SaveChangesAsync(); // Save to get the ID

                    // Add features for this plan
                    foreach (var (featureName, featureDescription, isEnabled) in features)
                    {
                        var planFeature = new PlanFeature
                        {
                            PlanId = plan.Id,
                            FeatureName = featureName,
                            Description = featureDescription,
                            IsEnabled = isEnabled,
                            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
                        };

                        context.PlanFeatures.Add(planFeature);
                    }
                }

                await context.SaveChangesAsync();
                logger?.LogInformation("Subscription plan seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during subscription plan seeding");
                throw;
            }
        }

        public static async Task CleanSubscriptionPlansAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting subscription plan cleanup...");

                // Remove all plan features first (foreign key constraint)
                var planFeatures = await context.PlanFeatures.ToListAsync();
                context.PlanFeatures.RemoveRange(planFeatures);

                // Remove all subscription plans
                var subscriptionPlans = await context.SubscriptionPlans.ToListAsync();
                context.SubscriptionPlans.RemoveRange(subscriptionPlans);

                await context.SaveChangesAsync();
                logger?.LogInformation("Subscription plan cleanup completed!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during subscription plan cleanup");
                throw;
            }
        }
    }
} 