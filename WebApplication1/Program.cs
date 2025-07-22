using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger configuration
using System.Text;
using WebApplication1.Data;
using WebApplication1.DTO.Configuration;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Repository.Interface;
using WebApplication1.Service;
using WebApplication1.Service.Interface;

namespace WebApplication1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register memory caching
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ICacheService, MemoryCacheService>();

            // Register the DbContext with the connection string
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"), sqloptions => sqloptions.UseHierarchyId()));
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
            builder.Services.AddIdentity<User, Role>(options =>
            {
                // Identity configuration (like password rules, etc.)
                options.SignIn.RequireConfirmedEmail = true;
            })
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
            //Bind EmailSettings
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Smtp"));
            builder.Services.Configure<DefaultUsersSettings>(builder.Configuration.GetSection(DefaultUsersSettings.SectionName));
            builder.Services.AddTransient<IUserTwoFactorTokenProvider<User>, DataProtectorTokenProvider<User>>();
            // Register services
            builder.Services.AddControllers();
            
            // Register Audit Services
            builder.Services.AddHttpContextAccessor(); // Required for accessing current user context
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddScoped<IDatabaseSeederService, DatabaseSeederService>();

            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailSender, EmailSenderService>();
            builder.Services.AddScoped<IChatbotModelsService, ChatbotModelsService>();
            builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            
            // Register Chat Services
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddHttpClient(); // Required for AI API calls
            
            // Register Repository
            builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
            builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
            builder.Services.AddScoped<IConversationBranchRepository, ConversationBranchRepository>();
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<IChatbotModelsRepository, ChatbotModelsRepository>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Register Swagger with Authorization configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddAuthorization(options =>
            {
                // Ví dụ tạo policy tên "RequireAdminRole"
                options.AddPolicy("RequireAdminRole", policy =>
                {
                    policy.RequireRole("Admin");
                });
            });
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

            
            var app = builder.Build();
            app.UseCors("AllowReactApp");
            // Map routes/UI, etc.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days.
                app.UseHsts();
            }
            // Seed the system user and admin user on application startup
            using (var scope = app.Services.CreateScope())
            {
                var seederService = scope.ServiceProvider.GetRequiredService<IDatabaseSeederService>();
                await seederService.SeedAllAsync();
            }
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