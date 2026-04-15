using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal abstract class CSharpEmitContextBase(CSharpEmitState state)
{
    public readonly CSharpEmitState State = state;
    
    public void Append(string s)
        => State.Append(s);
    
    public void AppendLine(string s)
        => State.AppendLine(s);

    public void AppendLine()
        => State.AppendLine();

    public CSharpEmitState.CurrentVarContext WithCurrent(string varName, ITypeSymbol varType)
        => State.WithCurrent(varName, varType);
    
    public CSharpEmitState.IndentContext WithCodeBlock()
        => State.WithCodeBlock();
    
    public CSharpEmitState.IndentContext WithIndent()
        => State.WithIndent();
    
    public CSharpEmitState.ExprContext WithExpr(bool paren)
        => State.WithExpr(paren);
}