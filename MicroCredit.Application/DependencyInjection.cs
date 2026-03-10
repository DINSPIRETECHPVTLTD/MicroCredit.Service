using MicroCredit.Application.Services;
using MicroCredit.Domain.Interfaces.Repository;
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
        services.AddScoped<IPOCService, POCService>();
        services.AddScoped<IInvestmentsService, InvestmentService>();
        services.AddScoped<ILedgerBalanceService, LedgerBalanceService>();
        services.AddScoped<ILedgerTransactionService, LedgerTransactionService>();
        services.AddScoped<IMasterLookupservice, MasterLookupservice>();
        services.AddScoped<IPaymentTermService, PaymentTermService>();       
        return services;
    }
}
