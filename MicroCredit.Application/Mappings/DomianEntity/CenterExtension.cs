using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Center;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class CenterExtension
{
    public static CenterResponse ToCenterResponse(this Center center)
    {
        return new CenterResponse
        {
            Id = center.Id,
            Name = center.Name,
            Address = $"{center.CenterAddress}, {center.City}",
           
        };
    }

    public static IEnumerable<CenterResponse> ToCenterResponses(this IEnumerable<Center> centers)
    {
        return centers.Select(c => c.ToCenterResponse());
    }
}
