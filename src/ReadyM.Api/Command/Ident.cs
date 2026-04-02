namespace ReadyM.Api.Command;

internal readonly struct Ident(string name)
{
    public readonly string Name = name;
    
    public override string ToString() 
        => $"Ident({Name})";
}