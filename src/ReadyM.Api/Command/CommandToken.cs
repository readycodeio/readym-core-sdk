namespace ReadyM.Api.Command;

internal enum CommandToken
{
    None,
    Slash,
    True,
    False,
    Identifier,
    Decimal,
    String,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    LeftAngle,
    RightAngle,
    Comma
}