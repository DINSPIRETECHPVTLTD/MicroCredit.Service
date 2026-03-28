using MicroCredit.Application.Services;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCredit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IBranchsService, BranchsService>();
        services.AddScoped<ILoansService, LoansService>();
        services.AddScoped<ILoanSchedulerService, LoanSchedulerService>();
        services.AddScoped<IPOCService, POCService>();
        services.AddScoped<IInvestmentsService, InvestmentService>();
        services.AddScoped<ILedgerBalanceService, LedgerBalanceService>();
        services.AddScoped<ILedgerRecordService, LedgerRecordService>();
        services.AddScoped<ILedgerTransactionService, LedgerTransactionService>();
        services.AddScoped<IMasterLookupservice, MasterLookupservice>();
        services.AddScoped<IPaymentTermService, PaymentTermService>();
        services.AddScoped<ICenterService, CenterService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMemberMembershipFeeService, MemberMembershipFeeService>();
        services.AddScoped<ILoanSchedulerService, LoanSchedulerService>();
        services.AddScoped<IRecoveryPostingService, RecoveryPostingService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
