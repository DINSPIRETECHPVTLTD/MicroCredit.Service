using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

public enum UserRole
{
    Owner = 1,
    BranchAdmin = 2,
    Staff = 3,
    Investor = 4
}

public enum UserLevel
{
    Org = 1,
    Branch = 2
}

[Table("Users")]
public class User
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
    public UserRole Role { get; private set; }

    [Required]
    [StringLength(200)]
    public string Email { get; private set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; private set; }

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
    public int OrgId { get; private set; }

    [Required]
    public UserLevel Level { get; private set; }

    public int? BranchId { get; private set; }

    [Required]
    public string PasswordHash { get; private set; } = string.Empty;

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

    [ForeignKey("BranchId")]
    public virtual Branch? Branch { get; private set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    private User() { } // EF

    public User(string firstName, string lastName, UserRole role, string email, string passwordHash, int orgId,
        UserLevel level, int createdBy, string? middleName = null, string? phoneNumber = null, string? address1 = null,
        string? address2 = null, string? city = null, string? state = null, string? zipCode = null, int? branchId = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        Email = email;
        PasswordHash = passwordHash;
        OrgId = orgId;
        Level = level;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        MiddleName = middleName;
        PhoneNumber = phoneNumber;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        BranchId = branchId;
    }

    public void UpdateDetails(string firstName, string? middleName, string lastName, string? phoneNumber,
        string? address1, string? address2, string? city, string? state, string? zipCode, int? branchId, int modifiedBy)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        BranchId = branchId;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash, int modifiedBy)
    {
        PasswordHash = passwordHash;
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
