using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.Email.Models;

namespace Paradigm.Enterprise.Services.Email;
public interface IEmailService : IService
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