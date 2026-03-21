using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Loan
{
    public class CreateLoanRequest
    {
            [Required]
            public int MemberId { get; set; }

            [Required]
            public decimal LoanAmount { get; set; }

            [Required]
            public decimal InterestAmount { get; set; }

            [Required]
            public decimal ProcessingFee { get; set; }

            [Required]
            public decimal InsuranceFee { get; set; }

            [Required]
            public bool IsSavingEnabled { get; set; }

            [Required]
            public decimal SavingAmount { get; set; }
            [Required]
            public decimal TotalAmount { get; set; }

            [Required]
            public DateTime DisbursementDate { get; set; }

            [Required]
            public DateTime CollectionStartDate { get; set; }

            [Required]
            public string CollectionTerm { get; set; }

            [Required]
            public int NoOfTerms { get; set; }
       
    }
}
