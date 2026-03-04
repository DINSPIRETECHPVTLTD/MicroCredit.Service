using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;

namespace MicroCredit.Infrastructure.Repositories;

internal class InMemoryMembersRepository : IMembersRepository
{
    private readonly List<Member> _store = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public Task CreateAsync(Member member, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // assign id if not set via reflection since Member.Id has private setter; skip setting Id
            _store.Add(member);
        }
        return Task.CompletedTask;
    }

    public Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var m = _store.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(m);
        }
    }

    public Task<IEnumerable<Member>> GetMembers(int branchId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // For testing ignore branchId
            return Task.FromResult(_store.AsEnumerable());
        }
    }

    public Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        // In-memory entity references are mutated by caller; nothing to do
        return Task.CompletedTask;
    }
}
