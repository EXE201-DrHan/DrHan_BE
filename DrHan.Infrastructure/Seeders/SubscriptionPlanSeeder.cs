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
                        CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30))
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
                            CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30))
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
        public static async Task SeedSubscriptionPlansAsync1(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting subscription plan seeding...");

                // Define subscription plans with their features
                var subscriptionPlans = new List<(string Name, string Description, decimal Price, string Currency, string BillingCycle, int? UsageQuota, bool IsActive, List<(string FeatureName, string Description, bool IsEnabled)> Features)>
        {
            // Free Plan
            ("Miễn phí", "Các tính năng cơ bản để bắt đầu", 0m, "VND", "Hàng tháng", 50, true, new List<(string, string, bool)>
            {
                ("Tìm kiếm công thức nấu ăn", "Tìm kiếm trong cơ sở dữ liệu công thức cơ bản", true),
                ("Lập kế hoạch bữa ăn cơ bản", "Tạo các kế hoạch bữa ăn đơn giản", true),
                ("Cảnh báo dị ứng", "Phát hiện dị ứng cơ bản", true),
                ("Quản lý hồ sơ", "Quản lý hồ sơ và sở thích của người dùng", true),
                ("Truy cập cộng đồng", "Truy cập các tính năng cộng đồng", false),
                ("Phân tích nâng cao", "Phân tích dinh dưỡng chi tiết", false),
                ("Hỗ trợ cao cấp", "Hỗ trợ khách hàng ưu tiên", false),
                ("Công thức tùy chỉnh", "Tạo và lưu công thức tùy chỉnh", false)
            }),

            // Basic Plan
            ("Cơ bản", "Các tính năng nâng cao cho người dùng thông thường", 9.99m, "VND", "Hàng tháng", 200, true, new List<(string, string, bool)>
            {
                ("Tìm kiếm công thức nấu ăn", "Tìm kiếm trong cơ sở dữ liệu công thức toàn diện", true),
                ("Lập kế hoạch bữa ăn cơ bản", "Tạo và tùy chỉnh kế hoạch bữa ăn", true),
                ("Cảnh báo dị ứng", "Phát hiện và cảnh báo dị ứng nâng cao", true),
                ("Quản lý hồ sơ", "Quản lý hồ sơ và sở thích của người dùng", true),
                ("Truy cập cộng đồng", "Truy cập các tính năng và diễn đàn cộng đồng", true),
                ("Danh sách mua sắm", "Tạo danh sách mua sắm từ kế hoạch bữa ăn", true),
                ("Theo dõi dinh dưỡng", "Theo dõi lượng dinh dưỡng hàng ngày", true),
                ("Phân tích nâng cao", "Phân tích dinh dưỡng chi tiết", false),
                ("Hỗ trợ cao cấp", "Hỗ trợ khách hàng ưu tiên", false),
                ("Công thức tùy chỉnh", "Tạo và lưu công thức tùy chỉnh không giới hạn", true)
            }),

            // Premium Plan
            ("Cao cấp", "Truy cập đầy đủ tất cả các tính năng", 19.99m, "VND", "Hàng tháng", 1000, true, new List<(string, string, bool)>
            {
                ("Tìm kiếm công thức nấu ăn", "Tìm kiếm trong cơ sở dữ liệu công thức toàn diện với gợi ý AI", true),
                ("Lập kế hoạch bữa ăn cơ bản", "Tạo, tùy chỉnh và chia sẻ kế hoạch bữa ăn", true),
                ("Cảnh báo dị ứng", "Phát hiện dị ứng nâng cao với mức độ nghiêm trọng", true),
                ("Quản lý hồ sơ", "Hồ sơ toàn diện với theo dõi sức khỏe", true),
                ("Truy cập cộng đồng", "Tính năng cộng đồng cao cấp và truy cập chuyên gia", true),
                ("Danh sách mua sắm", "Danh sách mua sắm thông minh với theo dõi giá", true),
                ("Theo dõi dinh dưỡng", "Theo dõi dinh dưỡng nâng cao với mục tiêu", true),
                ("Phân tích nâng cao", "Phân tích dinh dưỡng chi tiết và báo cáo", true),
                ("Hỗ trợ cao cấp", "Hỗ trợ khách hàng ưu tiên 24/7", true),
                ("Công thức tùy chỉnh", "Tạo, lưu và kiếm tiền từ công thức tùy chỉnh", true),
                ("Chia sẻ gia đình", "Chia sẻ kế hoạch với các thành viên gia đình", true),
                ("Truy cập ngoại tuyến", "Truy cập công thức và kế hoạch ngoại tuyến", true),
                ("Xuất dữ liệu", "Xuất tất cả dữ liệu cá nhân và công thức", true)
            }),

            // Annual Basic Plan
            ("Cơ bản hàng năm", "Các tính năng nâng cao với tiết kiệm hàng năm", 99.99m, "VND", "Hàng năm", 2500, true, new List<(string, string, bool)>
            {
                ("Tìm kiếm công thức nấu ăn", "Tìm kiếm trong cơ sở dữ liệu công thức toàn diện", true),
                ("Lập kế hoạch bữa ăn cơ bản", "Tạo và tùy chỉnh kế hoạch bữa ăn", true),
                ("Cảnh báo dị ứng", "Phát hiện và cảnh báo dị ứng nâng cao", true),
                ("Quản lý hồ sơ", "Quản lý hồ sơ và sở thích của người dùng", true),
                ("Truy cập cộng đồng", "Truy cập các tính năng và diễn đàn cộng đồng", true),
                ("Danh sách mua sắm", "Tạo danh sách mua sắm từ kế hoạch bữa ăn", true),
                ("Theo dõi dinh dưỡng", "Theo dõi lượng dinh dưỡng hàng ngày", true),
                ("Phân tích nâng cao", "Phân tích dinh dưỡng cơ bản", true),
                ("Hỗ trợ cao cấp", "Hỗ trợ khách hàng qua email", true),
                ("Công thức tùy chỉnh", "Tạo và lưu công thức tùy chỉnh không giới hạn", true)
            }),

            // Annual Premium Plan
            ("Cao cấp hàng năm", "Truy cập đầy đủ với tiết kiệm tối đa", 199.99m, "VND", "Hàng năm", 12000, true, new List<(string, string, bool)>
            {
                ("Tìm kiếm công thức nấu ăn", "Tìm kiếm trong cơ sở dữ liệu công thức toàn diện với gợi ý AI", true),
                ("Lập kế hoạch bữa ăn cơ bản", "Tạo, tùy chỉnh và chia sẻ kế hoạch bữa ăn", true),
                ("Cảnh báo dị ứng", "Phát hiện dị ứng nâng cao với mức độ nghiêm trọng", true),
                ("Quản lý hồ sơ", "Hồ sơ toàn diện với theo dõi sức khỏe", true),
                ("Truy cập cộng đồng", "Tính năng cộng đồng cao cấp và truy cập chuyên gia", true),
                ("Danh sách mua sắm", "Danh sách mua sắm thông minh với theo dõi giá", true),
                ("Theo dõi dinh dưỡng", "Theo dõi dinh dưỡng nâng cao với mục tiêu", true),
                ("Phân tích nâng cao", "Phân tích dinh dưỡng chi tiết và báo cáo", true),
                ("Hỗ trợ cao cấp", "Hỗ trợ khách hàng ưu tiên 24/7", true),
                ("Công thức tùy chỉnh", "Tạo, lưu và kiếm tiền từ công thức tùy chỉnh", true),
                ("Chia sẻ gia đình", "Chia sẻ kế hoạch với các thành viên gia đình", true),
                ("Truy cập ngoại tuyến", "Truy cập công thức và kế hoạch ngoại tuyến", true),
                ("Xuất dữ liệu", "Xuất tất cả dữ liệu cá nhân và công thức", true),
                ("Truy cập API", "Truy cập API dành cho nhà phát triển", true),
                ("Nhãn trắng", "Tùy chọn tùy chỉnh thương hiệu", true)
            })
        };

                // Process each subscription plan
                foreach (var (name, description, price, currency, billingCycle, usageQuota, isActive, features) in subscriptionPlans)
                {
                    // Check if plan exists by name
                    var existingPlan = await context.SubscriptionPlans
                        .Include(p => p.PlanFeatures)
                        .FirstOrDefaultAsync(p => p.Name == name);

                    if (existingPlan != null)
                    {
                        // Update existing plan
                        existingPlan.Description = description;
                        existingPlan.Currency = currency;
                        existingPlan.BillingCycle = billingCycle;
                        existingPlan.UsageQuota = usageQuota;
                        existingPlan.UpdateAt = DateTime.Now;

                        // Remove all existing features
                        context.PlanFeatures.RemoveRange(existingPlan.PlanFeatures);

                        // Add new features
                        foreach (var (featureName, featureDescription, isEnabled) in features)
                        {
                            var newFeature = new PlanFeature
                            {
                                PlanId = existingPlan.Id,
                                FeatureName = featureName,
                                Description = featureDescription,
                                IsEnabled = isEnabled,
                                CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30))
                            };
                            context.PlanFeatures.Add(newFeature);
                        }

                        context.SubscriptionPlans.Update(existingPlan);
                    }
                    else
                    {
                        // Create new plan
                        var newPlan = new SubscriptionPlan
                        {
                            Name = name,
                            Description = description,
                            Price = price,
                            Currency = currency,
                            BillingCycle = billingCycle,
                            UsageQuota = usageQuota,
                            IsActive = isActive,
                            CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30))
                        };

                        context.SubscriptionPlans.Add(newPlan);
                        await context.SaveChangesAsync(); // Save to get the ID

                        // Add features for the new plan
                        foreach (var (featureName, featureDescription, isEnabled) in features)
                        {
                            var planFeature = new PlanFeature
                            {
                                PlanId = newPlan.Id,
                                FeatureName = featureName,
                                Description = featureDescription,
                                IsEnabled = isEnabled,
                                CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30))
                            };

                            context.PlanFeatures.Add(planFeature);
                        }
                    }
                }

                await context.SaveChangesAsync();
                logger?.LogInformation("Gói đăng ký đã được khởi tạo và cập nhật thành công!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo hoặc cập nhật gói đăng ký");
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