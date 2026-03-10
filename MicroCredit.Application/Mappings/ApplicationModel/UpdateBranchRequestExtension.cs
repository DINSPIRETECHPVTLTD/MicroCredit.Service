using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.ApplicationModel
{
    public static class UpdateBranchRequestExtension
    {
        public static Branch ToBranch(this UpdateBranchRequest response, Branch existingBranch,int modifiedBy)
        {
            existingBranch.UpdateDetails(response.Name,
                response.Address1,
                response.Address2,
                response.City,
                response.State,
                response.Country,
                response.ZipCode,
                response.PhoneNumber,
                modifiedBy);
            return existingBranch;
        }
    }
}
