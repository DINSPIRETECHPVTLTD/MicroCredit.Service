using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Investments")]
public class Investment
{
    [Key]
    public int Id { get; private set; }

    [Required]
    public int UserId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    [Required]
    public DateTime InvestmentDate { get; private set; }

    [Required]
    public int CreatedById { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; private set; } = null!;

    [ForeignKey("CreatedById")]
    public virtual User? CreatedByUser { get; private set; }

    private Investment() { } // EF

    public Investment(int userId, decimal amount, DateTime investmentDate, int createdById)
    {
        UserId = userId;
        Amount = amount;
        InvestmentDate = investmentDate;
        CreatedById = createdById;
        CreatedDate = DateTime.UtcNow;
    }
}
