using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface IPaymentTermRepository
    {
        Task<IEnumerable<PaymentTerm>> GetPaymentTermAsync(CancellationToken cancellationToken = default);
        Task<PaymentTerm?> GetPaymentTermByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(PaymentTerm entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(PaymentTerm entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
