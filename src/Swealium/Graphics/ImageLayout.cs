namespace Swealium.Graphics;

public enum ImageStretch { None, Fill, Uniform, UniformToFill }
internal static class ImageLayout
{
    internal static Size Measure(Size source,Size available,ImageStretch stretch)
    {
        if(source.Width<=0||source.Height<=0||stretch==ImageStretch.None)return source;
        if(stretch==ImageStretch.Fill)return new(float.IsFinite(available.Width)?available.Width:source.Width,float.IsFinite(available.Height)?available.Height:source.Height);
        float scale=float.PositiveInfinity;
        if(float.IsFinite(available.Width))scale=Math.Min(scale,available.Width/source.Width);
        if(float.IsFinite(available.Height))scale=Math.Min(scale,available.Height/source.Height);
        if(!float.IsFinite(scale))scale=1;
        return new(source.Width*scale,source.Height*scale);
    }
    internal static Rect Fit(Size source,Rect target,ImageStretch stretch,UI.Alignment horizontal,UI.Alignment vertical)
    {
        float width=source.Width,height=source.Height;
        if(width<=0||height<=0)return new(target.X,target.Y,0,0);
        if(stretch==ImageStretch.Fill){width=target.Width;height=target.Height;}
        else if(stretch!=ImageStretch.None)
        {
            float scale=stretch==ImageStretch.Uniform?Math.Min(target.Width/width,target.Height/height):Math.Max(target.Width/width,target.Height/height);
            width*=scale;height*=scale;
        }
        return new(target.X+Offset(target.Width-width,horizontal),target.Y+Offset(target.Height-height,vertical),width,height);
    }
    private static float Offset(float extra,UI.Alignment alignment)=>alignment switch{UI.Alignment.Center=>extra/2,UI.Alignment.End=>extra,_=>0};
    internal static void ValidateRect(Rect rect)
    {if(!float.IsFinite(rect.X)||!float.IsFinite(rect.Y)||!float.IsFinite(rect.Width)||!float.IsFinite(rect.Height)||!float.IsFinite(rect.X+rect.Width)||!float.IsFinite(rect.Y+rect.Height)||rect.Width<0||rect.Height<0)throw new ArgumentOutOfRangeException(nameof(rect));}
    internal static void ValidateOpacity(float value){if(!float.IsFinite(value)||value<0||value>1)throw new ArgumentOutOfRangeException(nameof(value));}
}
