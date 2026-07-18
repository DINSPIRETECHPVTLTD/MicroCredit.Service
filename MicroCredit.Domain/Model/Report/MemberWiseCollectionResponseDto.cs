using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Report
{
    public class MemberWiseCollectionResponseDto
    {
        public int memberId { get; set; }
        public string memberName { get; set; } = string.Empty;
        public string guardianName { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string phoneNumber { get; set; } = string.Empty;
        public decimal loanAmount { get; set; }
        public int outstandingWeeks { get; set; }
        public decimal weeklyDueAmount { get; set; }
        public decimal asOnOutStanding { get; set; }
        public string collectionDay { get; set; } = string.Empty;
        public string attendStaff { get; set; } = string.Empty;
        public string centerName { get; set; } = string.Empty;
        public DateTime? disbursementDate { get; set; }
        public decimal principleCollected { get; set; }
        public decimal interestCollected { get; set; }
        public decimal collected { get; set; }
        public decimal toBeCollected { get; set; }
        public decimal osBalance { get; set; }
    }
}
