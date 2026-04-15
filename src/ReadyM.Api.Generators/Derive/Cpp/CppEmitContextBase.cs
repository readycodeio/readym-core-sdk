using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal abstract class CppEmitContextBase(CppEmitState state)
{
    public readonly CppEmitState State = state;
    
    public void Append(string s)
        => State.Append(s);
    
    public void AppendLine(string s)
        => State.AppendLine(s);

    public void AppendLine()
        => State.AppendLine();

    public CppEmitState.CurrentVarContext WithCurrent(string varName, ITypeSymbol sourceType, string cppVarType)
        => State.WithCurrent(varName, sourceType, cppVarType);
    
    public CppEmitState.IndentContext WithCodeBlock()
        => State.WithCodeBlock();
    
    public CppEmitState.IndentContext WithIndent()
        => State.WithIndent();
    
    public CppEmitState.ExprContext WithExpr(bool paren)
        => State.WithExpr(paren);
}