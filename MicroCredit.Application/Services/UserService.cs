using MicroCredit.Application.Common;
using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Mappings.ApplicationModel;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Interfaces;

namespace MicroCredit.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Users.GetOrgUsersAsync(orgId, cancellationToken)).ToUserResponses();
    }

    public async Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Users.GetBranchUsersAsync(orgId, branchId, cancellationToken)).ToUserResponses();
    }

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        return user?.ToUserResponse();
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserResponse request, IUserContext context, CancellationToken cancellationToken = default)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var entity = request.ToUser(context.OrgId, context.BranchId, context.UserId, passwordHash);
        await _unitOfWork.Users.CreateAsync(entity, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return entity.ToUserResponse();
    }

    public async Task<UserResponse> UpdateUserAsync(int id, UpdateUserResponse request, IUserContext context, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            throw new NotFoundException("User not found.");
        request.ToUser(user, context.OrgId, context.BranchId, context.UserId);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return user.ToUserResponse();
    }
}
