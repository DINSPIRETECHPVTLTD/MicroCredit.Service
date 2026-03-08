using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public  interface IMasterLookupRepository
    {
        Task<IEnumerable<MasterLookup>> GetMasterLookupAsync(string lookupKey, CancellationToken cancellationToken = default);


    }
}
