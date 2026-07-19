namespace ClinicMS.Web.Models.Api.Settings;

/// <summary>Where money actually sits or moves through -- distinct from the generic PaymentMethod
/// enum (Cash/CreditCard/BankTransfer/WalletCredit): a clinic can have multiple named accounts of
/// the same type (e.g. two Cash drawers), each tracked separately in reports.</summary>
public enum PaymentAccountType
{
    Cash,
    Evc,
    Merchant
}

public record PaymentAccountDto(int Id, string Name, PaymentAccountType AccountType, string? PhoneOrAccountNumber, bool IsActive);

public record CreatePaymentAccountRequest(string Name, PaymentAccountType AccountType, string? PhoneOrAccountNumber, bool IsActive);

public record UpdatePaymentAccountRequest(string Name, PaymentAccountType AccountType, string? PhoneOrAccountNumber, bool IsActive);
