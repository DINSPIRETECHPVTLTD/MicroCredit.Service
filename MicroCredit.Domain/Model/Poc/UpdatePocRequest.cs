namespace MicroCredit.Domain.Model.Poc;

public class UpdatePocRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
    public string? AltPhone { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public int CenterId { get; set; }

    public string? CollectionDay { get; set; }
    public string CollectionFrequency { get; set; } = string.Empty;
    public int CollectionBy { get; set; }
}