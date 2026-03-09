using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Master;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class PaymentTermService: IPaymentTermService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentTermService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<IEnumerable<PaymentTermResponse>> GetPaymentTermAsync(CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork.PaymentTerms.GetPaymentTermAsync(cancellationToken))
                .ToPaymentTermResponses();
        }
    }
}
