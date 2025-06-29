using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IAUN.ML.CustomerCredit.Extensions;

public static class DatasetExtensions
{
    public static List<DistanceInfo> CalculateDistances<T>(this ICollection<T> list, string keyPropertyName) where T : class
    {
        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var keyProperty = allProperties.Single(x => x.Name == keyPropertyName);
        var properties = allProperties.Except([keyProperty]).ToList();
        var result = new List<DistanceInfo>();
        object sync = new();
        var numericGetters = new List<RangeModel<T>>();
        var categoricalGetters = new List<Func<T, int?>>();


        properties.ForEach(property =>
        {

            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
            if (underlyingType == null) return;

            var isCategorical = underlyingType.IsAssignableTo(typeof(int?));

            if (isCategorical)
            {
                categoricalGetters.Add(x => (int?)property.GetValue(x));
            }
            else
            {
                numericGetters.Add(new RangeModel<T>
                {
                    Property = property,
                    Getter = x => (double?)property.GetValue(x),
                });

            }
        });

        var propertiesCount = properties.Count;


        var param = Expression.Parameter(typeof(T), "e");
        numericGetters.ForEach(p =>
        {

            double minValue = double.MaxValue, maxValue = double.MinValue;
            foreach (var item in list)
            {
                var v = p.Getter(item) ?? 0.0;
                if (v < minValue) minValue = v;
                if (v > maxValue) maxValue = v;
            }
            var range = maxValue - minValue;

            p.MaxValue = maxValue;
            p.MinValue = minValue;
            p.Range = range == 0 ? 1e-8 : range;

        });
        var count = list.Count;
        var listArray = list.ToArray();
        Parallel.For(0, count, () => new List<DistanceInfo>(), (i, loopState, localList) =>
        {
            var xi = listArray[i];
            var firstKey = keyProperty.GetValue(xi)!.ToString() ?? "---";

            for (int j = i + 1; j < count; j++)
            {
                var xj = listArray[j];
                double distance = 0;
                var secondKey = keyProperty.GetValue(xj)!.ToString() ?? "---";

                foreach (var numericalProperty in numericGetters)
                {
                    var firstValue = numericalProperty.Getter(xi) ?? 0;
                    var secondValue = numericalProperty.Getter(xj) ?? 0;
                    distance += Math.Abs(firstValue - secondValue) / numericalProperty.Range;
                }

                foreach (var categoricalProperty in categoricalGetters)
                {
                    var firstValue = categoricalProperty(xi) ?? 0;
                    var secondValue = categoricalProperty(xj) ?? 0;
                    distance += firstValue.Equals(secondValue) ? 0 : 1;
                }



                var gowerDistance = distance / propertiesCount;
                localList.Add(new DistanceInfo { FirstRecordId = firstKey, SecondRecordId = secondKey, Distance = gowerDistance });
            }
            return localList;
        },
        localList =>
        {
            lock (sync)
            {
                result.AddRange(localList);
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
