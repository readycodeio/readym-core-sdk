using System;
using System.Globalization;
using Superpower;
using Superpower.Parsers;

namespace ReadyM.Api.Command;

public static class StandardArgument
{
    public static TokenListParser<CommandToken, long> Integer { get; } =
        Token.EqualTo(CommandToken.Integer)
            .Select(t => long.Parse(t.ToStringValue(), NumberStyles.Float, CultureInfo.InvariantCulture));

    public static TokenListParser<CommandToken, double> Float { get; } =
        Token.EqualTo(CommandToken.Float)
            .Select(t => double.Parse(t.ToStringValue(), NumberStyles.Float, CultureInfo.InvariantCulture));

    public static TokenListParser<CommandToken, bool> Bool { get; } =
        Token.EqualTo(CommandToken.True).Value(true)
            .Or(Token.EqualTo(CommandToken.False).Value(false));

    public static TokenListParser<CommandToken, string> String { get; } =
        Token.EqualTo(CommandToken.String).Select(t =>
            UnescapeCStyleStringToken(t.ToStringValue()));

    public static TokenListParser<CommandToken, Ident> Ident { get; } =
        Token.EqualTo(CommandToken.Identifier).Select(t => 
            new Ident(t.ToStringValue()));
    
    private static string UnescapeCStyleStringToken(string tokenText)
    {
        // tokenText includes surrounding quotes, e.g. "\"ab\\n\""
        if (tokenText.Length < 2 || tokenText[0] != '"' || tokenText[tokenText.Length - 1] != '"')
            throw new FormatException("Invalid string token.");

        // Minimal C-style unescape. Extend as needed (e.g. \uXXXX).
        var inner = tokenText.Substring(1, tokenText.Length - 2);
        var result = new System.Text.StringBuilder(inner.Length);

        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];
            if (c != '\\')
            {
                result.Append(c);
                continue;
            }

            if (i == inner.Length - 1)
                throw new FormatException("Invalid escape at end of string.");

            var e = inner[++i];
            result.Append(e switch
            {
                '"' => '"',
                '\\' => '\\',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => e // keep unknown escapes as literal (or throw if you prefer)
            });
        }

        return result.ToString();
    }
}