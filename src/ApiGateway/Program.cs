using Microsoft.AspNetCore.Authentication.JwtBearer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (OpenTelemetry, Health checks)
builder.Services.AddServiceDefaults(builder.Configuration);

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add JWT Authentication for Auth0
// IMPORTANT: Clear claim type mappings to keep original claim names (sub, email, etc.)
Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication("Auth0")
    .AddJwtBearer("Auth0", options =>
    {
        var domain = builder.Configuration["Auth0:Domain"];
        options.Authority = $"https://{domain}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "sub" // Use 'sub' as the name identifier
        };
    });

builder.Services.AddAuthorization();

// Add Ocelot
builder.Services.AddOcelot();

var app = builder.Build();

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map default endpoints (health, metrics)
app.MapDefaultEndpoints();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
