using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Branch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.ApplicationModel
{
    public static class CreateBranchRequestExtensions
    {
        /// <summary>Maps to entity.</summary>
            public static Branch ToBranch(this CreateBranchRequest request, int orgId, int createdBy)
            {
                return new Branch(
                request.Name,
                orgId,
                createdBy,
                request.Address1,
                request.Address2,
                request.City,
                request.State,
                request.Country,
                request.ZipCode,
                request.PhoneNumber);

        }
    }
}
