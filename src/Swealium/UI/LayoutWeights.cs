namespace Swealium.UI;

internal static class LayoutWeights
{
    internal static float[] Allocate(Widget[] children, float available, float gap, bool vertical)
    {
        var lengths = children.Select(child =>
            child.Weight > 0 && child.WeightBasis == WeightBasis.Zero && float.IsFinite(available)
                ? (vertical ? child.MinHeight + child.Margin.Vertical : child.MinWidth + child.Margin.Horizontal)
                : (vertical ? child.HeightSizing : child.WidthSizing) == SizingMode.Fill && float.IsFinite(available)
                    ? child.FillOuterLength(available, vertical)
                    : vertical ? child.DesiredSize.Height : child.DesiredSize.Width).ToArray();
        if (!float.IsFinite(available)) return lengths;
        double free = Math.Max(0, (double)available - lengths.Sum(v => (double)v) - gap * Math.Max(0, children.Length - 1));
        var growing = Enumerable.Range(0, children.Length).Where(i => children[i].Weight > 0).ToList();
        while (free > 0.0001 && growing.Count > 0)
        {
            double scale = growing.Max(i => (double)children[i].Weight);
            double total = growing.Sum(i => children[i].Weight / scale), before = free, distributed = 0;
            foreach (int i in growing.ToArray())
            {
                var child = children[i];
                double maximum = vertical ? Math.Max(child.MinHeight, child.MaxHeight) + child.Margin.Vertical
                    : Math.Max(child.MinWidth, child.MaxWidth) + child.Margin.Horizontal;
                double amount = Math.Min(Math.Max(0, maximum - lengths[i]), before * (child.Weight / scale) / total);
                float old = lengths[i];
                lengths[i] = (float)(old + amount);
                distributed += lengths[i] - old;
                if (maximum - lengths[i] <= 0.0001) growing.Remove(i);
            }
            free = Math.Max(0, free - distributed);
            if (distributed <= 0.0001) break;
        }
        return lengths;
    }
}

