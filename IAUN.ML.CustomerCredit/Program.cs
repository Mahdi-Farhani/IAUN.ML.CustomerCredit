// See https://aka.ms/new-console-template for more information
using IAUN.ML.CustomerCredit;
using IAUN.ML.CustomerCredit.Extensions;


Console.WriteLine("Loading data...");
var data = DataPreparation.LoadCsv("CC GENERAL.csv");
Console.WriteLine($"Read {data.Count} records.");

string[] categorical = [nameof(CreditCardInfo.CashAdvanceTrx), nameof(CreditCardInfo.PurchaseTrx), nameof(CreditCardInfo.Tenure)];


Console.WriteLine("Imputing missing values...");
data.ImputeMissing(categorical);

Console.WriteLine("Running k-means (Gower)...");
for (int k = 4; k <= 10; k++)
{
    var kmeans = new KMeansGower<CreditCardInfo>(data, categorical);
    var (_, clusters, inertia) = kmeans.Run(k, maxIter: 100);

    Console.WriteLine($"k = {k}, Inertia = {inertia:F4}");
    for (int i = 0; i < k; i++)
        Console.WriteLine($"  Cluster {i}: size = {clusters[i].Count}");
    Console.WriteLine(new string('-', 40));
}

Console.WriteLine("Done.");
Console.ReadKey();