using SharedKernel;

namespace Shipping.Domain;

public sealed class Shipment : Entity
{
    public string TrackingNumber { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Created";

    private Shipment() { }

    public Shipment(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
        // CreatedAt is automatically set by Entity base class
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        MarkAsUpdated(); // Automatically sets UpdatedAt timestamp
    }
}

