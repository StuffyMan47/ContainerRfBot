namespace ContainerRfBot.Core.Interfaces.Settings.Models;

public class BotConfiguration
{
    public required string MaxToken { get; init; }
    public required string AiUri { get; init; }
    public required string AiToken { get; init; }
    public string? SiteUrl { get; init; }
    public string? SiteToken { get; init; }
    public GoogleAuth? GoogleAuth { get; init; }
}

public class GoogleAuth
{
    public required string Key { get; init; }
}