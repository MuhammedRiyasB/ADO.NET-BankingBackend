using System.Text;
using System.Threading.RateLimiting;
using Banking.API.Middleware;
using Banking.Application.Extensions;
using Banking.Application.Interfaces;
using Banking.Infrastructure.Configuration;
using Banking.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Bind secrets from environment variables ──────────────────────────
var dbConnection = Environment.GetEnvironmentVariable("BANKING_DB_CONNECTION");
var jwtSigningKey = Environment.GetEnvironmentVariable("BANKING_JWT_SIGNING_KEY");
var adminPassword = Environment.GetEnvironmentVariable("BANKING_ADMIN_PASSWORD");

if (string.IsNullOrWhiteSpace(dbConnection))
{
    throw new InvalidOperationException(
        "Environment variable 'BANKING_DB_CONNECTION' is required. Set it to your SQL Server connection string.");
}

if (string.IsNullOrWhiteSpace(jwtSigningKey) || jwtSigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Environment variable 'BANKING_JWT_SIGNING_KEY' is required and must be at least 32 characters.");
}

if (string.IsNullOrWhiteSpace(adminPassword))
{
    throw new InvalidOperationException(
        "Environment variable 'BANKING_ADMIN_PASSWORD' is required. Set it to the initial admin user password.");
}

builder.Configuration["SqlServer:ConnectionString"] = dbConnection;
builder.Configuration["Jwt:SigningKey"] = jwtSigningKey;
builder.Configuration["AdminSeed:Password"] = adminPassword;
// ─────────────────────────────────────────────────────────────────────

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 120;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking Backend API",
        Version = "v1",
        Description = "Core banking backend using ASP.NET, Clean Architecture, and ADO.NET."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Provide the JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await databaseInitializer.InitializeAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
// app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

app.Run();
