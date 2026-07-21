namespace ClinicMS.Web.Models.Api.Reports;

/// <summary>Each account's running cash balance -- all payments received into it minus all
/// expenses paid out of it, since the account was created. Purchase orders don't move cash
/// directly (this app has no "pay the PO" step), so they never touch this balance.</summary>
public record BalanceSheetAccountDto(int AccountId, string AccountName, decimal Balance);

/// <summary>A derived, as-of-today snapshot rather than formal double-entry bookkeeping: Assets =
/// cash across all accounts + inventory on hand (at cost) + patient wallet credits owed back to
/// patients (kept as a receivable-style figure per the app's own wallet model). Liabilities = value
/// of purchase orders placed with suppliers but not yet received (money effectively committed).</summary>
public record BalanceSheetDto(
    DateTime AsOf,
    IReadOnlyList<BalanceSheetAccountDto> AccountBalances,
    decimal TotalCash,
    decimal InventoryValue,
    decimal PatientWalletCredits,
    decimal TotalAssets,
    decimal PurchaseOrdersOutstanding,
    decimal TotalLiabilities,
    decimal Equity);
