using System;
using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal class CppModuleState()
{
    public readonly struct CppInclude(string ns, bool angleBrackets) : IEquatable<CppInclude>
    {
        public readonly string Namespace = ns;
        public readonly bool AngleBrackets = angleBrackets;

        public bool Equals(CppInclude other)
            => Namespace == other.Namespace && AngleBrackets == other.AngleBrackets;

        public override bool Equals(object? obj)
            => obj is CppInclude other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return (Namespace.GetHashCode() * 397) ^ AngleBrackets.GetHashCode(); }
        }
    }
    
    private readonly List<CppInclude> _includes = [];
    
    public IReadOnlyList<CppInclude> Includes => _includes;
    
    public void AddInclude(string ns, bool angleBrackets)
    {
        var include = new CppInclude(ns, angleBrackets);
        if (!_includes.Contains(include))
            _includes.Add(include);
    }

    public void AddIncludeList(List<(string ns, bool angleBrackets)> list)
    {
        foreach (var (ns, angleBrackets) in list)
        {
            AddInclude(ns, angleBrackets);
        }
    }
}