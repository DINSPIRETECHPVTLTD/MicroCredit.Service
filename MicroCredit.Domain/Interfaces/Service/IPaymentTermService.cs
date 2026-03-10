using MicroCredit.Domain.Model.PaymentTerm;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IPaymentTermService
{
    Task<IEnumerable<PaymentTermResponse>> GetAllPaymentTermsAsync(CancellationToken cancellationToken = default);

    Task<PaymentTermResponse?> GetPaymentTermByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreatePaymentTermAsync(CreatePaymentTermRequest request, int userId, CancellationToken cancellationToken = default);

    Task<bool> UpdatePaymentTermAsync(int id, UpdatePaymentTermRequest request, int userId, CancellationToken cancellationToken = default);

    Task<bool> DeletePaymentTermAsync(int id, CancellationToken cancellationToken = default);
}