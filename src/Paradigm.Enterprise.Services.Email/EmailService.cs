using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Providers.Email.Configuration;
using Paradigm.Enterprise.Providers.Email.Models;

namespace Paradigm.Enterprise.Providers.Email;
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
    /// Registers the specified services.
    /// </summary>
    /// <param name="services">The services.</param>
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton(typeof(EmailService));
        services.AddSingleton(typeof(IEmailService), typeof(EmailService));
    }

    /// <summary>
    /// Sends the e-mail.
    /// </summary>
    /// <param name="messageInfo"></param>
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
            if (_emailConfiguration is null ||
                string.IsNullOrWhiteSpace(_emailConfiguration.MailFrom) || string.IsNullOrWhiteSpace(_emailConfiguration.ConnectionString))
            {
                _logger.LogInformation("Email configuration is not valid.");
                return;
            }

            var client = new EmailClient(_emailConfiguration.ConnectionString);

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
}