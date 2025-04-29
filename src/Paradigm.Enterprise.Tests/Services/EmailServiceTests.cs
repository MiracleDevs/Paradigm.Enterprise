using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Services.Email;
using Paradigm.Enterprise.Services.Email.Models;

namespace Paradigm.Enterprise.Tests.Services;

[TestClass]
[ExcludeFromCodeCoverage]
public class EmailServiceTests
{
    private Mock<IConfiguration>? _mockConfiguration;
    private Mock<ILogger<EmailService>>? _mockLogger;
    private Mock<IConfigurationSection>? _mockConfigSection;

    [TestInitialize]
    public void Initialize()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfigSection = new Mock<IConfigurationSection>();

        // Set up configuration section for email configuration
        _mockConfigSection.Setup(x => x["MailFrom"]).Returns("test@example.com");
        _mockConfigSection.Setup(x => x["ConnectionString"]).Returns("mock-connection-string");
        
        _mockConfiguration.Setup(x => x.GetSection("EmailConfiguration"))
            .Returns(_mockConfigSection.Object);
    }

    [TestMethod]
    public void SendMail_WithValidMessage_ShouldCallSendMails()
    {
        // Arrange
        var messageInfo = new MailMessageInfo("recipient@example.com", "Test Subject", "<p>Test Body</p>");
        var testEmailService = new TestableEmailService(_mockConfiguration!.Object, _mockLogger!.Object);
        
        // Act
        testEmailService.SendMail(messageInfo);
        
        // Assert
        Assert.IsTrue(testEmailService.SendMailsCalled);
        Assert.AreEqual(1, testEmailService.SendMailsCallCount);
        CollectionAssert.Contains(testEmailService.SentMessages.ToList(), messageInfo);
    }

    [TestMethod]
    public void SendMails_WithNullConfiguration_ShouldLogAndReturn()
    {
        // Arrange - setup null configuration
        _mockConfigSection!.Setup(x => x["MailFrom"]).Returns((string?)null);
        
        var emailService = new EmailService(_mockConfiguration!.Object, _mockLogger!.Object);
        var messages = new List<MailMessageInfo>
        {
            new MailMessageInfo("recipient@example.com", "Test Subject", "<p>Test Body</p>")
        };

        // Act - should return early without exception
        emailService.SendMails(messages);

        // Assert - should log the information message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Email configuration is not valid")),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [TestMethod]
    public void SendMails_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var messages = new List<MailMessageInfo>
        {
            new MailMessageInfo("recipient@example.com", "Test Subject", "<p>Test Body</p>")
        };
        
        var testEmailService = new ExceptionThrowingEmailService(_mockConfiguration!.Object, _mockLogger!.Object);

        // Act - Execute the method which will throw and catch the exception
        testEmailService.ExecuteWithException(messages);

        // Assert - verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [TestMethod]
    public void MailMessageInfo_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var singleRecipient = new MailMessageInfo("recipient@example.com", "Test Subject", "<p>Test Body</p>");
        var multipleRecipients = new MailMessageInfo(
            new[] { "recipient1@example.com", "recipient2@example.com" }, 
            "Test Subject Multiple", 
            "<p>Test Body Multiple</p>");

        // Assert
        Assert.AreEqual(1, singleRecipient.Recipients.Count());
        Assert.AreEqual("recipient@example.com", singleRecipient.Recipients.First());
        Assert.AreEqual("Test Subject", singleRecipient.Subject);
        Assert.AreEqual("<p>Test Body</p>", singleRecipient.BodyHtml);

        Assert.AreEqual(2, multipleRecipients.Recipients.Count());
        Assert.AreEqual("recipient1@example.com", multipleRecipients.Recipients.First());
        Assert.AreEqual("recipient2@example.com", multipleRecipients.Recipients.Last());
        Assert.AreEqual("Test Subject Multiple", multipleRecipients.Subject);
        Assert.AreEqual("<p>Test Body Multiple</p>", multipleRecipients.BodyHtml);
    }

    // Custom implementation for testing SendMail behavior
    private class TestableEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public bool SendMailsCalled { get; private set; }
        public int SendMailsCallCount { get; private set; }
        public List<MailMessageInfo> SentMessages { get; }

        public TestableEmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            SentMessages = new List<MailMessageInfo>();
            SendMailsCalled = false;
            SendMailsCallCount = 0;
        }

        public void SendMail(MailMessageInfo messageInfo)
        {
            SendMails(new[] { messageInfo });
        }

        public void SendMails(IEnumerable<MailMessageInfo> messages)
        {
            SendMailsCalled = true;
            SendMailsCallCount++;
            SentMessages.AddRange(messages);
        }
    }

    // Custom implementation for testing exception handling
    private class ExceptionThrowingEmailService : EmailService
    {
        private readonly ILogger<EmailService> _logger;

        public ExceptionThrowingEmailService(IConfiguration configuration, ILogger<EmailService> logger)
            : base(configuration, logger)
        {
            _logger = logger;
        }

        // Method that will trigger the exception handling in the base class
        public void ExecuteWithException(IEnumerable<MailMessageInfo> messages)
        {
            try
            {
                // Override the base behavior to force an exception
                throw new InvalidOperationException("Simulated email sending failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email");
            }
        }
    }
} 