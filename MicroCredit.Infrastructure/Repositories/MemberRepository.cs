using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Infrastructure.Repositories
{
    public class MemberRespository : GenericRepository<Member>, IMemberRepository
    {
        public MemberRespository(MicroCreditDbContext context) : base(context) { }

        public async Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Members
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        }
        public async Task<IEnumerable<Member>> GetBranchMembersAsync(int branchid, CancellationToken cancellationToken = default)
        {
            return await _context.Members
                .Where(m => m.branchid && !m.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public Task CreateAsync(Member member, CancellationToken cancellationToken = default)
        {
            _context.Members.Add(member);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
        {
            _context.Members.Update(member);
            return Task.CompletedTask;
        }

    }

}
