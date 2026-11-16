namespace Swealium.Animation;

/// <summary>Normalized time curves. BackOut and custom Bezier curves can overshoot the value range.</summary>
public static class Easing
{
    private static double P(double value)
    { if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
    public static double Linear(double progress) => P(progress);
    public static double QuadraticIn(double progress) { double p=P(progress); return p*p; }
    public static double QuadraticOut(double progress) { double p=P(progress); return 1-(1-p)*(1-p); }
    public static double QuadraticInOut(double progress) { double p=P(progress); return p<.5?2*p*p:1-Math.Pow(-2*p+2,2)/2; }
    public static double CubicIn(double progress) => Math.Pow(P(progress),3);
    public static double CubicOut(double progress) => 1-Math.Pow(1-P(progress),3);
    public static double CubicInOut(double progress) { double p=P(progress); return p<.5?4*p*p*p:1-Math.Pow(-2*p+2,3)/2; }
    public static double SineInOut(double progress) => -(Math.Cos(Math.PI*P(progress))-1)/2;
    public static double ExponentialOut(double progress) { double p=P(progress); return p==1?1:1-Math.Pow(2,-10*p); }
    public static double BackOut(double progress) { double p=P(progress)-1; return 1+2.70158*p*p*p+1.70158*p*p; }
    public static double BounceOut(double progress)
    {
        double p=P(progress);
        if(p<1/2.75)return 7.5625*p*p;
        if(p<2/2.75){p-=1.5/2.75;return 7.5625*p*p+.75;}
        if(p<2.5/2.75){p-=2.25/2.75;return 7.5625*p*p+.9375;}
        p-=2.625/2.75;return 7.5625*p*p+.984375;
    }
    public static Func<double,double> Steps(int count, bool jumpAtStart=false)
    {
        if(count<1)throw new ArgumentOutOfRangeException(nameof(count));
        return progress=>{double p=P(progress);return p==1?1:Math.Min(1,(Math.Floor(p*count)+(jumpAtStart?1:0))/count);};
    }
    public static Func<double,double> CubicBezier(double x1,double y1,double x2,double y2)
    {
        if(!double.IsFinite(x1)||!double.IsFinite(x2)||x1<0||x1>1||x2<0||x2>1||!double.IsFinite(y1)||!double.IsFinite(y2))throw new ArgumentOutOfRangeException(nameof(x1));
        return progress=>
        {
            double p=P(progress);if(p==0||p==1)return p;
            static double Curve(double t,double a,double b)=>3*(1-t)*(1-t)*t*a+3*(1-t)*t*t*b+t*t*t;
            double low=0,high=1;
            for(int i=0;i<40;i++){double mid=(low+high)/2;if(Curve(mid,x1,x2)<p)low=mid;else high=mid;}
            double value=Curve((low+high)/2,y1,y2);
            if(!double.IsFinite(value))throw new InvalidOperationException("Bezier curve produced a nonfinite value.");return value;
        };
    }
}
