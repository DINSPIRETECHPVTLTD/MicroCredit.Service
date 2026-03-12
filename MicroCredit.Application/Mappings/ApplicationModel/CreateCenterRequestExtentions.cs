using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.ApplicationModel
{
    public static class CreateCenterRequestExtentions
    {
        public static Center ToCenter(this CreateCenterRequest request, int branchid, int createdBy)
        {
            return new Center(
            request.Name,
            branchid,
            createdBy,
            request.Address,
            request.City);

        }
    }
}
