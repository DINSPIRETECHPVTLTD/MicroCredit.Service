using MicroCredit.Application.Model.User;

namespace MicroCredit.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse?> GetorgUserAsync(int id, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetBranchUserAsync(UserRequest request, CancellationToken cancellationToken = default);
    }
}

