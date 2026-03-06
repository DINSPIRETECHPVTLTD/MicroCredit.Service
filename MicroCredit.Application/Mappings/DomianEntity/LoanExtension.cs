using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Loan;
namespace MicroCredit.Application.Mappings.DomianEntity;

public static class LoanExtension
{
    public static LoanResponse ToLoanResponse(this Loan loan)
    {
        return new LoanResponse
        {
            Id = loan.Id,
            MemberId = loan.MemberId,
            LoanAmount = loan.LoanAmount,
            InterestAmount = loan.InterestAmount,
            Status = loan.Status,
            DisbursementDate = loan.DisbursementDate,
            ClosureDate = loan.ClosureDate
        };

    }
    public static IEnumerable<LoanResponse> ToLoanResponses(this IEnumerable<Loan> loanList)
    {
        return loanList.Select(l => l.ToLoanResponse());
    }


}