// See https://aka.ms/new-console-template for more information
using IAUN.ML.CustomerCredit;
using IAUN.ML.CustomerCredit.Extensions;
using ScottPlot;


Console.WriteLine("Loading data...");
var data = DataPreparation.LoadCsv("CC GENERAL.csv");
Console.WriteLine($"Read {data.Count} records.");

string[] categorical = [nameof(CreditCardInfo.CashAdvanceTrx), nameof(CreditCardInfo.PurchaseTrx), nameof(CreditCardInfo.Tenure)];


Console.WriteLine("Imputing missing values...");
data.ImputeMissing(categorical);

Console.WriteLine("Running k-means (Gower)...");
var clusterCount = 15;
var inertias = new List<double>();
for (int k = 4; k <= clusterCount; k++)
{
    var kmeans = new KMeansGower<CreditCardInfo>(data, categorical);
    var (_, clusters, inertia) = kmeans.Run(k, maxIter: 100);
    inertias.Add(inertia);
    Console.WriteLine($"k = {k}, Inertia = {inertia:F4}");
    for (int i = 0; i < k; i++)
        Console.WriteLine($"  Cluster {i}: size = {clusters[i].Count}");
    Console.WriteLine(new string('-', 40));
}

var xs = Enumerable.Range(4, clusterCount).Select(x => (double)x).ToArray();
var ys = inertias.ToArray();
var plot=new ScottPlot.Plot();
plot.Add.Scatter(xs, ys);
plot.Title("Elbow plot");
plot.XLabel("Number of clusers k");
plot.YLabel("Inertia (Sum of distance)");
plot.SavePng("elbow.png",600,800);

Console.WriteLine("Done.");
Console.ReadKey();