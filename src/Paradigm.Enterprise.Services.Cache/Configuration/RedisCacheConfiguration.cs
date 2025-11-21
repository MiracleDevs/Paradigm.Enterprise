namespace Paradigm.Enterprise.Services.Cache.Configuration;

internal class RedisCacheConfiguration
{
    public int? ExpirationInMinutes { get; set; }
    public bool Disabled { get; set; }
}