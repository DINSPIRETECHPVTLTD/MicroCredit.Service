using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MicroCredit.Domain.Entities;

[Table("Centers")]
public class Center
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [StringLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public int BranchId { get; private set; }

    public string? CenterAddress { get; private set; }

    [StringLength(100)]
    public string? City { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; }

    // Navigation
    [ForeignKey("BranchId")]
    [JsonIgnore]
    public virtual Branch? Branch { get; private set; }

    [ForeignKey("CreatedBy")]
    [JsonIgnore]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    [JsonIgnore]
    public virtual User? ModifiedByUser { get; private set; }

    [JsonIgnore]
    public virtual ICollection<POC> POCs { get; private set; } = new List<POC>();

    [JsonIgnore]
    public virtual ICollection<Member> Members { get; private set; } = new List<Member>();

    private Center() { } // EF

    public Center(string name, int branchId, int createdBy, string? centerAddress = null, string? city = null)
    {
        Name = name;
        BranchId = branchId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        CenterAddress = centerAddress;
        City = city;
    }

    public void UpdateDetails(string name, string? centerAddress, string? city, int modifiedBy)
    {
        Name = name;
        CenterAddress = centerAddress;
        City = city;
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
