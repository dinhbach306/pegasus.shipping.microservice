using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Observability;

public static class Telemetry
{
    public const string ServiceName = "Pegasus.Service";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);
}

