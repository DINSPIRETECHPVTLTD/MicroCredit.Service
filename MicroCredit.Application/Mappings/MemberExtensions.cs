using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Application.Mappings;

public static class MemberExtensions
{
    public static MemberResponse ToMemberResponse(this Member member)
    {
        return new MemberResponse
        {
            Id = member.Id,
            FirstName = member.FirstName,
            MiddleName = member.MiddleName,
            LastName = member.LastName,
            PhoneNumber = member.PhoneNumber,
            AltPhone = member.AltPhone,
            Address1 = member.Address1,
            Address2 = member.Address2,
            City = member.City,
            State = member.State,
            ZipCode = member.ZipCode,
            CenterId = member.CenterId,
            BranchId = member.Center?.BranchId,
            Aadhaar = member.Aadhaar,
            Occupation = member.Occupation,
            Relationship = member.Relationship,
            DOB = member.DOB.HasValue ? member.DOB.Value.ToDateTime(TimeOnly.MinValue) : null,
            Age = member.Age,
            GuardianFirstName = member.GuardianFirstName,
            GuardianMiddleName = member.GuardianMiddleName,
            GuardianLastName = member.GuardianLastName,
            GuardianPhone = member.GuardianPhone,
            GuardianDOB = member.GuardianDOB.HasValue ? member.GuardianDOB.Value.ToDateTime(TimeOnly.MinValue) : null,
            GuardianAge = member.GuardianAge,
            POCId = member.POCId,
            Center = member.Center?.Name,
            Poc = member.POC != null
                ? string.Join(" ", new[] { member.POC.FirstName, member.POC.MiddleName, member.POC.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : null
        };
    }
}
