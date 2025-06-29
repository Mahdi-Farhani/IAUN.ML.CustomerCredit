namespace IAUN.ML.CustomerCredit;

public class CreditCardInfo
{
    public string CustomerId { get; set; }=string.Empty;
    public double? Balance { get; set; }
    public double? BalanceFrequency { get; set; }
    public double? Purchases { get; set; }
    public double? OneOffPurchase { get; set; }
    public double? InstallmentsPurchases { get; set; }
    public double? CashAdvance { get; set; }
    public double? PurchaseFrequency { get; set; }
    public double? OneOffPurchaseFrequency { get; set; }
    public double? PurchasesInstallmentsFrequency { get; set; }
    public double? CashAdvanceFrequency { get; set; }
    public int? CashAdvanceTrx { get; set; }
    public int? PurchaseTrx { get; set; }
    public double? CreditLimit { get; set; }
    public double? Payments { get; set; }
    public double? MinimumPayments { get; set; }
    public double? PRCFullPayment { get; set; }
    public int? Tenure { get; set; }
    
}
