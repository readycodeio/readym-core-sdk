namespace ReadyM.Api.Command;

public class StandardArgumentParserRegistration : IConsoleArgumentParserRegistration
{
    public void Register(ConsoleCommandParser parser)
    {
        parser.AddArgParser(nameof(StandardArgument.Decimal), StandardArgument.Decimal);
        parser.AddArgParser(nameof(StandardArgument.String), StandardArgument.String);
        parser.AddArgParser(nameof(StandardArgument.Bool), StandardArgument.Bool);
        parser.AddArgParser(nameof(StandardArgument.Ident), StandardArgument.Ident);
    }
}