using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface IMemberRepository : IGenericRepository<Member>
    {
        Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Member>> GetBranchMembersAsync(int branchid, CancellationToken cancellationToken = default);
        Task CreateAsync(Member member, CancellationToken cancellationToken = default);
        Task UpdateAsync(Member member, CancellationToken cancellationToken = default);
    }

}
