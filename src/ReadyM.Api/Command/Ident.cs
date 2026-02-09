namespace ReadyM.Api.Command;

public readonly struct Ident(string name)
{
    public readonly string Name = name;
    
    public override string ToString() 
        => $"Ident({Name})";
}