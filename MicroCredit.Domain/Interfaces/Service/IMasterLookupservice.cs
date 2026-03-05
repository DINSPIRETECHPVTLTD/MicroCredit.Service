using MicroCredit.Domain.Model.Master;
using MicroCredit.Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface IMasterLookupservice
    {
        Task<IEnumerable<LookupResponse>> GetMasterLookupAsync(string lookupKey, CancellationToken cancellationToken = default);
    }
}
