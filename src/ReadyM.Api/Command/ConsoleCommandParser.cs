using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace ReadyM.Api.Command;

public class ConsoleCommandParser
{
    private static readonly TextParser<TextSpan> FloatRegex = Span.Regex(
        @"[+-]?(?:(?:\d*\.\d+|\d+\.\d*)(?:[eE][+-]?\d+)?|\d+(?:[eE][+-]?\d+))" );
    
    private static readonly Tokenizer<CommandToken> Tokenizer =
        new TokenizerBuilder<CommandToken>()
            .Ignore(Span.WhiteSpace)

            .Match(Character.EqualTo('/'), CommandToken.Slash)
            .Match(Character.EqualTo('('), CommandToken.LeftParen)
            .Match(Character.EqualTo(')'), CommandToken.RightParen)
            .Match(Character.EqualTo('['), CommandToken.LeftBracket)
            .Match(Character.EqualTo(']'), CommandToken.RightBracket)
            .Match(Character.EqualTo('{'), CommandToken.LeftBrace)
            .Match(Character.EqualTo('}'), CommandToken.RightBrace)
            .Match(Character.EqualTo(','), CommandToken.Comma)

            // C-style quoted strings; tokenizer validates shape, we unescape in parser
            .Match(QuotedString.CStyle, CommandToken.String)

            // numbers incl exponent
            .Match(FloatRegex, CommandToken.Float)
            .Match(Numerics.Integer, CommandToken.Integer)
            
            // boolean literals
            .Match(Span.EqualToIgnoreCase("true"), CommandToken.True)
            .Match(Span.EqualToIgnoreCase("false"), CommandToken.False)

            // identifiers (command name, bare words, etc.)
            .Match(Identifier.CStyle, CommandToken.Identifier)
            .Build();
    
    private static TokenListParser<CommandToken, string> CommandName { get; } =
        from slash in Token.EqualTo(CommandToken.Slash)
        from name in Token.EqualTo(CommandToken.Identifier).Select(t => t.ToStringValue())
        select name;

    private static TokenListParser<CommandToken, long> Integer { get; } =
        Token.EqualTo(CommandToken.Integer)
            .Select(t => long.Parse(t.ToStringValue(), NumberStyles.Float, CultureInfo.InvariantCulture));

    private static TokenListParser<CommandToken, double> Float { get; } =
        Token.EqualTo(CommandToken.Float)
            .Select(t => double.Parse(t.ToStringValue(), NumberStyles.Float, CultureInfo.InvariantCulture));

    private static TokenListParser<CommandToken, bool> Bool { get; } =
        Token.EqualTo(CommandToken.True).Value(true)
            .Or(Token.EqualTo(CommandToken.False).Value(false));

    private static TokenListParser<CommandToken, string> String { get; } =
        Token.EqualTo(CommandToken.String).Select(t =>
            UnescapeCStyleStringToken(t.ToStringValue()));

    private static TokenListParser<CommandToken, Ident> Ident { get; } =
        Token.EqualTo(CommandToken.Identifier).Select(t => 
            new Ident(t.ToStringValue()));

    public ConsoleCommandParser()
    {
        AddArgParser(Integer);
        AddArgParser(Float);
        AddArgParser(String);
        AddArgParser(Bool);
        AddArgParser(Ident);
    }

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
    
    private readonly List<TokenListParser<CommandToken, object?>> _argParsers = new();
    private bool _argParsersDirty;
    private TokenListParser<CommandToken, ParsedCommandCall>? _cachedParser;
    
    public void AddArgParser(TokenListParser<CommandToken, object?> parser)
    {
        if (parser == null) throw new ArgumentNullException(nameof(parser));
        _argParsers.Add(parser);
        _argParsersDirty = true;
    }
    
    public void AddArgParser<T>(TokenListParser<CommandToken, T> parser)
    {
        if (parser == null) throw new ArgumentNullException(nameof(parser));
        _argParsers.Add(parser.Select(x => (object?)x));
        _argParsersDirty = true;
    }
    
    private TokenListParser<CommandToken, ParsedCommandCall> Build()
    {
        if (!_argParsersDirty && _cachedParser != null)
            return _cachedParser;
        
        if (_argParsers.Count == 0)
            throw new InvalidOperationException("No argument parsers registered.");

        // Compose Arg = p0 OR p1 OR p2 ...
        var anyArg = _argParsers[0];
        for (var i = 1; i < _argParsers.Count; i++)
        {
            anyArg = anyArg.Or(_argParsers[i]);
        }

        // /cmd <arg>*
        _cachedParser = from cmdName in CommandName
            from args in anyArg.Many()
            select new ParsedCommandCall(cmdName, args);
        _argParsersDirty = false;
        return _cachedParser;
    }
    
    public bool TryParse(string input, [NotNullWhen(true)] out ParsedCommandCall? parsedCommandCall, [NotNullWhen(false)] out CommandError? error)
    {
        TokenList<CommandToken> tokenList;

        try
        {
            tokenList = Tokenizer.Tokenize(input);
        }
        catch (ParseException ex)
        {
            parsedCommandCall = null;
            error = new CommandError.InvalidCommandFormat(input, ex);
            return false;
        }
        
        var commandName = CommandName.TryParse(tokenList);
        if (!commandName.HasValue)
        {
            parsedCommandCall = null;
            error = new CommandError.InvalidCommandFormat(input, null);
            return false;
        }
        
        var argList = new List<object>();
        tokenList = commandName.Remainder;

        while (!tokenList.IsAtEnd)
        {
            object? arg = null;

            TokenListParserResult<CommandToken, object?> r = default;
            
            foreach (var p in _argParsers)
            {
                r = p.TryParse(tokenList);
                if (r.HasValue)
                {
                    arg = r.Value;
                    break;
                }
            }

            if (arg == null)
            {
                parsedCommandCall = null;
                error = new CommandError.InvalidArgumentFormat(input, argList.Count, tokenList.First().Position);
                return false;
            }
            
            argList.Add(arg);
            tokenList = r.Remainder;
        }
        
        parsedCommandCall = new ParsedCommandCall(commandName.Value, argList);
        error = null;
        return true;
    }
}