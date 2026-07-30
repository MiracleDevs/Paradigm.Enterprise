using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.Email.Models;

namespace Paradigm.Enterprise.Services.Email;

/// <summary>
/// Sends HTML email through the configured Azure Communication Services account.
/// </summary>
/// <remarks>
/// Sending is synchronous and requests Azure to start delivery. The default implementation logs
/// and suppresses invalid configuration and delivery-start failures, so callers do not receive a
/// delivery result or exception.
/// </remarks>
/// <example>
/// <code>
/// emailService.SendMail(
///     new MailMessageInfo(
///         "customer@example.com",
///         "Order received",
///         "&lt;p&gt;Thanks for your order.&lt;/p&gt;"));
/// </code>
/// </example>
public interface IEmailService : IService
{
    /// <summary>
    /// Requests delivery for each message.
    /// </summary>
    /// <param name="messages">The messages to submit in enumeration order.</param>
    void SendMails(IEnumerable<MailMessageInfo> messages);

    /// <summary>
    /// Requests delivery for one message.
    /// </summary>
    /// <param name="message">The message to submit.</param>
    void SendMail(MailMessageInfo message);
}
