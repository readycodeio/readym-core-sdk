using System;
using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal class CppModuleState()
{
    public readonly struct CppInclude(string path, bool angleBrackets) : IEquatable<CppInclude>, IComparable<CppInclude>
    {
        public readonly string Path = path;
        public readonly bool AngleBrackets = angleBrackets;

        public bool Equals(CppInclude other)
            => Path == other.Path && AngleBrackets == other.AngleBrackets;

        public override bool Equals(object? obj)
            => obj is CppInclude other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return (Path.GetHashCode() * 397) ^ AngleBrackets.GetHashCode(); }
        }

        public int CompareTo(CppInclude other)
        {
            var pathComparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            if (pathComparison != 0) return pathComparison;
            return AngleBrackets.CompareTo(other.AngleBrackets);
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