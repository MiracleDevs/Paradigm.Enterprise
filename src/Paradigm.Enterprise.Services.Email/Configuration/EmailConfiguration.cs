namespace Paradigm.Enterprise.Services.Email.Configuration;

internal class EmailConfiguration
{
    /// <summary>
    /// Gets or sets the mail from.
    /// </summary>
    /// <value>
    /// The mail from.
    /// </value>
    public string? MailFrom { get; set; }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    /// <value>
    /// The connection string.
    /// </value>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the managed identity configuration.
    /// </summary>
    public EmailManagedIdentityConfiguration? ManagedIdentity { get; set; }
}

internal class EmailManagedIdentityConfiguration
{
    /// <summary>
    /// Gets or sets the ACS email endpoint.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the user-assigned managed identity client id.
    /// </summary>
    public string? ClientId { get; set; }
}