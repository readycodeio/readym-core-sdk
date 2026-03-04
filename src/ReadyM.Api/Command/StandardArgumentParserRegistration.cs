namespace ReadyM.Api.Command;

public class StandardArgumentParserRegistration : IConsoleArgumentParserRegistration
{
    public void Register(ConsoleCommandParser parser)
    {
        parser.AddArgParser(nameof(StandardArgument.Integer), StandardArgument.Integer);
        parser.AddArgParser(nameof(StandardArgument.Float), StandardArgument.Float);
        parser.AddArgParser(nameof(StandardArgument.String), StandardArgument.String);
        parser.AddArgParser(nameof(StandardArgument.Bool), StandardArgument.Bool);
        parser.AddArgParser(nameof(StandardArgument.Ident), StandardArgument.Ident);
    }
}