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
        public string memberName { get; set; }
        public string guardianName { get; set; }
        public string address { get; set; }
        public string phoneNumber { get; set; }
        public decimal loanAmount { get; set; }
        public int outstandingWeeks { get; set; }
        public decimal weeklyDueAmount { get; set; }
        public decimal asOnOutStanding { get; set; }
        public string collectionDay { get; set; }
        public string attendStaff { get; set; }
        public string centerName { get; set; }
    }
}
