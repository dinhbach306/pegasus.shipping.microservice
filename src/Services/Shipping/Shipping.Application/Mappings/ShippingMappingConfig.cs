using Mapster;
using Shipping.Application.DTOs;
using Shipping.Domain;

namespace Shipping.Application.Mappings;

/// <summary>
/// Mapster configuration for Shipping service mappings
/// </summary>
public class ShippingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Shipment -> ShipmentDto
        config.NewConfig<Shipment, ShipmentDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.TrackingNumber, src => src.TrackingNumber)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.IsActive, src => src.IsActive);

        // Shipment -> ShipmentListDto (for list views)
        config.NewConfig<Shipment, ShipmentListDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.TrackingNumber, src => src.TrackingNumber)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        // CreateShipmentRequest -> Shipment
        config.NewConfig<CreateShipmentRequest, Shipment>()
            .MapWith(src => new Shipment(src.TrackingNumber));

        // UpdateShipmentStatusRequest handling (will be applied manually in service via domain methods)
        // Note: Status updates should use Shipment.UpdateStatus() to maintain domain logic
        config.NewConfig<UpdateShipmentStatusRequest, Shipment>()
            .IgnoreNonMapped(true);
    }
}

