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
    public static class UpdateCenterRequestExtentions
    {
        public static Center ToCenter(this UpdateCenterRequest response, Center existingCenter, int modifiedBy)
        {
            existingCenter.UpdateDetails(response.Name,
                response.Address,
                response.City,
                modifiedBy);
            return existingCenter;
        }
    }
}
