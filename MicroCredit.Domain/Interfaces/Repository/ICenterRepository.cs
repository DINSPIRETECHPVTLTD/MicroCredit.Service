using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface ICenterRepository
    {
        Task<IEnumerable<Center>> GetCenters(int branchId, CancellationToken cancellationToken = default);
        Task<Center> GetByCenterId(int centerId, CancellationToken cancellationToken = default);
        Task CreateAsync(Center center, CancellationToken cancellationToken = default);
        Task UpdateAsync(Center center, CancellationToken cancellationToken = default);
    }
}
