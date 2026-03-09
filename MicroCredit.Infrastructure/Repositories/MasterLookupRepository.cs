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
            var query = _context.MasterLookups;
            if (!string.IsNullOrEmpty(lookupKey))
                query.Where(m => m.LookupKey == lookupKey);

            return await query.Where(m=>m.IsActive==true).OrderBy(m => m.LookupKey).ThenBy(m => m.SortOrder)
                .ToListAsync(cancellationToken);
        }
    }
}
