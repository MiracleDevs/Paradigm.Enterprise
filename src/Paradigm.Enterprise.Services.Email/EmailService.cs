using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Services.Email.Configuration;
using Paradigm.Enterprise.Services.Email.Models;

namespace Paradigm.Enterprise.Services.Email;

/// <summary>
/// Sends email through Azure Communication Services using a connection string or managed identity.
/// </summary>
/// <remarks>Invalid configuration and send failures are logged and suppressed.</remarks>
public class EmailService : IEmailService
{
    #region Properties

    /// <summary>
    /// The email configuration
    /// </summary>
    private readonly EmailConfiguration _emailConfiguration;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _emailConfiguration = new();
        configuration.Bind("EmailConfiguration", _emailConfiguration);
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sends the e-mail.
    /// </summary>
    /// <param name="messageInfo">The recipients, subject, and HTML body to send.</param>
    public void SendMail(MailMessageInfo messageInfo)
    {
        SendMails([messageInfo]);
    }

    /// <summary>
    /// Sends the mails.
    /// </summary>
    /// <param name="messages">The email information.</param>
    public void SendMails(IEnumerable<MailMessageInfo> messages)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_emailConfiguration.MailFrom))
            {
                _logger.LogInformation("Email configuration is not valid.");
                return;
            }

            var client = BuildEmailClient();

            if (client is null)
            {
                _logger.LogInformation("Email configuration is not valid.");
                return;
            }

            foreach (var messageInfo in messages)
            {
                var recipients = new EmailRecipients(messageInfo.Recipients.Select(x => new EmailAddress(x)));
                var content = new EmailContent(messageInfo.Subject) { Html = messageInfo.BodyHtml };
                client.Send(Azure.WaitUntil.Started, new EmailMessage(_emailConfiguration.MailFrom, recipients, content));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email.");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds the email client.
    /// </summary>
    /// <returns>A configured email client, or <see langword="null"/> when configuration is invalid.</returns>
    private EmailClient? BuildEmailClient()
    {
        var strategy = GetClientCreationStrategy();

        if (strategy == EmailClientCreationStrategy.ConnectionString)
            return new EmailClient(_emailConfiguration.ConnectionString!);

        if (strategy != EmailClientCreationStrategy.ManagedIdentity)
            return null;

        Uri.TryCreate(_emailConfiguration.ManagedIdentity!.Endpoint, UriKind.Absolute, out var endpointUri);
        return new EmailClient(endpointUri!, new DefaultAzureCredential(BuildManagedIdentityCredentialOptions()));
    }

    internal EmailClientCreationStrategy GetClientCreationStrategy()
    {
        if (!string.IsNullOrWhiteSpace(_emailConfiguration.ConnectionString))
            return EmailClientCreationStrategy.ConnectionString;

        if (_emailConfiguration.ManagedIdentity is null || string.IsNullOrWhiteSpace(_emailConfiguration.ManagedIdentity.Endpoint))
            return EmailClientCreationStrategy.Invalid;

        if (!Uri.TryCreate(_emailConfiguration.ManagedIdentity.Endpoint, UriKind.Absolute, out _))
            return EmailClientCreationStrategy.Invalid;

        return EmailClientCreationStrategy.ManagedIdentity;
    }

    internal DefaultAzureCredentialOptions BuildManagedIdentityCredentialOptions()
    {
        var credentialOptions = new DefaultAzureCredentialOptions();

        if (!string.IsNullOrWhiteSpace(_emailConfiguration.ManagedIdentity?.ClientId))
            credentialOptions.ManagedIdentityClientId = _emailConfiguration.ManagedIdentity.ClientId;

        return credentialOptions;
    }

    #endregion
}
