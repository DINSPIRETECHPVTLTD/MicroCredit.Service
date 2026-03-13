using MicroCredit.Domain.Entities;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MicroCredit.Domain.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Infrastructure.Repositories;
public class InvestmentRepository : IInvestmentRepository
{
    private readonly MicroCreditDbContext _context;

    public InvestmentRepository(MicroCreditDbContext context)
    {
        _context = context;
    }


    public async Task<IEnumerable<Investment>> GetInvestmentsAsync(int orgId, CancellationToken cancellationToken = default)
    {

        return await _context.Investments
            .Where(i => i.User.OrgId == orgId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddInvestmentAsync(Investment investment, CancellationToken cancellationToken = default)
    {
        await _context.Investments.AddAsync(investment, cancellationToken);
    }
}
