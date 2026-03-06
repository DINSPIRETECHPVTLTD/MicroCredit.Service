namespace MicroCredit.Domain.Model.Center;

public class CenterResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}
