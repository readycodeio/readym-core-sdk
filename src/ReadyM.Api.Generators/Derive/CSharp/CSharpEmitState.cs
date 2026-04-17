using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal class CSharpEmitState(StringBuilder sb, CSharpMethodState methodState)
{
    private struct CurrentVarEntry
    {
        public string VarName;
        public ITypeSymbol Type;
    }

    private struct IndentEntry
    {
        public bool EmitBlock;
        public bool Indent;
    }
    
    public struct IndentContext : IDisposable
    {
        private CSharpEmitState? _owner;
        
        public IndentContext(CSharpEmitState owner, bool emitBlock)
        {
            _owner = owner;
            _owner.PushIndent(emitBlock);
        }
        
        public void Dispose()
        {
            _owner?.PopIndent();
            _owner = null;
        }
    }

    public struct ExprContext : IDisposable
    {
        private CSharpEmitState? _owner;
        private readonly bool _paren;
        
        public ExprContext(CSharpEmitState owner, bool paren)
        {
            _owner = owner;
            _paren = paren;
            if (paren)
                _owner.Append("(");
        }
        
        public void Dispose()
        {
            if (_paren)
                _owner?.Append(")");
            _owner = null;
        }
    }

    public struct CurrentVarContext : IDisposable
    {
        private CSharpEmitState? _owner;
        
        public CurrentVarContext(CSharpEmitState owner, string varName, ITypeSymbol type)
        {
            _owner = owner;
            _owner.PushCurrent(varName, type);
        }
        
        public void Dispose()
        {
            _owner?.PopCurrent();
            _owner = null;
        }
    }

    public readonly CSharpMethodState MethodState = methodState;
    public CSharpClassState ClassState => MethodState.ClassState;
    public CSharpModuleState ModuleState => MethodState.ModuleState;
    
    private string _prefix = string.Empty;
    private bool _atNewLine = true;
    private readonly List<IndentEntry> _indentStack = [];
    private readonly List<CurrentVarEntry> _currentVarStack = [];
    private string? _generatedPropertyName;
    private readonly StringBuilder _sb = sb;
    
    public void Append(string s)
    {
        if (_atNewLine)
        {
            _sb.Append(_prefix);
            _atNewLine = false;
        }
        
        _sb.Append(s);
    }

    public void AppendLine(string s)
    {
        if (_atNewLine)
        {
            _sb.Append(_prefix);
            _atNewLine = false;
        }

        _sb.AppendLine(s);
        _atNewLine = true;
    }
    
    public void AppendLine()
        => AppendLine(string.Empty);

    public void ResetIndent(string s)
    {
        _indentStack.Clear();
        _prefix = s;
    }

    private void PushIndent(bool emitBlock)
    {
        IndentEntry? prevEntry = null;
        if (_indentStack.Count > 0)
            prevEntry = _indentStack[_indentStack.Count - 1];

        var entry = new IndentEntry()
        {
            EmitBlock = emitBlock,
            Indent = true,
        };
        
        if (prevEntry is { EmitBlock: false })
        {
            prevEntry = new IndentEntry()
            {
                EmitBlock = prevEntry.Value.EmitBlock,
                Indent = false,
            };
            _indentStack[_indentStack.Count - 1] = prevEntry.Value;
            
            _prefix = _prefix.Substring(0, _prefix.Length - 4);
        }
        
        if (emitBlock)
            AppendLine("{");
        
        _indentStack.Add(entry);
        _prefix += "    ";
    }

    private void PopIndent()
    {
        if (_indentStack.Count <= 0)
            throw new InvalidOperationException("Cannot pop indent when indent level is 0.");
        
        var entry = _indentStack[_indentStack.Count - 1];
        _indentStack.RemoveAt(_indentStack.Count - 1);
        
        if (entry.Indent)
            _prefix = _prefix.Substring(0, _prefix.Length - 4);
        
        if (entry.EmitBlock)
            AppendLine("}");
    }
    
    public IndentContext WithIndent()
        => new(this, emitBlock: false);

    public IndentContext WithCodeBlock()
        => new(this, emitBlock: true);

    public ExprContext WithExpr(bool paren = true)
        => new(this, paren);
    
    public string CurrentVar
        => _currentVarStack.Count > 0 ? _currentVarStack[_currentVarStack.Count - 1].VarName : throw new InvalidOperationException("No current entry in context.");

    public ITypeSymbol CurrentType
        => _currentVarStack.Count > 0 ? _currentVarStack[_currentVarStack.Count - 1].Type : throw new InvalidOperationException("No current entry in context.");

    public void ResetCurrent(string varName, ITypeSymbol symbol)
    {
        _currentVarStack.Clear();
        PushCurrent(varName, symbol);
    }

    private void PushCurrent(string varName, ITypeSymbol symbol)
    {
        _currentVarStack.Add(new CurrentVarEntry()
        {
            VarName = varName,
            Type = symbol,
        });
    }
    
    private void PopCurrent()
    {
        if (_currentVarStack.Count == 0)
            throw new InvalidOperationException("Cannot pop current var when value stack is empty.");
        _currentVarStack.RemoveAt(_currentVarStack.Count - 1);
    }
    
    public CurrentVarContext WithCurrent(string varName, ITypeSymbol varType)
        => new(this, varName, varType);

    public string GeneratedPropertyName
    {
        get
        {
            if (_generatedPropertyName == null)
                throw new InvalidOperationException("Generated property name is not set for current member.");
            return _generatedPropertyName;
        }
    }

    public void SetGeneratedPropertyName(string name)
        => _generatedPropertyName = name;
}