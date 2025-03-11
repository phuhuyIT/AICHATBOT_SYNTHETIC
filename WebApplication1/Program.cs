using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger configuration
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Repository.Interface;
using WebApplication1.Service;
using WebApplication1.Service.Interface;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register the DbContext with the connection string
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings.GetValue<string>("Secret");
            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000") // Allow the React/Next.js origin
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // Required for cookies/credentials
                });
            });
            // Add Identity services
            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Authentication - Combined Cookie and JWT Bearer in a single block
            builder.Services.AddAuthentication(options =>
            {
                // Set default authentication scheme to JWT Bearer
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // Configure JWT Bearer
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
                    ValidAudience = jwtSettings.GetValue<string>("Audience"), // <---- **Verify this line is correctly setting ValidAudience**
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Register other services
            builder.Services.AddControllers();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Register Swagger with Authorization configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //    c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI_chatbot_synthetic", Version = "v1" });

            //    // **Configure JWT Bearer Authorization in Swagger**
            //    c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
            //    {
            //        Description = @"**Cookie-based Authentication** \r\n\r\n" +
            //      "Swagger will use cookies obtained from successful login to authenticate subsequent requests.",
            //        Name = "loginCookie", // Changed Name to loginCookie for clarity, though less critical
            //        In = ParameterLocation.Cookie, // CORRECTED!  Set to ParameterLocation.Cookie
            //        Type = SecuritySchemeType.ApiKey,
            //        Scheme = "Cookie-based authentication"
            //    });

            //    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            //    {
            //        {
            //            new OpenApiSecurityScheme
            //            {
            //                Reference = new OpenApiReference()
            //                  {
            //                    Type = ReferenceType.SecurityScheme,
            //                    Id = "cookieAuth"
            //                  },
            //                  //Scheme = "oauth2",
            //                  //Name = "Bearer",
            //                  In = ParameterLocation.Cookie,
            //            },
            //            new List<string>()
            //        }
            //    });
            //});


            var app = builder.Build();
            app.UseCors("AllowReactApp");
            // Enable Swagger UI
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Use authentication and authorization middleware - in correct order
            app.UseRouting(); // Make sure UseRouting is before UseAuthentication and UseAuthorization
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();


            app.Run();
        }
    }
}