using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Persistence;

public class MicroCreditDbContext : DbContext
{
    public MicroCreditDbContext(DbContextOptions<MicroCreditDbContext> options)
        : base(options)
    {
    }
}
