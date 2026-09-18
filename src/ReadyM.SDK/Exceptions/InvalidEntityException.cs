namespace ReadyM.SDK.Exceptions;

public class InvalidEntityException : Exception
{
    public InvalidEntityException() { }

    public InvalidEntityException(string message) : base(message) { }
}