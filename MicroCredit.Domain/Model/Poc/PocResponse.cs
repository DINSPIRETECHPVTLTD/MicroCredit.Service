using System.ComponentModel.DataAnnotations;

namespace MicroCredit.Domain.Model.Poc;

public class PocResponse
{
    public int CenterId { get; set; }
    public int Id { get; set; }
    public string? FirstName { get; set; }   
    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string Name => string.Join(" ", new[]
    {
    FirstName,
    MiddleName,
    LastName
}.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }


    public string? City { get; set; }

 
    public string? State { get;  set; }

    
    public string? ZipCode { get;  set; }
    public string FullAddress => string.Join(", ", new[] { Address1, Address2, City, State, ZipCode }
                                           .Where(x => !string.IsNullOrWhiteSpace(x)));

 
}
