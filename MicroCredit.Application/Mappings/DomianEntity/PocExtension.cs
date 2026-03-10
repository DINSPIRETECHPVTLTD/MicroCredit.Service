
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
            CenterId = poc.CenterId,
            FirstName = poc.FirstName,
            MiddleName = poc.MiddleName,
            LastName = poc.LastName,

            PhoneNumber = poc.PhoneNumber ?? string.Empty,

            Address1 = poc.Address1,
            Address2 = poc.Address2,
            City = poc.City,
            State = poc.State,
            ZipCode = poc.ZipCode
        };
       
    }
    public static IEnumerable<PocResponse> ToPocResponses(this IEnumerable<POC> pocList)
    {
        return pocList.Select(p => p.ToPocResponse());
    }

}
