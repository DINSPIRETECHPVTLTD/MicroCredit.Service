using MicroCredit.Application.Mappings;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Application.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork unitOfWork;

    public MemberService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<MemberResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await unitOfWork.Members.GetByIdAsync(id, cancellationToken);
        return member?.ToMemberResponse();
    }

    public async Task<IEnumerable<MemberResponse>> GetMembersByBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var members = await unitOfWork.Members.GetMembersByBranchAsync(branchId, cancellationToken);
        if (members == null)
            return Enumerable.Empty<MemberResponse>();
        return members.Select(m => m.ToMemberResponse());
    }

    public async Task<MemberResponse> CreateAsync(CreateMemberRequest request, IUserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.UserId == 0)
            throw new UnauthorizedAccessException("User context is required.");

        var aadhaar = request.Aadhaar?.Trim();
        if (!string.IsNullOrWhiteSpace(aadhaar))
        {
            var aadhaarExists = await unitOfWork.Members.ExistsByAadhaarAsync(aadhaar, cancellationToken: cancellationToken);
            if (aadhaarExists)
                throw new InvalidOperationException("Member already exists with this Aadhaar number.");
        }

        var dob = request.Dob.HasValue ? DateOnly.FromDateTime(request.Dob.Value) : (DateOnly?)null;
        var guardianDob = request.GuardianDob.HasValue ? DateOnly.FromDateTime(request.GuardianDob.Value) : (DateOnly?)null;

        var entity = new Member(
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber,
            centerId: request.CenterId,
            pocId: request.PocId,
            createdBy: userContext.UserId,
            guardianFirstName: request.GuardianFirstName,
            guardianLastName: request.GuardianLastName,
            guardianPhone: request.GuardianPhone ?? string.Empty,
            age: request.Age,
            guardianAge: request.GuardianAge,
            middleName: request.MiddleName,
            altPhone: request.AltPhone,
            address1: request.Address1,
            address2: request.Address2,
            city: request.City,
            state: request.State,
            zipCode: request.ZipCode,
            aadhaar: aadhaar,
            occupation: request.Occupation,
            relationship: request.Relationship,
            dob: dob,
            guardianMiddleName: request.GuardianMiddleName,
            guardianDob: guardianDob
        );

        await unitOfWork.Members.CreateAsync(entity, cancellationToken);
        await unitOfWork.CompleteAsync();
        return entity.ToMemberResponse();
    }

    public async Task<MemberResponse> UpdateAsync(int id, UpdateMemberRequest request, IUserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.UserId == 0)
            throw new UnauthorizedAccessException("User context is required.");

        var member = await unitOfWork.Members.GetByIdAsync(id, cancellationToken);
        if (member == null)
            throw new Exception("Member not found");

        var dob = request.Dob.HasValue ? DateOnly.FromDateTime(request.Dob.Value) : (DateOnly?)null;
        var guardianDob = request.GuardianDob.HasValue ? DateOnly.FromDateTime(request.GuardianDob.Value) : (DateOnly?)null;

        member.UpdateDetails(
            centerId: request.CenterId,
            pocId: request.PocId,
            firstName: request.FirstName,
            middleName: request.MiddleName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber,
            altPhone: request.AltPhone,
            address1: request.Address1,
            address2: request.Address2,
            city: request.City,
            state: request.State,
            zipCode: request.ZipCode,
            aadhaar: request.Aadhaar,
            occupation: request.Occupation,
            relationship: request.Relationship,
            dob: dob,
            age: request.Age,
            guardianFirstName: request.GuardianFirstName,
            guardianMiddleName: request.GuardianMiddleName,
            guardianLastName: request.GuardianLastName,
            guardianPhone: request.GuardianPhone ?? string.Empty,
            guardianDob: guardianDob,
            guardianAge: request.GuardianAge,
            modifiedBy: userContext.UserId
        );

        await unitOfWork.Members.UpdateAsync(member, cancellationToken);
        await unitOfWork.CompleteAsync();
        return member.ToMemberResponse();
    }

    public async Task<bool> MarkAsInactiveAsync(int id, int modifiedBy, CancellationToken cancellationToken = default)
    {
        var member = await unitOfWork.Members.GetByIdAsync(id, cancellationToken);
        if (member == null)
            throw new Exception("Member not found");
        member.MarkDeleted(modifiedBy);
        await unitOfWork.Members.UpdateAsync(member, cancellationToken);
        await unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<IEnumerable<MemberResponse>> SearchMemebersByBranchAsync(SearchMemberRequest request, CancellationToken cancellationToken = default)
    {
        var members = await unitOfWork.Members.SearchMembersByBranchAsync(request, cancellationToken);
        if (members == null)
            return Enumerable.Empty<MemberResponse>();
        return members.Select(m => m.ToMemberResponse());
    }
}