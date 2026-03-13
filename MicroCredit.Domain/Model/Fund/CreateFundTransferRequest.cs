using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Fund
{
    public class CreateFundTransferRequest
    {
        public int PaidFromUserId { get; set; }
        public int PaidToUserId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Comments { get; set; }
    }
}
