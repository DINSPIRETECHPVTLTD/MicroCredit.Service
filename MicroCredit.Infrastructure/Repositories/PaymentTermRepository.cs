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

        public async Task<PaymentTerm?> GetPaymentTermByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.PaymentTerms.FirstOrDefaultAsync(p => p.PaymentTermId == id && !p.IsDeleted, cancellationToken);
        }

        public async Task AddAsync(PaymentTerm entity, CancellationToken cancellationToken = default)
        {
            await _context.PaymentTerms.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(PaymentTerm entity, CancellationToken cancellationToken = default)
        {
            _context.PaymentTerms.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var paymentTerm = await _context.PaymentTerms.FirstOrDefaultAsync(p => p.PaymentTermId == id && !p.IsDeleted, cancellationToken);
            if (paymentTerm != null)
            {
                paymentTerm.IsDeleted = true;
                _context.PaymentTerms.Update(paymentTerm);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
