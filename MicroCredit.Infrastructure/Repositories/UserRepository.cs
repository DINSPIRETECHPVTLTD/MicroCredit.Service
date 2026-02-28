using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(MicroCreditDbContext context) : base(context) { }

    
}
