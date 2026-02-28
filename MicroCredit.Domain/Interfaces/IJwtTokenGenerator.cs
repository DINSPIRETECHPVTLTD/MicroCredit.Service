using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
