using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

[Generator]
public class DeriveINetworkedComponentGenerator : IIncrementalGenerator
{
    private const string FloatComparisonEpsilon = "0.1f";
    private const string VectorComparisonEpsilon = "0.01f";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var codeProvider = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform);

        context.RegisterSourceOutput(codeProvider, (spc, nameAndContent) => { spc.AddSource($"{nameAndContent.Name}.g.cs", nameAndContent.Code); });
    }

    private bool Predicate(SyntaxNode syntaxNode, CancellationToken ct)
    {
        return syntaxNode is StructDeclarationSyntax { AttributeLists.Count: > 0 } structDecl
               && structDecl.AttributeLists
                   .SelectMany(a => a.Attributes)
                   .Any(attr => attr.Name is IdentifierNameSyntax { Identifier.Text: "DeriveINetworkedComponent" });
    }

    private (string Name, string Code) Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var node = context.Node;

        var model = context.SemanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
        var symbol = model.GetDeclaredSymbol(node) as INamedTypeSymbol;
        
        var mode = AttributeUtils.GetAttribute<byte>(symbol, "DeriveINetworkedComponentAttribute", "mode", (1 << 0) | (1 << 2));
        var emitDirtyMask = AttributeUtils.GetAttribute<bool>(symbol, "DeriveINetworkedComponentAttribute", "emitDirtyMask", true);

        var mapFields = (mode & (1 << 0)) != 0;
        var mapProperties = (mode & (1 << 1)) != 0;
        var mapPrivate = (mode & (1 << 2)) != 0;
        var mapPublic = (mode & (1 << 3)) != 0;
        var mapInternal = (mode & (1 << 4)) != 0;
        
        var name = symbol!.Name;
        var source = GenerateNetworkedComponent(
            symbol,
            mapFields: mapFields,
            mapProperties: mapProperties, 
            mapPrivate: mapPrivate, 
            mapPublic: mapPublic, 
            mapInternal: mapInternal,
            emitDirtyMask: emitDirtyMask);
        
        return (name, source);
    }

    private static string GetGeneratedPropertyName(string memberName)
    {
        if (memberName.StartsWith("_", StringComparison.Ordinal))
        {
            if (memberName.Length == 1)
                return "EmptyNameField";
            else
                return char.ToUpper(memberName[1]) + memberName.Substring(2);
        }
        else if (char.IsUpper(memberName[0]))
        {
            return memberName + "DirtyAware";
        }
        else
            return char.ToUpper(memberName[0]) + memberName.Substring(1);
    }

    private static bool HasSerializeMethod(ITypeSymbol type)
        => type.GetMembers("Serialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataWriter");

    private static bool HasDeserializeMethod(ITypeSymbol type)
        => type.GetMembers("Deserialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataReader");

    private static bool IsVectorLike(ITypeSymbol type)
        => type.Name is "Vector2" or "Vector3" or "Vector4" &&
           type.ContainingNamespace.ToDisplayString() == "System.Numerics";

    private string GenerateNetworkedComponent(INamedTypeSymbol symbol, bool mapFields, bool mapProperties, bool mapPrivate, bool mapPublic, bool mapInternal, bool emitDirtyMask)
    {
        var info = GeneratorHelper.GetSymbolInfo(
            symbol,
            mapFields: mapFields,
            mapProperties: mapProperties, 
            mapPrivate: mapPrivate, 
            mapPublic: mapPublic, 
            mapInternal: mapInternal);

        string maskType;
        string maskTypeRead;
        int maskBits;
        var invalidMask = false;

        if (!emitDirtyMask)
        {
            if (info.DirtyMaskType != null)
            {
                maskType = info.DirtyMaskType;
            }
            else
            {
                maskType = "ulong";
                invalidMask = true;
            }
        }
        else
        {
            switch (info.Members.Length)
            {
                case <= sizeof(byte) * 8:
                    maskType = "byte";
                    break;
                case <= sizeof(ushort) * 8:
                    maskType = "ushort";
                    break;
                case <= sizeof(uint) * 8:
                    maskType = "uint";
                    break;
                case <= sizeof(ulong) * 8:
                    maskType = "ulong";
                    break;
                default:
                    maskType = "ulong";
                    invalidMask = true;
                    break;
            }
        }        
        
        switch (maskType)
        {
            case "byte":
                maskBits = sizeof(byte) * 8;
                break;
            case "ushort":
                maskBits = sizeof(ushort) * 8;
                break;
            case "uint":
                maskBits = sizeof(uint) * 8;
                break;
            case "ulong":
                maskBits = sizeof(ulong) * 8;
                break;
            default:
                maskBits = sizeof(ulong) * 8;
                invalidMask = true;
                break;
        }

        if (maskBits < info.Members.Length)
        {
            maskType = "ulong";
            invalidMask = true;
        }
        
        switch (maskType)
        {
            case "byte":
                maskTypeRead = "GetByte";
                break;
            case "ushort":
                maskTypeRead = "GetUShort";
                break;
            case "uint":
                maskTypeRead = "GetUInt";
                break;
            case "ulong":
                maskTypeRead = "GetULong";
                break;
            default:
                maskTypeRead = "GetULong";
                invalidMask = true;
                break;
        }

        var sb = new StringBuilder($@"// <auto-generated/>
#nullable enable

using System;
using System.Numerics;
using LiteNetLib.Utils;
using ReadyM.Api.Generators;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common;

namespace {info.Namespace}
{{
    public partial struct {info.Name} : INetworkedComponent
    {{
");
        if (invalidMask)
        {
            if (emitDirtyMask)
                sb.AppendLine($"        #error Too many networked members in '{info.Name}' to fit in a dirty mask. Maximum supported is {maskBits}, but {info.Members.Length} were found.");
            else
                sb.AppendLine($"        #error Too many networked members in '{info.Name}' to fit in the specified _dirtyMask. Maximum supported is {maskBits}, but {info.Members.Length} were found.");
        }
        
        if (emitDirtyMask)
        {
            sb.AppendLine($"        private {maskType} _dirtyMask;\n");
        }
        
        foreach (var error in info.ErrorMessage)
        {
            sb.AppendLine($"    #error {error}");
        }
        
        var usePutGet = new bool[info.Members.Length];
        var isEnum = new bool[info.Members.Length];
        var enumBaseType = new SpecialType[info.Members.Length];
        var isEquatable = new bool[info.Members.Length];
        var isDeltaEquatable = new bool[info.Members.Length];
        var isCustomSerializable = new bool[info.Members.Length];
        var isVectorLike = new bool[info.Members.Length];
        var isSupported = new bool[info.Members.Length];
        
        for (var i = 0; i < info.Members.Length; i++)
        {
            usePutGet[i] = SerializationHelper.IsSerializablePrimitive(info.Members[i].Type.SpecialType);
            isEnum[i] = info.Members[i].Type.TypeKind == TypeKind.Enum;
            isEquatable[i] = SerializationHelper.IsEquatable(info.Members[i].Type);
            isDeltaEquatable[i] = SerializationHelper.IsDeltaEquatable(info.Members[i].Type);
            isCustomSerializable[i] = HasSerializeMethod(info.Members[i].Type) && HasDeserializeMethod(info.Members[i].Type);
            isVectorLike[i] = IsVectorLike(info.Members[i].Type);
            isSupported[i] = usePutGet[i] || isEnum[i] || isEquatable[i] || isDeltaEquatable[i] || isCustomSerializable[i] || isVectorLike[i];

            if (info.Members[i].IsInvalid)
                isSupported[i] = false;
            
            if (isEnum[i])
            {
                enumBaseType[i] = SerializationHelper.GetEnumBaseType(info.Members[i].Type);
            }

            if (!isSupported[i])
            {
                sb.AppendLine($"        #error Unsupported type '{info.Members[i].Type.ToDisplayString()}' for networked member '{info.Members[i].Name}'.");
                sb.AppendLine();
            }
        }

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];
            var type = field.Type.ToDisplayString();
            var fieldName = field.Name;
            var genPropertyName = GetGeneratedPropertyName(fieldName);

            if (field.ReadOnly)
            {
                sb.AppendLine($"        public readonly {type} {genPropertyName}");
                sb.AppendLine($"            => {field.Name};");
            }
            else
            {
                sb.AppendLine($"        public {type} {genPropertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {field.Name};");

                if (!isSupported[i])
                {
                    sb.AppendLine($"            set {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }}");
                }
                else if (field.Type.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
                {
                    sb.AppendLine($"            set {{ if (Math.Abs({field.Name} - value) > {FloatComparisonEpsilon}) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }
                else if (field.Type.Name == "Vector3")
                {
                    sb.AppendLine($"            set {{ if (Vector3.DistanceSquared({field.Name}, value) > {VectorComparisonEpsilon}) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }
                else if (field.Type.Name == "Vector2")
                {
                    sb.AppendLine($"            set {{ if (Vector2.DistanceSquared({field.Name}, value) > {VectorComparisonEpsilon}) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }
                else if (isDeltaEquatable[i])
                {
                    if (field.Type.IsValueType)
                        sb.AppendLine($"            set {{ if (!{field.Name}.DeltaEquals(value, {VectorComparisonEpsilon})) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                    else
                        sb.AppendLine($"            set {{ if (!({field.Name}?.DeltaEquals(value, {VectorComparisonEpsilon}) ?? value is null)) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }
                else if (isEquatable[i])
                {
                    if (field.Type.IsValueType)
                        sb.AppendLine($"            set {{ if (!{field.Name}.Equals(value)) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                    else
                        sb.AppendLine($"            set {{ if (!({field.Name}?.Equals(value) ?? value is null)) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }
                else
                {
                    sb.AppendLine($"            set {{ if ({field.Name} != value) {{ {field.Name} = value; _dirtyMask |= ({maskType})1 << {i}; }} }}");
                }

                sb.AppendLine("        }\n");
            }
        }

        sb.AppendLine($$"""
                                public void Serialize(NetDataWriter writer)
                                {
                        """);

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];

            if (!isSupported[i])
            {
                continue;
            }
            else if (usePutGet[i])
            {
                sb.AppendLine($"            writer.Put({field.Name});");
            }
            else if (isEnum[i])
            {
                var baseType = SerializationHelper.GetSpecialTypeCSharpName(enumBaseType[i]);
                sb.AppendLine($"            writer.Put(({baseType}){field.Name});");
            }
            else
            {
                sb.AppendLine($"            {field.Name}.Serialize(writer);");
            }
        }

        sb.AppendLine("        }\n");
        
        sb.AppendLine("        public void Deserialize(NetDataReader reader)");
        sb.AppendLine("        {");

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];
            var genPropertyName = GetGeneratedPropertyName(field.Name);

            if (!isSupported[i])
            {
                continue;
            }
            else if (usePutGet[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(field.Type.SpecialType);
                sb.AppendLine($"            {genPropertyName} = reader.{getMethod}();");
            }
            else if (isEnum[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(enumBaseType[i]);
                sb.AppendLine($"            {genPropertyName} = ({field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})reader.{getMethod}();");
            }
            else
            {
                sb.AppendLine($"            {{ {field.Name}.Deserialize(reader); _dirtyMask |= ({maskType})1 << {i}; }}");
            }
        }

        sb.AppendLine("        }\n");

        sb.AppendLine("""
                              public void WriteDelta(NetDataWriter writer)
                              {
                                  var mask = _dirtyMask;
                                  writer.Put(mask);
                      """);

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];

            if (!isSupported[i])
            {
                continue;
            }
            else if (usePutGet[i])
            {
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) writer.Put({field.Name});");
            }
            else if (isEnum[i])
            {
                var baseType = SerializationHelper.GetSpecialTypeCSharpName(enumBaseType[i]);
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) writer.Put(({baseType}){field.Name});");
            }
            else
            {
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) {field.Name}.Serialize(writer);");
            }
        }

        sb.AppendLine("        }\n");

        sb.AppendLine("        public void ReadDelta(NetDataReader reader)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var mask = reader.{maskTypeRead}();");

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];
            var propertyName = GetGeneratedPropertyName(field.Name);

            if (!isSupported[i])
            {
                continue;
            }
            else if (usePutGet[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(field.Type.SpecialType);
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) {propertyName} = reader.{getMethod}();");
            }
            else if (isEnum[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(enumBaseType[i]);
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) {propertyName} = ({field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})reader.{getMethod}();");
            }
            else
            {
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) {{ {field.Name}.Deserialize(reader); _dirtyMask |= ({maskType})1 << {i}; }}");
            }
        }

        sb.AppendLine("        }\n");
        sb.AppendLine("        public void SkipDelta(NetDataReader reader)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var mask = reader.{maskTypeRead}();");

        for (var i = 0; i < info.Members.Length; i++)
        {
            var field = info.Members[i];

            if (!isSupported[i])
            {
                continue;
            }
            else if (usePutGet[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(field.Type.SpecialType);
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) reader.{getMethod}();");
            }
            else if (isEnum[i])
            {
                var getMethod = SerializationHelper.GetDeserializationMethod(enumBaseType[i]);
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) reader.{getMethod}();");
            }
            else
            {
                sb.AppendLine($"            if ((mask & (({maskType})1 << {i})) != 0) {{ var dummy = default({field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}); dummy.Deserialize(reader); }}");
            }
        }

        sb.AppendLine("        }\n");
        sb.AppendLine("        public void ClearDirty() => _dirtyMask = 0;");
        sb.AppendLine("        public bool IsDirty => _dirtyMask != 0;");
        sb.AppendLine("    }");
        
        sb.AppendLine(@"
}

#nullable disable
                      ");

        return sb.ToString();
    }
}