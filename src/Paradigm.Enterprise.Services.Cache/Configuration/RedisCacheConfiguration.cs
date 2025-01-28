namespace Paradigm.Enterprise.Providers.Cache.Configuration;
internal class RedisCacheConfiguration
{
    public int? ExpirationInMinutes { get; set; }
    public bool Disabled { get; set; }
}