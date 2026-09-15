namespace ReadyM.SDK.Entity;

public class InvalidEntityException : Exception
{
    public InvalidEntityException() { }

    public InvalidEntityException(string message) : base(message) { }
}

/// <summary>
/// The entity is alive but its archetype does not carry the component the accessor reads.
/// </summary>
public class ComponentNotFoundException(string message) : Exception(message);
