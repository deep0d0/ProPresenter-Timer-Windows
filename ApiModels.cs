using System.Text.Json.Serialization;

namespace ProPresenterTimer;

internal sealed class ClipResponse
{
    public Transport? Transport { get; set; }
}

internal sealed class Transport
{
    public Position? Position { get; set; }
}

internal sealed class Position
{
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Value { get; set; }
}
