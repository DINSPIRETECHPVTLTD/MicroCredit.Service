namespace MicroCredit.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    Task<int> CompleteAsync();
}
