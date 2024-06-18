using System;

public class Easings
{
    //constants for Back
    private const double C1 = 1.70158;
    private const double C2 = C1 * 1.525;
    private const double C3 = C1 + 1;
    private const double C4 = 2 * Math.PI / 3;
    private const double N1 = 7.5625;
    private const double D1 = 2.75;


    //1
    public static double EaseLinear(double x)
    {
        return x;
    }
    //2
    public static double EaseOutSin(double x)
    {
        return Math.Sin((x * Math.PI) / 2);
    }
    //3
    public static double EaseInSin(double x)
    {
        return 1 - Math.Cos((x * Math.PI) / 2);
    }
    //4
    public static double EaseOutQuad(double x)
    {
        return 1 - (1 - x) * (1 - x);
    }
    //5
    public static double EaseInQuad(double x)
    {
        return x * x;
    }
    //6
    public static double EaseInOutSin(double x)
    {
        return -(Math.Cos(Math.PI * x) - 1) / 2;
    }
    //7
    public static double EaseInOutQuad(double x)
    {
        return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
    }
    //8
    public static double EaseOutCubic(double x)
    {
        return 1 - Math.Pow(1 - x, 3);
    }
    //9
    public static double EaseInCubic(double x)
    {
        return x * x * x;
    }
    //10
    public static double EaseOutQuart(double x)
    {
        return 1 - Math.Pow(1 - x, 4);
    }
    //11
    public static double EaseInQuart(double x)
    {
        return x * x * x * x;
    }
    //12
    public static double EaseInOutCubic(double x)
    {
        return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
    }
    //13
    public static double EaseInOutQuart(double x)
    {
        return x < 0.5 ? 8 * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 4) / 2;
    }
    //14
    public static double EaseOutQuint(double x)
    {
        return 1 - Math.Pow(1 - x, 5);
    }
    //15
    public static double EaseInQuint(double x)
    {
        return x * x * x * x * x;
    }
    //16
    public static double EaseOutExpo(double x)
    {
        return Math.Abs(x - 1) < 1e-5 ? 1 : 1 - Math.Pow(2, -10.0 * x);
    }
    //17
    public static double EaseInExpo(double x)
    {
        return x == 0 ? 0 : Math.Pow(2, 10 * x - 10);
    }
    //18
    public static double EaseOutCirc(double x)
    {
        return Math.Sqrt(1 - Math.Pow(x - 1, 2));
    }
    //19
    public static double EaseInCirc(double x)
    {
        return 1 - Math.Sqrt(1 - Math.Pow(x, 2));
    }
    //20
    public static double EaseOutBack(double x)
    {
        return 1 + C3 * Math.Pow(x - 1, 3) + C1 * Math.Pow(x - 1, 2);
    }
    //21
    public static double EaseInBack(double x)
    {
        return C3 * x * x * x - C1 * x * x;
    }
    //22
    public static double EaseInOutCirc(double x)
    {
        return x < 0.5
        ? (1 - Math.Sqrt(1 - Math.Pow(2 * x, 2))) / 2
        : (Math.Sqrt(1 - Math.Pow(-2 * x + 2, 2)) + 1) / 2;
    }
    //23
    public static double EaseInOutBack(double x)
    {
        return x < 0.5
        ? (Math.Pow(2 * x, 2) * ((C2 + 1) * 2 * x - C2)) / 2
        : (Math.Pow(2 * x - 2, 2) * ((C2 + 1) * (x * 2 - 2) + C2) + 2) / 2;
    }
    //24
    public static double EaseOutElastic(double x)
    {
        return x == 0
          ? 0
          : Math.Abs(x - 1) < 1e-5
          ? 1
          : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * C4) + 1;
    }
    //25
    public static double EaseInElastic(double x)
    {
        return x == 0
          ? 0
          : Math.Abs(x - 1) < 1e-5
          ? 1
          : -Math.Pow(2, 10 * x - 10) * Math.Sin((x * 10 - 10.75) * C4);
    }
    //26
    public static double EaseOutBounce(double x)
    {
        if (x < 1 / D1)
        {
            return N1 * x * x;
        }

        if (x < 2 / D1)
        {
            return N1 * (x -= 1.5 / D1) * x + 0.75;
        }

        if (x < 2.5 / D1)
        {
            return N1 * (x -= 2.25 / D1) * x + 0.9375;
        }

        return N1 * (x -= 2.625 / D1) * x + 0.984375;

    }
    //27
    public static double EaseInBounce(double x)
    {
        return 1 - EaseOutBounce(1 - x);
    }
    //28
    public static double EaseInOutBounce(double x)
    {
        return x < 0.5
          ? (1 - EaseOutBounce(1 - 2 * x)) / 2
          : (1 + EaseOutBounce(2 * x - 1)) / 2;
    }


    public delegate double EasingFunc(double x);

    public static EasingFunc[] EaseFunctions = {
        EaseLinear,
        EaseLinear,
        EaseOutSin,
        EaseInSin,
        EaseOutQuad,
        EaseInQuad,//5
        EaseInOutSin,
        EaseInOutQuad,
        EaseOutCubic,
        EaseInCubic,
        EaseOutQuart,//10
        EaseInQuart,
        EaseInOutCubic,
        EaseInOutQuart,
        EaseOutQuint,
        EaseInQuint,//15
        EaseOutExpo,
        EaseInExpo,
        EaseOutCirc,
        EaseInCirc,
        EaseOutBack,//20
        EaseInBack,
        EaseInOutCirc,
        EaseInOutBack,
        EaseOutElastic,
        EaseInElastic,//25
        EaseOutBounce,
        EaseInBounce,
        EaseInOutBounce
    };
}
