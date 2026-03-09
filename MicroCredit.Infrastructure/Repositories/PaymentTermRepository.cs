using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories
{
    public  class PaymentTermRepository : GenericRepository<PaymentTerm>, IPaymentTermRepository
    {
        public PaymentTermRepository(MicroCreditDbContext context) : base(context) { }


        public async Task<IEnumerable<PaymentTerm>> GetPaymentTermAsync(CancellationToken cancellationToken = default)
        {  
            return await _context.PaymentTerms.Where(p => !p.IsDeleted).OrderBy(p => p.PaymentTermId)
                .ToListAsync(cancellationToken);
        }
    }
}
