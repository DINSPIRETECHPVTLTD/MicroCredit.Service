using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Poc;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class PocExtension
{
    public static PocResponse ToPocResponse(this POC poc)
    {
        return new PocResponse
        {
            Id = poc.Id,
            FirstName = poc.FirstName,
            MiddleName = poc.MiddleName,
            LastName = poc.LastName,
            PhoneNumber = poc.PhoneNumber,
            AltPhone = poc.AltPhone,
            Address1 = poc.Address1,
            Address2 = poc.Address2,
            City = poc.City,
            State = poc.State,
            ZipCode = poc.ZipCode,
            CenterId = poc.CenterId,
            CreatedBy = poc.CreatedBy,
            CollectionDay = poc.CollectionDay,
            CollectionFrequency = poc.CollectionFrequency,
            CollectionBy = poc.CollectionBy,
            CreatedAt = poc.CreatedAt,
            CenterName = poc.Center?.Name ?? string.Empty
        };
    }

    public static IEnumerable<PocResponse> ToPocResponses(this IEnumerable<POC> pocList)
    {
        return pocList.Select(p => p.ToPocResponse());
    }
}