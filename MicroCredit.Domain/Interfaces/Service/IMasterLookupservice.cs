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
    Task<IEnumerable<LookupResponse>> GetMasterLookupAsync(string? lookupKey, CancellationToken cancellationToken = default);
    Task<int> CreateMasterLookupAsync(CreateLookupRequest request, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMasterLookupAsync(int id, UpdateLookupRequest request, int userId, CancellationToken cancellationToken = default);
    Task<bool> SetInactiveAsync(int id, int userId, CancellationToken cancellationToken = default);
    }
}
