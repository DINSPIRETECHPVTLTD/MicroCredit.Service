using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Branchs")]
public class Branch
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

    [StringLength(100)]
    public string? Country { get; private set; }

    [StringLength(20)]
    public string? ZipCode { get; private set; }

    [StringLength(20)]
    public string? PhoneNumber { get; private set; }

    [Required]
    public int OrgId { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; }

    // Navigation
    [ForeignKey("OrgId")]
    public virtual Organization Organization { get; private set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    public virtual ICollection<User> Users { get; private set; } = new List<User>();
    public virtual ICollection<Center> Centers { get; private set; } = new List<Center>();

    private Branch() { } // EF

    public Branch(string name, int orgId, int createdBy, string? address1 = null, string? address2 = null,
        string? city = null, string? state = null, string? country = null, string? zipCode = null, string? phoneNumber = null)
    {
        Name = name;
        OrgId = orgId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
    }

    public void UpdateDetails(string name, string? address1, string? address2, string? city, string? state,
        string? country, string? zipCode, string? phoneNumber, int modifiedBy)
    {
        Name = name;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
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
