namespace ReadyM.SDK.Exceptions;

/// <summary>
/// Something that would change the shape of the world was asked for while a query was running.
/// </summary>
public class StructuralChangeInQueryException(string what)
    : Exception($"{what} cannot run inside a query. Collect what you need and do it after the loop.");
