using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace IAUN.ML.CustomerCredit.Extensions;
public static class DatasetExtensions
{
    public static Dictionary<(string,string),double> CalculateDistances<T>(
        this IList<T> list,
        string keyPropertyName)
        where T : class
    {
        var dictionaryResult = new ConcurrentDictionary<(string, string), double>();

        var excludeProperties = new[] { "Label", "IsCenter" };
        var allProps = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !excludeProperties.Contains(p.Name))
            .ToArray();

        var keyProp = allProps.Single(p => p.Name == keyPropertyName);
        var featureProps = allProps.Where(p => p != keyProp).ToArray();

        var numericProps = new List<PropertyInfo>();
        var categoricalProps = new List<PropertyInfo>();

        foreach (var p in featureProps)
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (t == typeof(int))
            {
                categoricalProps.Add(p);
            }
            else if (t == typeof(double) || t == typeof(float) || t == typeof(decimal))
            {
                numericProps.Add(p);
            }

        }

        int n = list.Count;
        int numNum = numericProps.Count;
        int numCat = categoricalProps.Count;
        int dim = numNum + numCat;

        var keys = new string[n];
        var numData = new float[n][];

        var mins = new double[numNum];
        var maxs = new double[numNum];
        for (int k = 0; k < numNum; k++)
        {
            mins[k] = double.MaxValue;
            maxs[k] = double.MinValue;
        }

        var catData = new byte[n][];


        for (int i = 0; i < n; i++)
        {
            var item = list[i];
            keys[i] = keyProp.GetValue(item)?.ToString() ?? "";

            var rowNum = new float[numNum];
            for (int k = 0; k < numNum; k++)
            {
                object raw = numericProps[k].GetValue(item) ?? 0.0;
                double v = Convert.ToDouble(raw);
                if (v < mins[k]) mins[k] = v;
                if (v > maxs[k]) maxs[k] = v;
                rowNum[k] = (float)v;
            }
            numData[i] = rowNum;

            var rowCat = new byte[numCat];
            for (int k = 0; k < numCat; k++)
            {
                object raw = categoricalProps[k].GetValue(item) ?? 0;
                rowCat[k] = (byte)Convert.ToInt32(raw);
            }
            catData[i] = rowCat;
        }

        var ranges = new double[numNum];
        for (int k = 0; k < numNum; k++)
        {
            double r = maxs[k] - mins[k];
            ranges[k] = (r == 0 ? 1e-8 : r);
        }
        for (int i = 0; i < n; i++)
        {
            for (int k = 0; k < numNum; k++)
            {
                numData[i][k] = (float)((numData[i][k] - mins[k]) / ranges[k]);
            }
        }

        int pairCount = (n * (n - 1)) / 2;
        var result = new Dictionary<(string, string), double>((int)pairCount);
        object locker = new();
        int totalFeatures = numNum + numCat;

        Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            () => new Dictionary<(string,string),double>(),
            (i, state, localBuf) =>
        {
            var xiNum = numData[i];
            var xiCat = catData[i];
            string keyI = keys[i];

            for (int j = i + 1; j < n; j++)
            {
                var xjNum = numData[j];
                var xjCat = catData[j];
                string keyJ = keys[j];


                float sum = 0f;
                for (int k = 0; k < numNum; k++)
                {
                    sum += MathF.Abs(xiNum[k] - xjNum[k]);
                }
                for (int k = 0; k < numCat; k++)
                {
                    sum += (xiCat[k] == xjCat[k] ? 0f : 1f);
                }

                double gower = sum / totalFeatures;




                var key = keyI.CompareTo(keyJ) <= 0 ? (keyI, keyJ) : (keyJ, keyI);

                localBuf.Add(key,gower);
                
            }
            return localBuf;
        },
        localBuf =>
        {
            lock (locker)
            {
                foreach (var kv in localBuf)
                    result[kv.Key] = kv.Value;
            }
        });

        return result;
    }



    public static void ReplaceNullValues<T>(this ICollection<T> list) where T : class
    {
        var nullableProperties = typeof(T)
          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p =>
          {
              var underlyingType = Nullable.GetUnderlyingType(p.PropertyType);
              return underlyingType != null
                     && underlyingType.IsValueType
                     && p.CanRead
                     && p.CanWrite;
          })
          .ToList();

        nullableProperties.ForEach(x =>
        {
            var underlyingType = Nullable.GetUnderlyingType(x.PropertyType);
            if (underlyingType == null) return;

            var isCategorical = underlyingType.IsAssignableTo(typeof(int?));

            var param = Expression.Parameter(typeof(T), "e");
            var access = Expression.Property(param, x);
            var isNull = Expression.Equal(access, Expression.Constant(null, x.PropertyType));
            var lambda = Expression.Lambda<Func<T, bool>>(isNull, param).Compile();

            var toFix = list.Where(lambda).ToList();
            if (toFix.Count == 0) return;

            if (isCategorical)
            {
                var intSelector = Expression.Lambda<Func<T, int?>>(access, param).Compile();
                var max = list.Max(intSelector);

                foreach (var item in toFix)
                {
                    var value = Convert.ChangeType(max, underlyingType!);
                    x.SetValue(item, max);
                }
            }
            else
            {
                var doubleSelector = Expression.Lambda<Func<T, double?>>(access, param).Compile();
                var avg = list.Average(doubleSelector);
                foreach (var item in toFix)
                {
                    var value = Convert.ChangeType(avg, underlyingType!);
                    x.SetValue(item, avg);
                }
                return;
            }

        });




    }
}
