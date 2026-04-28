
namespace MicroCredit.Domain.Model.Loan;

    public class LoanResponse
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DisbursementDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

