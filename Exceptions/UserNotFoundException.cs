namespace TreePassBot.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(ulong qqId)
        : base($"User with QQ ID {qqId} not found.")
    {
    }

    public UserNotFoundException(string message)
        : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}