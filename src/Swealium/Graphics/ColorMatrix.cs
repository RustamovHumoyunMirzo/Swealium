namespace Swealium.Graphics;

/// <summary>Immutable row-major 4x5 RGBA matrix. Offsets use the 0..255 color channel scale.</summary>
public sealed class ColorMatrix
{
    internal float[] Values { get; }
    public ColorMatrix(ReadOnlySpan<float> values)
    {
        if (values.Length != 20) throw new ArgumentException("A color matrix requires 20 values.", nameof(values));
        foreach (float value in values) if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values));
        Values = values.ToArray();
    }
    public float[] CopyValues() => (float[])Values.Clone();
    public static ColorMatrix Identity { get; } = new([1,0,0,0,0, 0,1,0,0,0, 0,0,1,0,0, 0,0,0,1,0]);
    public static ColorMatrix Saturation(float amount)
    {
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        float r = .2126f*(1-amount), g = .7152f*(1-amount), b = .0722f*(1-amount);
        return new([r+amount,g,b,0,0, r,g+amount,b,0,0, r,g,b+amount,0,0, 0,0,0,1,0]);
    }
    public static ColorMatrix Grayscale(float amount = 1) { ImageLayout.ValidateOpacity(amount); return Saturation(1-amount); }
    /// <summary>Multiplicative RGB brightness; 1 leaves brightness unchanged.</summary>
    public static ColorMatrix Brightness(float amount)
    { if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); return new([amount,0,0,0,0, 0,amount,0,0,0, 0,0,amount,0,0, 0,0,0,1,0]); }
    public static ColorMatrix Contrast(float amount)
    { if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); float t = 127.5f*(1-amount); return new([amount,0,0,0,t, 0,amount,0,0,t, 0,0,amount,0,t, 0,0,0,1,0]); }
}
