using System.Reflection;

namespace IAUN.ML.CustomerCredit.Extensions;

public static class DataPreparation
{
    public static void ImputeMissing<T>(this IList<T> list, IEnumerable<string> categoricalPropertiesClaim)
            where T : class
    {
        var excludeProperties = new[] { "CustomerId","Label", "IsCenter" };
        var categorical = new HashSet<string>(categoricalPropertiesClaim);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !excludeProperties.Contains(p.Name))
            .ToList();

        var categoricalProperties = properties.Where(p => categorical.Contains(p.Name)).ToList();
        var numericalProperties = properties.Except(categoricalProperties)
            .Where(p =>
            {
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                return t == typeof(double) || t == typeof(int) || t == typeof(float) || t == typeof(decimal);
            })
            .ToList();

        foreach (var p in numericalProperties)
        {
            var values = list
                .Select(x => p.GetValue(x))
                .Where(v => v != null)
                .Select(v => Convert.ToDouble(v))
                .ToList();
            if (values.Count == 0) continue;
            double avg = values.Average();

            foreach (var x in list.Where(x => p.GetValue(x) == null))
            {
                if (p==null) continue;
                p.SetValue(x, Convert.ChangeType(avg, Nullable.GetUnderlyingType(p.PropertyType)??typeof(object)));
            }
        }

        foreach (var p in categoricalProperties)
        {
            var values = list
                .Select(x => p.GetValue(x))
                .Where(v => v != null)
                .Select(v => v!.ToString())
                .ToList();
            if (values.Count == 0) continue;
            var mode = values
                .GroupBy(s => s)
                .OrderByDescending(g => g.Count())
                .First().Key;

            foreach (var x in list.Where(x => p.GetValue(x) == null))
                p.SetValue(x, Convert.ChangeType(mode, Nullable.GetUnderlyingType(p.PropertyType) ?? typeof(object)));
        }
    }

    public static List<CreditCardInfo> LoadCsv(string path)
    {
        var lines = File.ReadAllLines(path)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
        var data = new List<CreditCardInfo>();
        for (int i = 1; i < lines.Count; i++)
        {
            var fields = lines[i].Split(',');
            var o = new CreditCardInfo
            {
                CustomerId = fields[0],
                Balance = TryParseDouble(fields[1]),
                BalanceFrequency = TryParseDouble(fields[2]),
                Purchases = TryParseDouble(fields[3]),
                OneOffPurchase = TryParseDouble(fields[4]),
                InstallmentsPurchases = TryParseDouble(fields[5]),
                CashAdvance = TryParseDouble(fields[6]),
                PurchaseFrequency = TryParseDouble(fields[7]),
                OneOffPurchaseFrequency = TryParseDouble(fields[8]),
                PurchasesInstallmentsFrequency = TryParseDouble(fields[9]),
                CashAdvanceFrequency = TryParseDouble(fields[10]),
                CashAdvanceTrx = TryParseInt(fields[11]),
                PurchaseTrx = TryParseInt(fields[12]),
                CreditLimit = TryParseDouble(fields[13]),
                Payments = TryParseDouble(fields[14]),
                MinimumPayments = TryParseDouble(fields[15]),
                PRCFullPayment = TryParseDouble(fields[16]),
                Tenure = TryParseInt(fields[17])
            };
            data.Add(o);
        }
        return data;
    }

    private static double? TryParseDouble(string s)
        => double.TryParse(s, out var d) ? (double?)d : null;
    private static int? TryParseInt(string s)
        => int.TryParse(s, out var i) ? (int?)i : null;
}

