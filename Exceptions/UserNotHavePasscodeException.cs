namespace TreePassBot.Exceptions;

public class UserNotHavePasscodeException : Exception
{
    public UserNotHavePasscodeException(ulong qqId)
        : base($"User with QQ ID {qqId} does not have a passcode.")
    {
    }

    public UserNotHavePasscodeException(string message)
        : base(message)
    {
    }
}