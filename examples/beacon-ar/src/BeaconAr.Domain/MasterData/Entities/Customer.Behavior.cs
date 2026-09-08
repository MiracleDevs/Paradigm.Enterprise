using System.Net.Mail;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Interfaces.MasterData.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.MasterData.Entities;

public partial class Customer
{
    #region Nested Types

    private sealed record ProposedState(string AccountNumber, string Name, string Email, string? Phone,
        decimal CreditLimit, short PaymentTermsDays, bool IsActive);

    #endregion

    #region Constants

    private const decimal MaximumCreditLimit = 99999999999999999.99m;

    #endregion

    #region Properties

    public ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();

    #endregion

    #region Public Methods

    public static Customer Create(CustomerCreateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.AccountNumber, request.Name, request.Email, request.Phone,
            request.CreditLimit, request.PaymentTermsDays, request.IsActive);
        ValidateState(state);
        return new Customer
        {
            AccountNumber = state.AccountNumber,
            Name = state.Name,
            Email = state.Email,
            Phone = state.Phone,
            CreditLimit = state.CreditLimit,
            PaymentTermsDays = state.PaymentTermsDays,
            IsActive = state.IsActive,
            CreatedByUserId = userId,
            CreationDate = now,
        };
    }

    public void Replace(CustomerUpdateRequest request, int userId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        ProposedState state = Normalize(request.AccountNumber, request.Name, request.Email, request.Phone,
            request.CreditLimit, request.PaymentTermsDays, request.IsActive);
        ValidateState(state);
        AccountNumber = state.AccountNumber;
        Name = state.Name;
        Email = state.Email;
        Phone = state.Phone;
        CreditLimit = state.CreditLimit;
        PaymentTermsDays = state.PaymentTermsDays;
        IsActive = state.IsActive;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Activate(int userId, DateTimeOffset now)
    {
        IsActive = true;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    public void Deactivate(int userId, DateTimeOffset now)
    {
        IsActive = false;
        ModifiedByUserId = userId;
        ModificationDate = now;
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(AccountNumber, Name, Email, Phone, CreditLimit, PaymentTermsDays, IsActive));

    partial void BeforeMap(ICustomer model)
    {
        _ = Id;
        ValidateState(Normalize(model.AccountNumber, model.Name, model.Email, model.Phone, model.CreditLimit,
            model.PaymentTermsDays, model.IsActive));
    }

    partial void AfterMap(ICustomer model)
    {
        ProposedState state = Normalize(AccountNumber, Name, Email, Phone, CreditLimit, PaymentTermsDays, IsActive);
        AccountNumber = state.AccountNumber;
        Name = state.Name;
        Email = state.Email;
        Phone = state.Phone;
    }

    private static ProposedState Normalize(string? accountNumber, string? name, string? email, string? phone,
        decimal creditLimit, short paymentTermsDays, bool isActive) => new(
        accountNumber?.Trim() ?? string.Empty,
        name?.Trim() ?? string.Empty,
        email?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        creditLimit,
        paymentTermsDays,
        isActive);

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.AccountNumber.Length is >= 1 and <= 50, "Account number is required and cannot exceed 50 characters.");
        rules.Assert(state.Name.Length is >= 1 and <= 120, "Name is required and cannot exceed 120 characters.");
        rules.Assert(state.Email.Length is >= 1 and <= 320 && IsValidEmail(state.Email),
            "Email must be syntactically valid and cannot exceed 320 characters.");
        rules.Assert(state.Phone is null || state.Phone.Length <= 50, "Phone cannot exceed 50 characters.");
        rules.Assert(state.CreditLimit >= 0 && state.CreditLimit <= MaximumCreditLimit &&
                     state.CreditLimit == decimal.Round(state.CreditLimit, 2),
            "Credit limit must be non-negative, fit decimal(19,2), and have no more than two fractional digits.");
        rules.Assert(state.PaymentTermsDays is 0 or 15 or 30 or 45 or 60,
            "Payment terms must be 0, 15, 30, 45, or 60 days.");
        rules.ThrowIfAny();
    }

    private static bool IsValidEmail(string value)
    {
        if (value.Any(char.IsWhiteSpace))
            return false;
        try
        {
            return new MailAddress(value).Address == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    #endregion
}
