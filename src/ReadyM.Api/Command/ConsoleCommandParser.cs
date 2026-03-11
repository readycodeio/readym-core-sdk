using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace ReadyM.Api.Command;

public class ConsoleCommandParser
{
    private static readonly Tokenizer<CommandToken> Tokenizer =
        new TokenizerBuilder<CommandToken>()
            .Ignore(Span.WhiteSpace)

            .Match(Character.EqualTo('('), CommandToken.LeftParen)
            .Match(Character.EqualTo(')'), CommandToken.RightParen)
            .Match(Character.EqualTo('['), CommandToken.LeftBracket)
            .Match(Character.EqualTo(']'), CommandToken.RightBracket)
            .Match(Character.EqualTo('{'), CommandToken.LeftBrace)
            .Match(Character.EqualTo('}'), CommandToken.RightBrace)
            .Match(Character.EqualTo('<'), CommandToken.LeftAngle)
            .Match(Character.EqualTo('>'), CommandToken.RightAngle)
            .Match(Character.EqualTo(','), CommandToken.Comma)

            // C-style quoted strings; tokenizer validates shape, we unescape in parser
            .Match(QuotedString.CStyle, CommandToken.String)

            // numbers incl exponent
            .Match(Numerics.Decimal, CommandToken.Decimal)
            
            // boolean literals
            .Match(Span.EqualToIgnoreCase("true"), CommandToken.True)
            .Match(Span.EqualToIgnoreCase("false"), CommandToken.False)

            // identifiers (command name, bare words, etc.)
            .Match(Identifier.CStyle, CommandToken.Identifier)
            .Build();
    
    private static TokenListParser<CommandToken, string> CommandName { get; } =
        from name in Token.EqualTo(CommandToken.Identifier).Select(t => t.ToStringValue())
        select name;
    
    private readonly List<(string ParserName, TokenListParser<CommandToken, object?> Parser)> _argParsers = new();
    private bool _argParsersDirty;
    private TokenListParser<CommandToken, ParsedCommandCall>? _cachedParser;

    public ConsoleCommandParser(IReadOnlyList<IConsoleArgumentParserRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.Register(this);
        }
    }

    public void AddArgParser(string parserName, TokenListParser<CommandToken, object?> parser)
    {
        if (parser == null)
            throw new ArgumentNullException(nameof(parser));
        _argParsers.Add((parserName, parser));
        _argParsersDirty = true;
    }
    
    public void AddArgParser<T>(string parserName, TokenListParser<CommandToken, T> parser)
    {
        if (parser == null)
            throw new ArgumentNullException(nameof(parser));
        _argParsers.Add((parserName, parser.Select(x => (object?)x)));
        _argParsersDirty = true;
    }
    
    private TokenListParser<CommandToken, ParsedCommandCall> Build()
    {
        if (!_argParsersDirty && _cachedParser != null)
            return _cachedParser;
        
        if (_argParsers.Count == 0)
            throw new InvalidOperationException("No argument parsers registered.");

        // Compose Arg = p0 OR p1 OR p2 ...
        var anyArg = _argParsers[0].Parser;
        for (var i = 1; i < _argParsers.Count; i++)
        {
            anyArg = anyArg.Or(_argParsers[i].Parser);
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
                r = p.Parser.TryParse(tokenList);
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