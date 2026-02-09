using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using MyFirstApi.Services;
using MyFirstApi.Filters;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Starting Career Guidance API (Full Version)...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

try
{
    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add services to the container with global exception filter
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register HttpClient for GroqService and JobApiService
    builder.Services.AddHttpClient<GroqService>();
    builder.Services.AddHttpClient<JobApiService>();

    // Register Core Services
    builder.Services.AddScoped<DatabaseService>();
    builder.Services.AddScoped<CareerProgressService>();

    // Register Job Services
    builder.Services.AddScoped<JobApiService>();
    builder.Services.AddScoped<JobDatabaseService>();

    // Register Company Module Services
    builder.Services.AddScoped<CompanyService>();
    builder.Services.AddScoped<HiringNotificationService>();
    builder.Services.AddScoped<JobApplicationService>();

    // Register Crash Reporting Service
    builder.Services.AddScoped<ICrashReportingService, LocalCrashReportingService>();

    // Configure JWT Authentication
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection.GetValue<string>("Key");
    if (!string.IsNullOrEmpty(jwtKey))
    {
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                    ValidAudience = jwtSection.GetValue<string>("Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        Console.WriteLine($"Token validated for user: {userId}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Authentication challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();
    }

    Console.WriteLine("Services registered successfully");

    var app = builder.Build();

    Console.WriteLine("App built successfully");

    // Always enable Swagger (not just in Development)
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseStaticFiles();

    // DO NOT use HTTPS redirection on Azure (handled by Azure itself)
    // app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoints
    app.MapGet("/", () => new
    {
        status = "OK",
        message = "Career Guidance API",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    });

    app.MapGet("/health", () => new
    {
        status = "healthy",
        version = "1.0",
        timestamp = DateTime.UtcNow
    });

    app.MapControllers();

    Console.WriteLine("Routes configured, starting app...");

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: Startup failed: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    throw;
}