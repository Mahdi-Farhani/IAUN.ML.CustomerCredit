using System.Reflection;

namespace IAUN.ML.CustomerCredit;

public class DistanceInfo
{
    public (string,string) Key { get; set; }
    public double Distance { get; set; }

}

public class RangeModel<T>
{
    public PropertyInfo Property { get; set; } = default!;
    public Func<T, double?> Getter { get; set; } = default!;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double Range { get; set; }

}