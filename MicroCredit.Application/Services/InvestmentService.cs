using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Model.Fund;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Application.Utilities;

namespace MicroCredit.Application.Services
{
    public class InvestmentService : IInvestmentsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerRecordService _ledgerRecordService;
        private readonly IUsersService _usersService;
        private readonly IUserContext _userContext;

        public InvestmentService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService, IUsersService usersService, IUserContext userContext)
        {
            _unitOfWork = unitOfWork;
            _ledgerRecordService = ledgerRecordService;
            _usersService = usersService;
            _userContext = userContext;
        }

        public async Task<IEnumerable<InvestmentResponse>> GetInvestmentsAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .Investments
                .GetInvestmentsAsync(orgId, cancellationToken))
                .ToInvestmentResponses();
        }

        public async Task<int> CreateInvestmentAsync(CreateInvestmentRequest request, int createdByUserId, CancellationToken cancellationToken = default)
        {
            var investmentDate = ClientDateTimeConverter.NormalizeForStorage(request.InvestmentDate, _userContext.TimeZoneId);
            var createdDate = ClientDateTimeConverter.NormalizeForStorage(request.CreatedDate, _userContext.TimeZoneId);

            var investment = new Investment(
                userId: request.UserId,
                amount: request.Amount,
                investmentDate: investmentDate,
                createdById: createdByUserId,
                createdDate: createdDate);

            await _unitOfWork.Investments.AddInvestmentAsync(investment, cancellationToken);

            await _unitOfWork.CompleteAsync();

            var investor = await _usersService.GetByIdAsync(request.UserId, cancellationToken);
            var investorLabel = investor is null
                ? $"UserId {request.UserId}"
                : $"{investor.FirstName} {investor.Surname}".Trim();

            var investmentComment = $"Investment of {request.Amount} from {investorLabel}";

            await _ledgerRecordService.RecordInvestmentAsync(
                request.UserId,
                request.Amount,
                investmentDate,
                createdByUserId,
                createdDate,
                "Investment",
                referenceId: investment.Id,
                comments: investmentComment,
                cancellationToken);

            return investment.Id;
        }
    }
}
