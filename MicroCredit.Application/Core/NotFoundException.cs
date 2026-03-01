namespace MicroCredit.Application.Core;

/// <summary>
/// Thrown when a requested entity does not exist. Map to HTTP 404 in API.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}
