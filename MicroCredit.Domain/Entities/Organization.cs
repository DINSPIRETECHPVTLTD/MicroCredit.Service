using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Organizations")]
public class Organization
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [StringLength(200)]
    public string Name { get; private set; } = string.Empty;

    [StringLength(200)]
    public string? Address1 { get; private set; }

    [StringLength(200)]
    public string? Address2 { get; private set; }

    [StringLength(100)]
    public string? City { get; private set; }

    [StringLength(100)]
    public string? State { get; private set; }

    [StringLength(20)]
    public string? ZipCode { get; private set; }

    [StringLength(20)]
    public string? PhoneNumber { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; }

    // Navigation
    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    public virtual ICollection<User> Users { get; private set; } = new List<User>();
    public virtual ICollection<Branch> Branches { get; private set; } = new List<Branch>();

    private Organization() { } // EF

    public Organization(string name, int createdBy, string? address1 = null, string? address2 = null,
        string? city = null, string? state = null, string? zipCode = null, string? phoneNumber = null)
    {
        Name = name;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
    }

    public void UpdateDetails(string name, string? address1, string? address2, string? city, string? state,
        string? zipCode, string? phoneNumber, int modifiedBy)
    {
        Name = name;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
    }
}
