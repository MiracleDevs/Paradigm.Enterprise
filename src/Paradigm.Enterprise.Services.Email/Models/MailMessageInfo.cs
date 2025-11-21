namespace Paradigm.Enterprise.Services.Email.Models;

public class MailMessageInfo
{
    #region Properties

    /// <summary>
    /// Gets the recipients.
    /// </summary>
    /// <value>
    /// The recipients.
    /// </value>
    public IEnumerable<string> Recipients { get; private set; }
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public string Subject { get; private set; }
    /// <summary>
    /// Gets the body HTML.
    /// </summary>
    /// <value>
    /// The body HTML.
    /// </value>
    public string BodyHtml { get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MailMessageInfo"/> class.
    /// </summary>
    /// <param name="recipient">The recipient.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="bodyHtml">The body HTML.</param>
    public MailMessageInfo(string recipient, string subject, string bodyHtml) : this(new[] { recipient }, subject, bodyHtml)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailMessageInfo"/> class.
    /// </summary>
    /// <param name="recipients">The recipients.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="bodyHtml">The body HTML.</param>
    public MailMessageInfo(IEnumerable<string> recipients, string subject, string bodyHtml)
    {
        Recipients = recipients;
        Subject = subject;
        BodyHtml = bodyHtml;
    }

    #endregion
}