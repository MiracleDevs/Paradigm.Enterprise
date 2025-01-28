using Paradigm.Enterprise.Providers.Email.Models;

namespace Paradigm.Enterprise.Providers.Email;
public interface IEmailService
{
    /// <summary>
    /// Sends the e-mails.
    /// </summary>
    /// <param name="messages">The messages.</param>
    void SendMails(IEnumerable<MailMessageInfo> messages);

    /// <summary>
    /// Sends the e-mail.
    /// </summary>
    /// <param name="message">The message.</param>
    void SendMail(MailMessageInfo message);
}