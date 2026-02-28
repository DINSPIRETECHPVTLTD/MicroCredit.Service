using MicroCredit.Application.Model.Organization;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class OrganizationExtension
{
    public static OrgResponse ToOrgResponse(this Organization org)
    {
        return  new OrgResponse
        {
            Id = org.Id,
            Name = org.Name,
            Address = $"{org.Address1} {org.Address2} {org.City} {org.State} {org.ZipCode}".Trim(),
            PhoneNumber = org.PhoneNumber ?? string.Empty
        };
    }
}
