using MicroCredit.Domain.Model.Branch;
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
            Address1= branch.Address1 ?? string.Empty,
            Address2=branch.Address2 ?? string.Empty,
            City=branch.City ?? string.Empty,
            State=branch.State ?? string.Empty,
            ZipCode=branch.ZipCode ?? string.Empty,
            Country=branch.Country ?? string.Empty,            
            PhoneNumber = branch.PhoneNumber ?? string.Empty
        };
    }

        public static IEnumerable<BranchResponse> ToBranchResponses(this IEnumerable<Branch> branchList)
        {
            return branchList.Select(b => b.ToBranchResponse());
    }
}
