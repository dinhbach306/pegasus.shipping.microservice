using Identity.Api.Authorization;
// using Identity.Api.Consumers;
// using Identity.Application.Events;
using Identity.Application;
using Identity.Infrastructure;
using Messaging;
using ServiceDefaults;
using SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults(builder.Configuration);
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddKafkaProducer(builder.Configuration);

// Example: Subscribe to events from other services
// Uncomment to enable cross-service event handling
// builder.Services.AddKafkaConsumer<ShipmentCreatedEvent, ShipmentCreatedConsumer>(KafkaTopics.ShipmentCreated);

// Register user context from headers (forwarded by API Gateway)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HeaderUserContext>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;

    var permissionsHeader = httpContext?.Request.Headers["X-User-Permissions"].FirstOrDefault() ?? string.Empty;
    var permissions = permissionsHeader
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Trim())
        .ToArray();

    return new HeaderUserContext
    {
        UserId = httpContext?.Request.Headers["X-User-Id"].FirstOrDefault(),
        Email = httpContext?.Request.Headers["X-User-Email"].FirstOrDefault(),
        Permissions = permissions
    };
});

// Add permission-based authorization
builder.Services.AddPermissionBasedAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
