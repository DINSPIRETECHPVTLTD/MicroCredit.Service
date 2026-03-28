using MicroCredit.Domain.Contracts;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using MicroCredit.Infrastructure.Providers;
using MicroCredit.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCredit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddDbContext<MicroCreditDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IMasterLookupRepository, MasterLookupRepository>();
        services.AddScoped<IPOCRepository, POCRepository>();

        services.AddScoped<ILoanSchedulersRepository, LoanSchedulersRepository>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IInvestmentRepository, InvestmentRepository>();
        services.AddScoped<ILedgerBalanceRepository, LedgerBalanceRepository>();
        services.AddScoped<ILedgerTransactionRepository, LedgerTransactionRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberMembershipFeeRepository, MemberMembershipFeeRepository>();
        services.AddScoped<IRecoveryPostingRepository, RecoveryPostingRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }
}
