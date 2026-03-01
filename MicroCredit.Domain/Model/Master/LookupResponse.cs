namespace MicroCredit.Domain.Model.Master;

public class LookupResponse
{
    public int Id { get; set; }
    public string LookupKey { get; set; } = string.Empty;
    public string LookupValue { get; set; } = string.Empty;
    public string LookupCode { get; set; } = string.Empty;
    public int? NumericValue { get; set; }
    public int SortOrder { get; set; }
}
