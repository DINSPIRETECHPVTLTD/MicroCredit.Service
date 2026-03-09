using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("POCs")]
public class POC
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; private set; } = string.Empty;

    [StringLength(100)]
    public string? MiddleName { get; private set; }

    [Required]
    [StringLength(100)]
    public string LastName { get; private set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; private set; } = string.Empty;

    [StringLength(20)]
    public string? AltPhone { get; private set; }

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

    [Required]
    public int CenterId { get; private set; }

    [ForeignKey("CenterId")]
    public virtual Center Center { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [StringLength(20)]
    public string? CollectionDay { get; private set; }

    [Required]
    [StringLength(20)]
    public string CollectionFrequency { get; private set; } = string.Empty;

    [Required]
    public int CollectionBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; } 

    [ForeignKey("CollectionBy")]
    public virtual User? CollectionByUser { get; private set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    public virtual ICollection<Member>? Members { get; private set; }
    public int BranchId { get; set; }

    private POC() { } // EF

    public POC(string firstName, string lastName, string phoneNumber, int centerId, int createdBy,
        string collectionFrequency, int collectionBy, string? middleName = null, string? altPhone = null,
        string? address1 = null, string? address2 = null, string? city = null, string? state = null,
        string? zipCode = null, string? collectionDay = null)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CenterId = centerId;
        CreatedBy = createdBy;
        CollectionFrequency = collectionFrequency;
        CollectionBy = collectionBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        MiddleName = middleName;
        AltPhone = altPhone;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        CollectionDay = collectionDay;
    }

    public void UpdateDetails(string firstName, string? middleName, string lastName, string phoneNumber, string? altPhone,
        string? address1, string? address2, string? city, string? state, string? zipCode,
        string? collectionDay, string collectionFrequency, int collectionBy, int modifiedBy)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        AltPhone = altPhone;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        CollectionDay = collectionDay;
        CollectionFrequency = collectionFrequency;
        CollectionBy = collectionBy;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void MarkDeleted(int modifiedBy)
    {
        IsDeleted = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
