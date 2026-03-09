using MicroCredit.Domain.Model.Master;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface IPaymentTermService
    {
        Task<IEnumerable<PaymentTermResponse>> GetPaymentTermAsync(CancellationToken cancellationToken = default);
    }
}
