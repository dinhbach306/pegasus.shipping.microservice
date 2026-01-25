using MapsterMapper;
using Messaging;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Shipping.Application;
using Shipping.Application.DTOs;

namespace Shipping.Api.Controllers;

[ApiController]
[Route("api/shipments")]
public sealed class ShipmentsController(
    IShipmentService shipmentService, 
    IShipmentRepository shipmentRepository,
    IMapper mapper,
    HeaderUserContext userContext) : ControllerBase
{
    /// <summary>
    /// PUBLIC endpoint - No authentication required
    /// Get shipping service status and basic info
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            service = "Shipping API",
            status = "operational",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            message = "Shipping service is running. Use /api/shipments/{trackingNumber} with authentication to track shipments."
        });
    }

    /// <summary>
    /// DEBUG endpoint - Shows headers received from API Gateway
    /// </summary>
    // [HttpGet("debug-headers")]
    // public IActionResult DebugHeaders()
    // {
    //     var userId = Request.Headers["X-User-Id"].FirstOrDefault();
    //     var email = Request.Headers["X-User-Email"].FirstOrDefault();
    //     var permissions = Request.Headers["X-User-Permissions"].FirstOrDefault();
    //
    //     return Ok(new
    //     {
    //         receivedHeaders = new
    //         {
    //             userId,
    //             email,
    //             permissions,
    //             permissionsArray = permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray()
    //         },
    //         userContext = new
    //         {
    //             userContext.UserId,
    //             userContext.Email,
    //             userContext.Permissions,
    //             userContext.IsAuthenticated
    //         },
    //         allHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
    //     });
    // }

    /// <summary>
    /// PRIVATE endpoint - Requires read:products permission
    /// Get shipment by tracking number
    /// </summary>
    [RequirePermission("read:products", "admin:all")]
    [HttpGet("{trackingNumber}")]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber, CancellationToken cancellationToken)
    {
        // Gateway already validated authentication, user context is in headers
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        var shipment = await shipmentRepository.GetByTrackingNumberAsync(trackingNumber, cancellationToken);
        if (shipment is null)
        {
            return NotFound(new { error = "Shipment not found", trackingNumber });
        }

        var shipmentDto = mapper.Map<ShipmentDto>(shipment);

        return Ok(new
        {
            shipment = shipmentDto,
            requestedBy = new
            {
                userContext.UserId,
                userContext.UserName,
                userContext.Email,
                permissions = userContext.Permissions
            }
        });
    }

    /// <summary>
    /// PRIVATE endpoint - Requires write:products permission
    /// Create a new shipment
    /// </summary>
    [RequirePermission("write:products", "admin:all")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        // Gateway already validated authentication, user context is in headers
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        // Create shipment and publish event to Kafka
        var shipment = await shipmentService.CreateAsync(
            request, 
            userContext.UserId,
            userContext.UserName,
            userContext.Email, 
            cancellationToken);
        
        var shipmentDto = mapper.Map<ShipmentDto>(shipment);
        
        return CreatedAtAction(nameof(GetByTrackingNumber), new { trackingNumber = shipment.TrackingNumber }, 
            new
            {
                shipment = shipmentDto,
                createdBy = new
                {
                    userContext.UserId,
                    userContext.UserName,
                    userContext.Email,
                    permissions = userContext.Permissions
                },
                eventPublished = KafkaTopics.ShipmentCreated
            });
    }

    /// <summary>
    /// ADMIN endpoint - Requires admin:all permission
    /// Delete a shipment (admin only)
    /// </summary>
    [RequirePermission("admin:all")]
    [HttpDelete("{trackingNumber}")]
    public IActionResult Delete(string trackingNumber)
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        // Business logic for delete would go here
        return Ok(new
        {
            message = $"Shipment {trackingNumber} deleted successfully",
            deletedBy = new
            {
                userContext.UserId,
                userContext.UserName,
                userContext.Email,
                permissions = userContext.Permissions
            }
        });
    }
}
