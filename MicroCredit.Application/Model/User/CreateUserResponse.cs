namespace MicroCredit.Application.Model.User;

public class CreateUserResponse : UserBaseResponse
{
    public string Password { get; private set; } = string.Empty;
}
