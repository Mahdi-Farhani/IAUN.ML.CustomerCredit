namespace IAUN.ML.CustomerCredit;

public class Clustering
{

    public static Dictionary<int, List<CreditCardInfo>> KMeans(int maxItteration, int k, List<CreditCardInfo> dataset, Dictionary<(string, string), double> distances)
    {
        var labelPrefix = "cluster_";
        var results = new Dictionary<int, List<CreditCardInfo>>();
        var list = new List<CreditCardInfo>(dataset);

        var centeroids = new List<CreditCardInfo>();
        var count = list.Count;
        var rnd = new Random();
        var indices = Enumerable.Range(0, count).OrderBy(_ => rnd.Next()).Take(k).ToList();




        double GetDistance(string a, string b)
        {
            if (a == b) return 0;
            var key = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
            return distances.TryGetValue(key, out var dd) ? dd
                                                         : throw new KeyNotFoundException($"No distance for {a},{b}");
        }


        foreach (var item in indices)
        {
            centeroids.Add(list[item]);
        }

        var lables = new int[count];
        var changed = true;
        var iteration = 0;
        while (changed && iteration < maxItteration)
        {
            changed = false;
            iteration++;

            Parallel.For(0, count, i =>
            {
                double bestDistance = double.MaxValue;
                int bestK = -1;

                var first = list[i].CustomerId;
                for (int ci = 0; ci < k; ci++)
                {
                    var second = centeroids[ci].CustomerId;
                    double distance = GetDistance(first, second);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestK = ci;
                    }

                    list[i].Label = $"{labelPrefix}{bestK}";
                }
            });

            var newCenteroids = new List<CreditCardInfo>();

            for (int ci = 0; ci < k; ci++)
            {
                var records = list.Where(x => x.Label == $"{labelPrefix}{ci}").ToList();
                if (records.Count == 0) continue;

                double bestTotal = double.MaxValue;
                var bestMember = records[0];

                foreach (var candidate in records)
                {
                    double sumDistance = 0;
                    var candidateId = candidate.CustomerId;
                    foreach (var otherMember in records)
                    {
                        if (candidate.CustomerId == otherMember.CustomerId) continue;
                        var otherMemberId = otherMember.CustomerId;
                        sumDistance += GetDistance(candidateId, otherMemberId);
                    }
                    if (sumDistance < bestTotal)
                    {
                        bestTotal = sumDistance;
                        bestMember = candidate;
                    }

                }
                newCenteroids.Add(bestMember);
            }

            if (!centeroids.SequenceEqual(newCenteroids))
            {
                centeroids = newCenteroids;
                changed = true;
            }

        }
        foreach (var center in centeroids)
        {
            center.IsCenter = true;
        }
        for (int i = 0; i < k; i++)
        {
            results.Add(i, [.. list.Where(x => x.Label == $"{labelPrefix}{i}")]);
        }

        return results;
    }
}
