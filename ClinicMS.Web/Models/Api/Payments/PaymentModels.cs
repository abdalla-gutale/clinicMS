namespace ClinicMS.Web.Models.Api.Payments;

public enum PaymentMethod
{
    Cash,
    CreditCard,
    BankTransfer,
    WalletCredit
}

public enum PaymentStatus
{
    Paid,
    Partial,
    Unpaid
}

public record PaymentDto(
    int Id,
    int? InvoiceId,
    int PatientId,
    string PatientName,
    decimal AmountPaid,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    DateTime PaymentDate,
    int? AccountId,
    string? AccountName);

public record CreatePaymentRequest(
    int? InvoiceId,
    int PatientId,
    decimal AmountPaid,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    int? AccountId);

/// <summary>An invoice with a balance still owed, for an accounts-receivable view. There is no
/// "list all invoices" endpoint on the real API -- this (and GetById for drill-down) is the only
/// global invoice read available without picking a specific patient first.</summary>
public record OutstandingInvoiceDto(
    int InvoiceId,
    int? PatientId,
    string? PatientName,
    string? PatientPhone,
    DateTime InvoiceDate,
    decimal NetAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    PaymentStatus PaymentStatus);

public record InvoiceItemDto(
    int Id,
    string ItemType,
    int? ServiceId,
    string? ServiceName,
    int? ProductSkuId,
    string? ProductName,
    string? SkuCode,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    int? PatientId,
    string? PatientName,
    int? SessionId,
    string InvoiceType,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal VatAmount,
    decimal NetAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    PaymentStatus PaymentStatus,
    DateTime InvoiceDate,
    IReadOnlyList<InvoiceItemDto> Items);

public record RevenueSummaryDto(
    DateOnly From,
    DateOnly To,
    decimal TotalNetAmount,
    decimal TotalDiscountAmount);

/// <summary>Where money collected (Payments) and spent (Expenses) sits, per configured Payment
/// Account -- lets reports show income vs. expense broken down by Cash/EVC/Merchant account rather
/// than just an aggregate total.</summary>
public record AccountBreakdownDto(int? AccountId, string AccountName, decimal TotalIncome, decimal TotalExpense, decimal Net);

public enum RefundType
{
    Full,
    Partial
}

public record RefundItemDto(int ProductSkuId, string SkuCode, string ProductName, int Quantity, decimal RefundUnitPrice, bool RestockItem);

public record RefundItemRequest(int ProductSkuId, int Quantity, decimal RefundUnitPrice, bool RestockItem);

public record ProductRefundDto(
    int Id, int InvoiceId, string InvoiceNumber, int? PatientId, string? PatientName,
    decimal TotalRefundAmount, RefundType RefundType, string? Reason, DateTime RefundDate,
    IReadOnlyList<RefundItemDto> Items);

public record CreateProductRefundRequest(int InvoiceId, RefundType RefundType, string? Reason, IReadOnlyList<RefundItemRequest> Items);
