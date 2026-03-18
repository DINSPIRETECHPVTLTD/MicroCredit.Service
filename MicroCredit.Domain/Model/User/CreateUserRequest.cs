namespace MicroCredit.Domain.Model.User;

public class CreateUserRequest : UpdateUserRequest
{
    public string Password { get; set; } = string.Empty;
}
