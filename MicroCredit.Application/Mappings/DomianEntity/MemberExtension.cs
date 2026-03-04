using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class MemberExtension
{
    public static MemberResponse ToMemberResponse(this Member m)
        => new MemberResponse
        {
            Id = m.Id,
            FirstName = m.FirstName,
            SurName = m.LastName,
            PhoneNumber = m.PhoneNumber
        };

    public static IEnumerable<MemberResponse> ToMemberResponses(this IEnumerable<Member> members)
        => members.Select(m => m.ToMemberResponse());
}
