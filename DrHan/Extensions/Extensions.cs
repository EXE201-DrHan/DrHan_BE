using DrHan.API.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
namespace DrHan.API.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static void AddPresentation(this WebApplicationBuilder builder, IConfiguration configuration)
        {

            // Add Controllers with Endpoints
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            }); ;
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKeyString = jwtSettings["SecretKey"];
            
            // Validate JWT configuration
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }
            
            if (secretKeyString.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
            }
            
            var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

            // Add Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(3), //Validator will still consider the token validate if it has expired 3 seconds ago, this is to prevent in case the request takes some time to reach to the api
                    RequireExpirationTime = true
                };
            });

            // Add Authorization
            builder.Services.AddAuthorization(options =>
            {
                var requireAuthPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                options.DefaultPolicy = requireAuthPolicy;
            });

            // Config Routing on Urls
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "DrHan",
                    Version = "v1"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Location");
                });

                options.AddPolicy("AllowGemini", policy =>
                {
                    policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Content-Disposition");
                });
            });


            // tell swagger to support minimal apis, which the Identity apis are.
            builder.Services.AddEndpointsApiExplorer();

            // Add Custom middlewares
            builder.Services.AddScoped<ErrorHandlingMiddleware>();
            builder.Services.AddScoped<TimeLoggingMiddleware>();

            // Add Serilogs
            //builder.Host.UseSerilog((context, configuration) =>
            //{
            //    configuration.ReadFrom.Configuration(context.Configuration);
            //});

        }
    }

}
