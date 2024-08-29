namespace Genie.Common.Types;

public record Coordinate
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Altitude { get; set; }
}