using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Master;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class MasterLookupExtention
    {
        public static LookupResponse ToMasterLookupResponse(this MasterLookup lookup)
        {
            return new LookupResponse
            {
                Id = lookup.Id,
                LookupKey=lookup.LookupKey,
                LookupValue=lookup.LookupValue, 
                LookupCode=lookup.LookupCode,
                NumericValue=lookup.NumericValue.HasValue ? (int)lookup.NumericValue.Value : (int?)null,
                SortOrder=lookup.SortOrder,
                };
        }

        public static IEnumerable<LookupResponse> ToMasterLookupResponses(this IEnumerable<MasterLookup> userList)
        {
            return userList.Select(u => u.ToMasterLookupResponse());
        }
    }
}
