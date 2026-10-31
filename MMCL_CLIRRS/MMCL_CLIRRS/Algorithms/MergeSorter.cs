namespace MMCL_CLIRRS;

public static class MergeSorter
{
    public static List<Reservation> Sort(List<Reservation> items, IComparer<Reservation> comparer)
    {
        if (items.Count <= 1) return new List<Reservation>(items);

        int mid = items.Count / 2;
        List<Reservation> left = Sort(items.GetRange(0, mid), comparer);
        List<Reservation> right = Sort(items.GetRange(mid, items.Count - mid), comparer);
        return Merge(left, right, comparer);
    }

    private static List<Reservation> Merge(List<Reservation> left, List<Reservation> right,
                                           IComparer<Reservation> comparer)
    {
        var result = new List<Reservation>(left.Count + right.Count);
        int i = 0, j = 0;

        while (i < left.Count && j < right.Count)
            result.Add(comparer.Compare(left[i], right[j]) <= 0 ? left[i++] : right[j++]);

        while (i < left.Count) result.Add(left[i++]);
        while (j < right.Count) result.Add(right[j++]);
        return result;
    }
}