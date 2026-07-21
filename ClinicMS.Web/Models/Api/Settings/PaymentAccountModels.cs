namespace ClinicMS.Web.Models.Api.Settings;

/// <summary>Where money actually sits or moves through -- distinct from the generic PaymentMethod
/// enum (Cash/CreditCard/BankTransfer/WalletCredit): a clinic can have multiple named accounts of
/// the same type (e.g. two Cash drawers), each tracked separately in reports.</summary>
public enum PaymentAccountType
{
    Cash,
    Merchant,
    MasterCard
}

/// <summary>Mobile money carrier a Cash or Merchant account is actually held with -- not applicable
/// to MasterCard, which is always a plain card number. None means a plain drawer/terminal with no
/// specific carrier attached.</summary>
public enum PaymentAccountTypeSub
{
    None,
    Evc,
    Zaad,
    Somtel
}

public record PaymentAccountDto(
    int Id, string Name, PaymentAccountType AccountType, PaymentAccountTypeSub AccountTypeSub, string? Number, decimal MonthlyBudgetEstimate, bool IsActive);

public record CreatePaymentAccountRequest(
    string Name, PaymentAccountType AccountType, PaymentAccountTypeSub AccountTypeSub, string? Number, decimal MonthlyBudgetEstimate, bool IsActive);

public record UpdatePaymentAccountRequest(
    string Name, PaymentAccountType AccountType, PaymentAccountTypeSub AccountTypeSub, string? Number, decimal MonthlyBudgetEstimate, bool IsActive);
