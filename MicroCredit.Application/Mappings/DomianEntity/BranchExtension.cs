using MicroCredit.Application.Model.Organization;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class BranchExtension
{
    public static BranchResponse ToBranchResponse(this Branch branch)
    {
        return new BranchResponse
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = $"{branch.Address1} {branch.Address2} {branch.City} {branch.State} {branch.Country} {branch.ZipCode}".Trim(),
            PhoneNumber = branch.PhoneNumber ?? string.Empty
        };
    }
}
