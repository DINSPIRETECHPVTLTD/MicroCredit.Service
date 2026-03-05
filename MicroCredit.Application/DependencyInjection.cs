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
        services.AddScoped<IMasterLookupservice, MasterLookupservice>();
        return services;
    }
}
