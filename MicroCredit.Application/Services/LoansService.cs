using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Loan;
using MicroCredit.Application.Mappings.DomianEntity;
namespace MicroCredit.Application.Services;

public class LoansService : ILoansService
{
    private readonly IUnitOfWork _unitOfWork;

    public LoansService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Loans.GetAllAsync(cancellationToken)).ToLoanResponses();
    }

    public async Task<LoanResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(id, cancellationToken)
              ?? throw new KeyNotFoundException($"Loan with Id {id} not found");

        return loan.ToLoanResponse();
    }
}