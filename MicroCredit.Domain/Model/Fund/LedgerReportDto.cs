using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Fund
{
    public class LedgerReportDto
    {
        public int id { get; set; }
        public string UserName { get; set; }
        public decimal? Amount { get; set; }
        public decimal? InsuranceAmount { get; set; }
        public decimal? ClaimedAmount { get; set; }
    }
}
