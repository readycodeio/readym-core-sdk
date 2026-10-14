using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal enum DeriveQuantizationKind
{
    Range,
    Angle,
    Half,
}

/// How a field marked [Quantize], [QuantizeAngle] or [HalfPrecision] goes over the wire.
internal sealed class DeriveQuantization(DeriveQuantizationKind kind, float min, float max, int bits)
{
    private const string Runtime = "global::ReadyM.Api.Multiplayer.Serialization.Quantization";

    public DeriveQuantizationKind Kind { get; } = kind;
    public float Min { get; } = min;
    public float Max { get; } = max;
    public int Bits { get; } = bits;

    public static DeriveQuantization? Read(ISymbol member, out int attributeCount)
    {
        attributeCount = 0;
        DeriveQuantization? result = null;

        if (AttributeUtils.HasAttribute(member, "QuantizeAttribute"))
        {
            attributeCount++;
            result = new DeriveQuantization(
                DeriveQuantizationKind.Range,
                AttributeUtils.GetAttribute(member, "QuantizeAttribute", "min", 0f),
                AttributeUtils.GetAttribute(member, "QuantizeAttribute", "max", 0f),
                AttributeUtils.GetAttribute(member, "QuantizeAttribute", "bits", 0));
        }

        if (AttributeUtils.HasAttribute(member, "QuantizeAngleAttribute"))
        {
            attributeCount++;
            result = new DeriveQuantization(
                DeriveQuantizationKind.Angle, 0f, 0f,
                AttributeUtils.GetAttribute(member, "QuantizeAngleAttribute", "bits", 16));
        }

        if (AttributeUtils.HasAttribute(member, "HalfPrecisionAttribute"))
        {
            attributeCount++;
            result = new DeriveQuantization(DeriveQuantizationKind.Half, 0f, 0f, 16);
        }

        return result;
    }

    /// The reasons this quantization cannot apply to a field of <paramref name="type"/>, empty when it can.
    public IEnumerable<string> Problems(ITypeSymbol type)
    {
        var isFloat = type.SpecialType == SpecialType.System_Single;
        var isVector = SerializationHelper.IsVectorLike(type);

        switch (Kind)
        {
            case DeriveQuantizationKind.Range:
                if (!isFloat && !isVector)
                    yield return "[Quantize] only applies to float, Vector2, Vector3 and Vector4";
                if (Bits is not (8 or 16 or 24))
                    yield return "[Quantize] takes 8, 16 or 24 bits";
                if (!(Min < Max))
                    yield return "[Quantize] needs min below max";
                break;

            case DeriveQuantizationKind.Angle:
                if (!isFloat)
                    yield return "[QuantizeAngle] only applies to float";
                if (Bits is not (8 or 16))
                    yield return "[QuantizeAngle] takes 8 or 16 bits";
                break;

            case DeriveQuantizationKind.Half:
                if (!isFloat && !isVector)
                    yield return "[HalfPrecision] only applies to float, Vector2, Vector3 and Vector4";
                break;
        }
    }

    /// A C# expression that is true when writing <paramref name="b"/> over <paramref name="a"/> is no change: it stays
    /// within the plain fields' cutoff or encodes the same.
    public string UnchangedExpression(string a, string b)
        => Kind switch
        {
            DeriveQuantizationKind.Range => $"{Runtime}.Unchanged({a}, {b}, {Float(Min)}, {Float(Max)}, {Bits})",
            DeriveQuantizationKind.Angle => $"{Runtime}.UnchangedAngle({a}, {b}, {Bits})",
            _ => $"{Runtime}.UnchangedHalf({a}, {b})",
        };

    /// A C# statement writing <paramref name="value"/> to <c>writer</c>.
    public string WriteStatement(string value)
        => Kind switch
        {
            DeriveQuantizationKind.Range => $"{Runtime}.Write(writer, {value}, {Float(Min)}, {Float(Max)}, {Bits});",
            DeriveQuantizationKind.Angle => $"{Runtime}.WriteAngle(writer, {value}, {Bits});",
            _ => $"{Runtime}.WriteHalf(writer, {value});",
        };

    /// A C# expression reading a value of <paramref name="type"/> from <c>reader</c>.
    public string ReadExpression(ITypeSymbol type)
    {
        var suffix = type.SpecialType == SpecialType.System_Single ? null : type.Name;

        return Kind switch
        {
            DeriveQuantizationKind.Range => $"{Runtime}.Read{suffix ?? "Float"}(reader, {Float(Min)}, {Float(Max)}, {Bits})",
            DeriveQuantizationKind.Angle => $"{Runtime}.ReadAngle(reader, {Bits})",
            _ => $"{Runtime}.ReadHalf{suffix}(reader)",
        };
    }

    /// The C++ counterpart of <see cref="UnchangedExpression"/>.
    public string CppUnchangedExpression(string a, string b)
        => Kind switch
        {
            DeriveQuantizationKind.Range => $"Interop::Quantization::Unchanged({a}, {b}, {Float(Min)}, {Float(Max)}, {Bits})",
            DeriveQuantizationKind.Angle => $"Interop::Quantization::UnchangedAngle({a}, {b}, {Bits})",
            _ => $"Interop::Quantization::UnchangedHalf({a}, {b})",
        };

    // A literal both C# and C++ accept: C++ needs a decimal point or an exponent before the f suffix
    private static string Float(float value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (text.IndexOfAny(['.', 'E', 'e']) < 0)
            text += ".0";

        return text + "f";
    }
}
