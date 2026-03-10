namespace MicroCredit.Domain.Model.Master;

public class UpdateLookupRequest
{
    public string LookupKey { get; set; } = string.Empty;
    public string LookupCode { get; set; } = string.Empty;
    public string LookupValue { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
}
