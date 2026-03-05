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
    public class MasterLookupRepository: GenericRepository<MasterLookup>, IMasterLookupRepository
    {
        public MasterLookupRepository(MicroCreditDbContext context) : base(context) { }

       
        public async Task<IEnumerable<MasterLookup>> GetMasterLookupAsync(string lookupKey, CancellationToken cancellationToken = default)
        {
            return await _context.MasterLookups.Where(m =>  m.LookupKey == lookupKey && m.IsActive== true)
                .ToListAsync(cancellationToken);
        }
    }
}
