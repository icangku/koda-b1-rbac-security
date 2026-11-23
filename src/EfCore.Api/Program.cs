using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using EfCore.Api.Middlewares;
using EfCore.Application.Common.Interfaces;
using EfCore.Application.Security;
using EfCore.Application.Services;
using EfCore.Infrastructure;
using EfCore.Infrastructure.Identity;
using EfCore.Infrastructure.Persistence;
using EfCore.Infrastructure.Repositories;
using EfCore.Infrastructure.Seeding;
using KodaStore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();

var JwtOption = builder.Configuration
    .GetSection(JwtOptions.SectionName).Get<JwtOptions>() ??  throw new InvalidOperationException("Jwt not available");

if(string.IsNullOrWhiteSpace(JwtOption.Issuer)) throw new InvalidOperationException("Jwt: Issuer must present");
if(string.IsNullOrWhiteSpace(JwtOption.Audience)) throw new InvalidOperationException("Jwt: Audience must present");
if(JwtOption.AccessTokenMinutes is < 1 or > 60) throw new InvalidOperationException("Jwt: Minutes must between 1 to 60");

byte[] signingKeyBytes;

try
{
	signingKeyBytes = Convert.FromBase64String(JwtOption.SigningKeyBase64);
}
catch (FormatException e)
{
	throw new InvalidOperationException("Jwt: SigningKeyBase64 not using Base64 format");
}

if(signingKeyBytes.Length < 32) throw new InvalidOperationException("JWT signing key minimal 32");

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(option =>
{
   option.AddPolicy("ReactClient", policy =>
   {
      policy.WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
   });
});


builder.Services.AddRateLimiter(option =>
{
    option.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    option.AddConcurrencyLimiter("category-writes", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.QueueLimit = 0;
    });

    // Untuk Login

    option.AddPolicy("auth-login", context =>
    {
      var ip = context.Connection.RemoteIpAddress?.ToString() ?? "null";
      return RateLimitPartition
        .GetFixedWindowLimiter(
          partitionKey: $"login: {ip}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(3),
            QueueLimit = 0,
            AutoReplenishment = true
          }
        );
    });

    option.AddPolicy("auth-register", context =>
    {
      var ip = context.Connection.RemoteIpAddress?.ToString() ?? "null";
      return RateLimitPartition
        .GetFixedWindowLimiter(
          partitionKey: $"register: {ip}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(3),
            QueueLimit = 0,
            AutoReplenishment = true
          }
        );
    });

    option.AddPolicy("auth-refresh", context =>
    {
      var ip = context.Connection.RemoteIpAddress?.ToString() ?? "null";
      return RateLimitPartition
        .GetFixedWindowLimiter(
          partitionKey: $"refresh: {ip}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(3),
            QueueLimit = 0,
            AutoReplenishment = true
          }
        );
    });

    option.AddFixedWindowLimiter("LoginLimiter", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(3);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(option =>
	{
		option.MapInboundClaims = false;
		option.IncludeErrorDetails = false;
		option.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = JwtOption.Issuer,
			ValidateAudience = true,
			ValidAudience = JwtOption.Audience,
			ValidateIssuerSigningKey = true, 
			IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
			RequireSignedTokens = true,
			ValidAlgorithms = [
				SecurityAlgorithms.HmacSha256
			],
			ValidateLifetime = true, 
			RequireExpirationTime = true,
			ClockSkew = TimeSpan.Zero,
			NameClaimType = JwtRegisteredClaimNames.Sub,
			RoleClaimType = "role"
		};
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.CanManageCategory, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AppRoles.Admin);
    });

    options.AddPolicy(AppPolicies.CanManageProduct, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AppRoles.Admin);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<AppUser>>();

    var bootstrapAdminEmail = app.Configuration["RoleBootstrap:AdminEmail"];

    await RoleSeedData.SeedAsync(
        roleManager,
        userManager,
        bootstrapAdminEmail);

    app.MapOpenApi();
}

//PIPELINES
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<RequestTimingMiddleware>();
app.UseCors("ReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.MapControllers();

app.Run();
