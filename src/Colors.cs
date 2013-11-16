using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace go
{
    public struct Color
    {
        public double A;
        public double R;
        public double G;
        public double B;

        public Color(double _R, double _G, double _B, double _A)
        {
            A = _A;
            R = _R;
            G = _G;
            B = _B;
        }
        public static implicit operator System.Drawing.Color(Color c)
        {
            return System.Drawing.Color.FromArgb((int)(c.A * 255), (int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255));
        }
        public static implicit operator Cairo.Color(Color c)
        {
            return new Cairo.Color(c.R, c.G, c.B, c.A);
        }
        public static readonly Color White = new Color(1, 1, 1, 1);
        public static readonly Color Black = new Color(0, 0, 0, 1);
        public static readonly Color LightGray = new Color(0.8, 0.8, 0.8, 1);
        public static readonly Color DarkGray = new Color(0.2, 0.2, 0.2, 1);
        public static readonly Color Gray = new Color(0.5, 0.5, 0.5, 1);
        public static readonly Color DimGray = new Color(0.2, 0.2, 0.2, 0.8);
        public static readonly Color Transparent = new Color(0.0, 0.0, 0.0, 0.0);
        public static readonly Color Red1 = new Color(0.9, 0.4, 0.4, 0.9);
        public static readonly Color DimWhite = new Color(0.9, 0.9, 0.9, 0.8);
        public static readonly Color ElectricBlue = new Color(0.3, 0.3, 0.6, 1);
        public static readonly Color SkyBlue = new Color(0.7, 0.8, 1, 1);
        public static readonly Color Lavande = new Color(0.8, 0.8, 1, 1);
        public static readonly Color YellowGreen = new Color(0.8, 0.8, 0.1, 1);
        public static readonly Color blue1 = new Color(0.3, 0.3, 0.4, 1);
        public static readonly Color Green = new Color(0, 1, 0, 1);
        public static readonly Color Blue = new Color(0, 0, 1, 1);
        public static readonly Color Red = new Color(1, 0, 0, 1);
        public static readonly Color LightGoldenrodYellow = new Color(0.5, 0.5, 0, 0.5);
        public static readonly Color DarkOrange = new Color(1, 0.2, 0, 1);
        public static readonly Color MediumBlue = new Color(0, 0, 1, 1);
        public static readonly Color LightBlue = new Color(0.7, 0.7, 1, 1);
        public static readonly Color Navy = new Color(0, 0, 1, 1);
        public static readonly Color MediumTurquoise = new Color(0, 0, 1, 1);
        public static readonly Color Goldenrod = new Color(0, 0, 1, 1);
        public static readonly Color Yellow = new Color(1, 1, 0, 1);
    }
}
