using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal class CppEmitState(StringBuilder sb, CppModuleState moduleState)
{
    private struct CurrentVarEntry
    {
        public string VarName;
        public ITypeSymbol SourceType;
        public string CppType;
    }

    private struct IndentEntry
    {
        public bool EmitBlock;
        public bool Indent;
    }
    
    public struct IndentContext : IDisposable
    {
        private CppEmitState? _owner;
        
        public IndentContext(CppEmitState owner, bool emitBlock)
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
        private CppEmitState? _owner;
        private readonly bool _paren;
        
        public ExprContext(CppEmitState owner, bool paren)
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
        private CppEmitState? _owner;
        
        public CurrentVarContext(CppEmitState owner, string varName, ITypeSymbol type, string cppType)
        {
            _owner = owner;
            _owner.PushCurrent(varName, type, cppType);
        }
        
        public void Dispose()
        {
            _owner?.PopCurrent();
            _owner = null;
        }
    }

    public readonly CppModuleState ModuleState = moduleState;
    
    private readonly Dictionary<string, int> _varCounters = [];
    private readonly List<CurrentVarEntry> _currentVarStack = [];
    
    private string _prefix = string.Empty;
    private bool _atNewLine = true;
    private readonly List<IndentEntry> _indentStack = [];
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

    public ExprContext WithExpr(bool paren)
        => new(this, paren);
    
    public string NewVarName(string name)
    {
        if (!_varCounters.TryGetValue(name, out var index))
        {
            index = 0;
            _varCounters.Add(name, 0);
        }
        
        _varCounters[name] = index + 1;
        return name + index;
    }

    public string CurrentVar
        => _currentVarStack.Count > 0 ? _currentVarStack[_currentVarStack.Count - 1].VarName : throw new InvalidOperationException("No current entry in context.");

    public ITypeSymbol CurrentType
        => _currentVarStack.Count > 0 ? _currentVarStack[_currentVarStack.Count - 1].SourceType : throw new InvalidOperationException("No current entry in context.");

    public string CurrentCppType
        => _currentVarStack.Count > 0 ? _currentVarStack[_currentVarStack.Count - 1].CppType : throw new InvalidOperationException("No current entry in context.");

    public void PushCurrent(string varName, ITypeSymbol origType, string cppType)
    {
        _currentVarStack.Add(new CurrentVarEntry()
        {
            VarName = varName,
            SourceType = origType,
            CppType = cppType,
        });
    }
    
    public void PopCurrent()
    {
        if (_currentVarStack.Count == 0)
            throw new InvalidOperationException("Cannot pop current var when value stack is empty.");
        _currentVarStack.RemoveAt(_currentVarStack.Count - 1);
    }
    
    public CurrentVarContext WithCurrent(string varName, ITypeSymbol sourceType, string cppVarType)
        => new(this, varName, sourceType, cppVarType);
}