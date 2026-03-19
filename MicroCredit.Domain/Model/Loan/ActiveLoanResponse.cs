using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Loan
{
    public class ActiveLoanResponse
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DisbursementDate { get; set; }
        public DateTime? ClosureDate { get; set; }
    }
}
