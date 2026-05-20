namespace Paradigm.Enterprise.Services.Email.Models;

internal enum EmailClientCreationStrategy
{
    Invalid = 0,
    ConnectionString = 1,
    ManagedIdentity = 2,
}
