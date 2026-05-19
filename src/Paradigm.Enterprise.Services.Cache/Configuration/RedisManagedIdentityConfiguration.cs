namespace Paradigm.Enterprise.Services.Cache.Configuration;

internal class RedisManagedIdentityConfiguration
{
    public required string Host { get; set; }
    public int Port { get; set; } = 6380;
    public bool UseSsl { get; set; } = true;
    public string Scope { get; set; } = "https://redis.azure.com/.default";
    public string? User { get; set; }
    public string? ClientId { get; set; }
}
