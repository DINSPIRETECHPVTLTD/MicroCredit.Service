using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.RecoveryPosting;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IRecoveryPostingService
{
    Task<IReadOnlyList<RecoveryPostingSchedulerResponse>> GetSchedulersAsync(
        RecoveryPostingSchedulersRequest request,
        IUserContext userContext,
        CancellationToken cancellationToken = default);

    Task<RecoveryPostingPostResponse> PostRecoveriesAsync(
        RecoveryPostingPostRequest request,
        IUserContext userContext,
        CancellationToken cancellationToken = default);
}
