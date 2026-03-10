using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.PaymentTerm;

namespace MicroCredit.Application.Services;

public class PaymentTermService : IPaymentTermService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentTermService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PaymentTermResponse>> GetAllPaymentTermsAsync(CancellationToken cancellationToken = default)
    {
        var paymentTerms = await _unitOfWork.PaymentTerms.GetPaymentTermAsync(cancellationToken);
        return paymentTerms.ToPaymentTermResponses();
    }

    public async Task<PaymentTermResponse?> GetPaymentTermByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var paymentTerm = await _unitOfWork.PaymentTerms.GetPaymentTermByIdAsync(id, cancellationToken);
        return paymentTerm?.ToPaymentTermResponse();
    }

    public async Task<int> CreatePaymentTermAsync(CreatePaymentTermRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var paymentTerm = request.ToPaymentTermEntity(userId);
        await _unitOfWork.PaymentTerms.AddAsync(paymentTerm, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return paymentTerm.PaymentTermId;
    }

    public async Task<bool> UpdatePaymentTermAsync(int id, UpdatePaymentTermRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var paymentTerm = await _unitOfWork.PaymentTerms.GetPaymentTermByIdAsync(id, cancellationToken);
        if (paymentTerm == null)
            return false;

        paymentTerm.UpdateFromRequest(request, userId);
        await _unitOfWork.PaymentTerms.UpdateAsync(paymentTerm, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<bool> DeletePaymentTermAsync(int id, CancellationToken cancellationToken = default)
    {
        var paymentTerm = await _unitOfWork.PaymentTerms.GetPaymentTermByIdAsync(id, cancellationToken);
        if (paymentTerm == null)
            return false;

        await _unitOfWork.PaymentTerms.DeleteAsync(id, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}