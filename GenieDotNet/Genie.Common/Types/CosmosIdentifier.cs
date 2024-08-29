namespace Genie.Common.Types;
public record CosmosIdentifier
{
    public string Id { get; set; } = "";
    public string PartitionKey { get; set; } = "";
}
