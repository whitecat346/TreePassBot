namespace TreePassBot.Handlers.Commands.Permission;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequiredPremission : Attribute
{
    /// <summary>
    /// Required user roles to execute the command.
    /// </summary>
    public UserRoles RequiredRoles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredPremission"/> class.
    /// </summary>
    /// <param name="requiredRoles">The required user roles.</param>
    public RequiredPremission(UserRoles requiredRoles)
    {
        RequiredRoles = requiredRoles;
    }
}