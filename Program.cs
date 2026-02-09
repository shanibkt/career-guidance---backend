using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using MyFirstApi.Services;
using MyFirstApi.Filters;

var builder = WebApplication.CreateBuilder(args);

// FORCE GLOBAL CONFIGURATION OVERRIDE (Diagnostic Fix for Azure SslMode issue)
// This ensures ALL services (JobDatabaseService, ProfileController, etc.) use this string
// ignoring whatever is set in Azure Environment Variables.
builder.Configuration["ConnectionStrings:DefaultConnection"] = "Server=sql.freedb.tech;Port=3306;Database=freedb_career_guidence;User ID=freedb_shanib;Password=x%g7emc?X@uz7?W;ConvertZeroDateTime=True;AllowZeroDateTime=True;AllowPublicKeyRetrieval=True;SslMode=None;ConnectionTimeout=60;DefaultCommandTimeout=60;Pooling=true;MinimumPoolSize=0;MaximumPoolSize=100;";

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

    app.MapGet("/test-db", async (DatabaseService db) =>
    {
        try
        {
            using var conn = db.GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            var result = await cmd.ExecuteScalarAsync();
            return Results.Ok(new 
            { 
                status = "success", 
                message = "Database Connected Successfully!", 
                test_result = result,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new 
            { 
                status = "error", 
                message = "Database Connection Failed", 
                error_details = ex.Message, 
                stack_trace = ex.StackTrace 
            }, statusCode: 500);
        }
    });

    app.MapGet("/test-network", async () =>
    {
        var host = "sql.freedb.tech";
        var port = 3306;
        var result = new Dictionary<string, object>();

        try
        {
            // 1. DNS Lookup
            var ips = await System.Net.Dns.GetHostAddressesAsync(host);
            result["dns_lookup"] = ips.Select(ip => ip.ToString()).ToArray();
            result["dns_status"] = "Success";
        }
        catch (Exception ex)
        {
            result["dns_status"] = "Failed: " + ex.Message;
        }

        try
        {
            // 2. TCP Ping
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(5000));
            
            if (completedTask == connectTask)
            {
                await connectTask; // Propagate exceptions
                result["tcp_ping"] = "Success";
                result["tcp_connected"] = client.Connected;
            }
            else
            {
                result["tcp_ping"] = "Timed out after 5000ms";
            }
        }
        catch (Exception ex)
        {
            result["tcp_ping"] = "Failed: " + ex.Message;
        }

        result["timestamp"] = DateTime.UtcNow;
        return Results.Ok(result);
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