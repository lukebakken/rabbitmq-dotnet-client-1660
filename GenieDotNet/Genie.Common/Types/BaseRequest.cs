using System.Text.Json.Serialization;

namespace Genie.Common.Types;

public record BaseRequest : CosmosBase
{
    public Guid DeviceId { get; set; }
    public Coordinate? Origin { get; set; }
    public int? HorizontalAccuracy { get; set; }
    public int? VerticalAccuracy { get; set; }
    public string? Info { get; set; }
    public string? Type { get; set; }
    public string? IPAddressSource { get; set; }
    public string? IPAddressDestination { get; set; }
    [JsonIgnore]
    public DateTime? RequestTtl { get; set; }
    [JsonIgnore]
    public long Length { get; set; }
    [JsonIgnore]
    public int GracePeriod { get; set; }
}