using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class CenterExtention
    {
        public static CenterResponse ToCenterResponse(this Center center)
        {
            return new CenterResponse
            {
                Id = center.Id,
                Name = center.Name,
                Address = center.CenterAddress ?? string.Empty,
                City = center.City ?? string.Empty,
            };
        }

        public static IEnumerable<CenterResponse> ToCenterResponses(this IEnumerable<Center> centerList)
        {
            return centerList.Select(b => b.ToCenterResponse());
        }
    }
}
