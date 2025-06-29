// See https://aka.ms/new-console-template for more information
using IAUN.ML.CustomerCredit;
using IAUN.ML.CustomerCredit.Extensions;
using System.Diagnostics;

Console.WriteLine("------- IAUN ML Project -------");
Console.WriteLine("Loading Dataset");
var dataset = LoadData();
Console.WriteLine($"{dataset.Count} record read");

dataset.ReplaceNullValues();
var sw = new Stopwatch();

Console.WriteLine("Calculating distances started ....");
sw.Start();
Console.WriteLine($"It will be take about {dataset.Count * 8} ms");
var distances = dataset.CalculateDistances(nameof(CreditCardInfo.CustomerId));
sw.Stop();
Console.WriteLine("Calculating distances finished....");
Console.WriteLine($"Distance calculation took {sw.ElapsedMilliseconds} ms");

Console.ReadKey();


List<CreditCardInfo> LoadData()
{
    var index = 0;
    var dataset = File.ReadLines("CC GENERAL.csv").Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => ReadLine(x, index++))
        .ToList();
    return [.. dataset.Skip(1)];
}

CreditCardInfo ReadLine(string x, int index)
{
    if (index == 0) return new CreditCardInfo();
    var information = x.Split(',').ToList();    
    var row = new CreditCardInfo
    {
        CustomerId = information[0],
        Balance = string.IsNullOrEmpty(information[1]) ? null : double.Parse(information[1]),
        BalanceFrequency = string.IsNullOrEmpty(information[2]) ? null : double.Parse(information[2]),
        Purchases = string.IsNullOrEmpty(information[3]) ? null : double.Parse(information[3]),
        OneOffPurchase = string.IsNullOrEmpty(information[4]) ? null : double.Parse(information[4]),
        InstallmentsPurchases = string.IsNullOrEmpty(information[5]) ? null : double.Parse(information[5]),
        CashAdvance = string.IsNullOrEmpty(information[6]) ? null : double.Parse(information[6]),
        PurchaseFrequency = string.IsNullOrEmpty(information[7]) ? null : double.Parse(information[7]),
        OneOffPurchaseFrequency = string.IsNullOrEmpty(information[8]) ? null : double.Parse(information[8]),
        PurchasesInstallmentsFrequency = string.IsNullOrEmpty(information[9]) ? null : double.Parse(information[9]),
        CashAdvanceFrequency = string.IsNullOrEmpty(information[10]) ? null : double.Parse(information[10]),
        CashAdvanceTrx = string.IsNullOrEmpty(information[11]) ? null : int.Parse(information[11]),
        PurchaseTrx = string.IsNullOrEmpty(information[12]) ? null : int.Parse(information[12]),
        CreditLimit = string.IsNullOrEmpty(information[13]) ? null : double.Parse(information[13]),
        Payments = string.IsNullOrEmpty(information[14]) ? null : double.Parse(information[14]),
        MinimumPayments = string.IsNullOrEmpty(information[15]) ? null : double.Parse(information[15]),
        PRCFullPayment = string.IsNullOrEmpty(information[16]) ? null : double.Parse(information[16]),
        Tenure = string.IsNullOrEmpty(information[17]) ? null : int.Parse(information[17]), 
        

    };
    return row;
}