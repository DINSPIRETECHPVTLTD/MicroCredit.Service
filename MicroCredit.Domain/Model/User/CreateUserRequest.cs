namespace MicroCredit.Domain.Model.User;

public class CreateUserRequest : UserBaseRequest
{
    public string Password { get; private set; } = string.Empty;
}
