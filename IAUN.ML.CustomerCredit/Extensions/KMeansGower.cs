using System.Reflection;

namespace IAUN.ML.CustomerCredit.Extensions;

public class KMeansGower<T> where T : class, new()
{
    private readonly IList<T> data;
    private readonly List<PropertyInfo> numericalPropertiesInfo;
    private readonly List<PropertyInfo> categoricalPropertiesInfo;
    private readonly double[] mins, maxs, ranges;
    private readonly int dimension;

    public KMeansGower(IList<T> data, IEnumerable<string> categoricalFeatureNames)
    {
        this.data = data;
        var categoricalProperties = new HashSet<string>(categoricalFeatureNames);
        var excludeProperties = new[] { "CustomerId", "Label", "IsCenter" };

        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !excludeProperties.Contains(p.Name))
            .ToList();

        categoricalPropertiesInfo = allProperties.Where(p => categoricalProperties.Contains(p.Name)).ToList();
        numericalPropertiesInfo = [.. allProperties.Except(categoricalPropertiesInfo)
                       .Where(p =>
                       {
                           var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                           return t == typeof(double) || t == typeof(int)
                                  || t == typeof(float) || t == typeof(decimal);
                       })];

        int m = numericalPropertiesInfo.Count;
        mins = [.. Enumerable.Repeat(double.MaxValue, m)];
        maxs = [.. Enumerable.Repeat(double.MinValue, m)];
        foreach (var x in this.data)
        {
            for (int j = 0; j < m; j++)
            {
                var raw = numericalPropertiesInfo[j].GetValue(x);
                if (raw == null) continue;
                double v = Convert.ToDouble(raw);
                if (v < mins[j]) mins[j] = v;
                if (v > maxs[j]) maxs[j] = v;
            }
        }
        ranges = new double[m];
        for (int j = 0; j < m; j++)
            ranges[j] = (maxs[j] - mins[j] > 1e-8) ? (maxs[j] - mins[j]) : 1.0;

        dimension = numericalPropertiesInfo.Count + categoricalPropertiesInfo.Count;
    }

    private double GowerDistance(T a, T b)
    {
        double sum = 0;

        for (int j = 0; j < numericalPropertiesInfo.Count; j++)
        {
            double xa = Convert.ToDouble(numericalPropertiesInfo[j].GetValue(a) ?? 0.0);
            double xb = Convert.ToDouble(numericalPropertiesInfo[j].GetValue(b) ?? 0.0);
            sum += Math.Abs(xa - xb) / ranges[j];
        }

        foreach (var p in categoricalPropertiesInfo)
        {
            var va = p.GetValue(a)?.ToString();
            var vb = p.GetValue(b)?.ToString();
            sum += (va == vb ? 0.0 : 1.0);
        }
        return sum / dimension;
    }

    public (List<T> Centers, Dictionary<int, List<T>> Clusters, double Inertia) Run(int k, int maxIter = 100)
    {
        var rnd = new Random();

        var centers = data.OrderBy(_ => rnd.Next())
                           .Take(k)
                           .Select(x => Copy(x))
                           .ToList();

        Dictionary<int, List<T>> clusters = [];
        double prevInertia = double.MaxValue;

        for (int iter = 0; iter < maxIter; iter++)
        {

            clusters = Enumerable.Range(0, k).ToDictionary(i => i, i => new List<T>());
            foreach (var x in data)
            {
                int best = 0;
                double bd = GowerDistance(x, centers[0]);
                for (int c = 1; c < k; c++)
                {
                    var d = GowerDistance(x, centers[c]);
                    if (d < bd) { bd = d; best = c; }
                }
                clusters[best].Add(x);
            }

            double inertia = 0;
            foreach (var kv in clusters)
            {
                foreach (var x in kv.Value)
                {
                    inertia += GowerDistance(x, centers[kv.Key]);
                }
            }
            
            if (Math.Abs(inertia - prevInertia) < 1e-6) break;
            prevInertia = inertia;

            var newCenters = new List<T>();
            foreach (var kv in clusters)
            {
                var block = kv.Value;
                var ctr = new T();

                foreach (var p in numericalPropertiesInfo)
                {
                    var values = block.Select(x => p.GetValue(x))
                                    .Where(v => v != null)
                                    .Select(v => Convert.ToDouble(v))
                                    .ToList();
                    double avg = values.Count != 0 ? values.Average() : 0.0;
                    p.SetValue(ctr, Convert.ChangeType(avg, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
                }
                
                foreach (var p in categoricalPropertiesInfo)
                {
                    var mode = block.Select(x => p.GetValue(x)?.ToString())
                                    .Where(s => s != null)
                                    .GroupBy(s => s)
                                    .OrderByDescending(g => g.Count())
                                    .First().Key;
                    p.SetValue(ctr, Convert.ChangeType(mode, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
                }
                newCenters.Add(ctr);
            }
            centers = newCenters;
        }

        return (centers, clusters, prevInertia);
    }

    private static T Copy(T src)
    {
        var dst = new T();
        foreach (var p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => p.CanRead && p.CanWrite))
            p.SetValue(dst, p.GetValue(src));
        return dst;
    }
}
