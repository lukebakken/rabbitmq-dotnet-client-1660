namespace Genie.Common.Types;

public record BaseResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Info { get; set; }
    public string? TransactionId { get; set; }
}