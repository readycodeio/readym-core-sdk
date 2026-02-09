namespace ReadyM.Api.Command;

public enum CommandToken
{
    None,
    Slash,
    True,
    False,
    Identifier,
    Float,
    Integer,
    String,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    Comma
}