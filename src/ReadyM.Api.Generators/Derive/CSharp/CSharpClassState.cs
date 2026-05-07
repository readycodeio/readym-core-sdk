using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal class CSharpClassState(CSharpModuleState moduleState)
{
    public struct MemberEntry(string memberName, ITypeSymbol memberType, bool isStatic, bool isThreadStatic)
    {
        public readonly string MemberName = memberName;
        public readonly ITypeSymbol MemberType = memberType;
        public readonly bool IsStatic = isStatic;
        public readonly bool IsThreadStatic = isThreadStatic;
    }
    
    public readonly CSharpModuleState ModuleState = moduleState;
    private readonly List<MemberEntry> _members = [];
    private readonly Dictionary<string, int> _memberCounters = [];
    
    public IReadOnlyList<MemberEntry> Members => _members;
    
    public string NewMemberName(string name)
    {
        if (!_memberCounters.TryGetValue(name, out var index))
        {
            index = 0;
            _memberCounters.Add(name, 0);
        }

        _memberCounters[name] = index + 1;
        return name + index;
    }
    
    public string AddMember(string name, ITypeSymbol type, bool isStatic = false, bool threadStatic = false)
    {
        var memberName = NewMemberName(name);
        _members.Add(new MemberEntry(memberName, type, isStatic, threadStatic));
        return memberName;
    }

    public string AddTempThreadStatic(ITypeSymbol symbol)
        => AddMember("_threadLocal", symbol, isStatic: true, threadStatic: true);
}