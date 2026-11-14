namespace Swealium.Graphics;

public readonly record struct Point(float X, float Y);
public readonly record struct Size(float Width, float Height);
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public bool Contains(Point point) =>
        Width > 0 && Height > 0 && point.X >= X && point.Y >= Y && point.X < X + Width && point.Y < Y + Height;
}
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color White => new(255, 255, 255);
    public static Color Black => new(0, 0, 0);
}

