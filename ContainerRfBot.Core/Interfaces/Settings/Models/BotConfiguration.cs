namespace ContainerRfBot.Core.Interfaces.Settings.Models;

public class BotConfiguration
{
    public required string MaxToken { get; init; }
    public required string AiUri { get; init; }
    public required string AiToken { get; init; }
}