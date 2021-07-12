// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
//using FastEnumUtility;

namespace Crow
{
	public enum Colors : UInt32
	{
		AliceBlue            = 0xF0F8FFFF,
		AntiqueWhite         = 0xFAEBD7FF,
		//Aqua                 = 0x00FFFFFF,
		Aquamarine           = 0x7FFFD4FF,
		Azure                = 0xF0FFFFFF,
		Beige                = 0xF5F5DCFF,
		Bisque               = 0xFFE4C4FF,
		Black                = 0x000000FF,
		BlanchedAlmond       = 0xFFEBCDFF,
		Blue                 = 0x0000FFFF,
		BlueViolet           = 0x8A2BE2FF,
		Brown                = 0xA52A2AFF,
		BurlyWood            = 0xDEB887FF,
		CadetBlue            = 0x5F9EA0FF,
		Chartreuse           = 0x7FFF00FF,
		Chocolate            = 0xD2691EFF,
		Coral                = 0xFF7F50FF,
		CornflowerBlue       = 0x6495EDFF,
		Cornsilk             = 0xFFF8DCFF,
		Crimson              = 0xDC143CFF,
		Cyan                 = 0x00FFFFFF,
		DarkBlue             = 0x00008BFF,
		DarkCyan             = 0x008B8BFF,
		DarkGoldenRod        = 0xB8860BFF,
		DarkGrey             = 0x202020FF,
		DarkGreen            = 0x006400FF,
		DarkKhaki            = 0xBDB76BFF,
		DarkMagenta          = 0x8B008BFF,
		DarkOliveGreen       = 0x556B2FFF,
		DarkOrange           = 0xFF8C00FF,
		DarkOrchid           = 0x9932CCFF,
		DarkRed              = 0x8B0000FF,
		DarkSalmon           = 0xE9967AFF,
		DarkSeaGreen         = 0x8FBC8FFF,
		DarkSlateBlue        = 0x483D8BFF,
		DarkSlateGrey        = 0x2F4F4FFF,
		DarkTurquoise        = 0x00CED1FF,
		DarkViolet           = 0x9400D3FF,
		DeepPink             = 0xFF1493FF,
		DeepSkyBlue          = 0x00BFFFFF,
		DimGrey              = 0x696969FF,
		DodgerBlue           = 0x1E90FFFF,
		FireBrick            = 0xB22222FF,
		FloralWhite          = 0xFFFAF0FF,
		ForestGreen          = 0x228B22FF,
		Fuchsia              = 0xFF00FFFF,
		Gainsboro            = 0xDCDCDCFF,
		GhostWhite           = 0xF8F8FFFF,
		Gold                 = 0xFFD700FF,
		GoldenRod            = 0xDAA520FF,
		Grey                 = 0x808080FF,
		Green                = 0x008000FF,
		GreenYellow          = 0xADFF2FFF,
		HoneyDew             = 0xF0FFF0FF,
		HotPink              = 0xFF69B4FF,
		IndianRed            = 0xCD5C5CFF,
		Indigo               = 0x4B0082FF,
		Ivory                = 0xFFFFF0FF,
		Jet                  = 0x343434FF,
		Khaki                = 0xF0E68CFF,
		Lavender             = 0xE6E6FAFF,
		LavenderBlush        = 0xFFF0F5FF,
		LawnGreen            = 0x7CFC00FF,
		LemonChiffon         = 0xFFFACDFF,
		LightBlue            = 0xADD8E6FF,
		LightCoral           = 0xF08080FF,
		LightCyan            = 0xE0FFFFFF,
		LightGoldenRodYellow = 0xFAFAD2FF,
		LightGrey            = 0xD3D3D3FF,
		LightGreen           = 0x90EE90FF,
		LightPink            = 0xFFB6C1FF,
		LightSalmon          = 0xFFA07AFF,
		LightSeaGreen        = 0x20B2AAFF,
		LightSkyBlue         = 0x87CEFAFF,
		LightSlateGrey       = 0x778899FF,
		LightSteelBlue       = 0xB0C4DEFF,
		LightYellow          = 0xFFFFE0FF,
		Lime                 = 0x00FF00FF,
		LimeGreen            = 0x32CD32FF,
		Linen                = 0xFAF0E6FF,
		Magenta              = 0xFF00FFFF,
		Maroon               = 0x800000FF,
		MediumAquaMarine     = 0x66CDAAFF,
		MediumBlue           = 0x0000CDFF,
		MediumOrchid         = 0xBA55D3FF,
		MediumPurple         = 0x9370DBFF,
		MediumSeaGreen       = 0x3CB371FF,
		MediumSlateBlue      = 0x7B68EEFF,
		MediumSpringGreen    = 0x00FA9AFF,
		MediumTurquoise      = 0x48D1CCFF,
		MediumVioletRed      = 0xC71585FF,
		MidnightBlue         = 0x191970FF,
		MintCream            = 0xF5FFFAFF,
		MistyRose            = 0xFFE4E1FF,
		Moccasin             = 0xFFE4B5FF,
		NavajoWhite          = 0xFFDEADFF,
		Navy                 = 0x000080FF,
		OldLace              = 0xFDF5E6FF,
		Olive                = 0x808000FF,
		OliveDrab            = 0x6B8E23FF,
		Onyx                 = 0x353839FF,
		Orange               = 0xFFA500FF,
		OrangeRed            = 0xFF4500FF,
		Orchid               = 0xDA70D6FF,
		PaleGoldenRod        = 0xEEE8AAFF,
		PaleGreen            = 0x98FB98FF,
		PaleTurquoise        = 0xAFEEEEFF,
		PaleVioletRed        = 0xDB7093FF,
		PapayaWhip           = 0xFFEFD5FF,
		PeachPuff            = 0xFFDAB9FF,
		Peru                 = 0xCD853FFF,
		Pink                 = 0xFFC0CBFF,
		Plum                 = 0xDDA0DDFF,
		PowderBlue           = 0xB0E0E6FF,
		Purple               = 0x800080FF,
		RebeccaPurple        = 0x663399FF,
		Red                  = 0xFF0000FF,
		RosyBrown            = 0xBC8F8FFF,
		RoyalBlue            = 0x4169E1FF,
		SaddleBrown          = 0x8B4513FF,
		Salmon               = 0xFA8072FF,
		SandyBrown           = 0xF4A460FF,
		SeaGreen             = 0x2E8B57FF,
		SeaShell             = 0xFFF5EEFF,
		Sienna               = 0xA0522DFF,
		Silver               = 0xC0C0C0FF,
		SkyBlue              = 0x87CEEBFF,
		SlateBlue            = 0x6A5ACDFF,
		SlateGrey            = 0x708090FF,
		Snow                 = 0xFFFAFAFF,
		SpringGreen          = 0x00FF7FFF,
		SteelBlue            = 0x4682B4FF,
		Tan                  = 0xD2B48CFF,
		Teal                 = 0x008080FF,
		Thistle              = 0xD8BFD8FF,
		Tomato               = 0xFF6347FF,
		Turquoise            = 0x40E0D0FF,
		Violet               = 0xEE82EEFF,
		Wheat                = 0xF5DEB3FF,
		White                = 0xFFFFFFFF,
		WhiteSmoke           = 0xF5F5F5FF,
		Yellow               = 0xFFFF00FF,
		YellowGreen          = 0x9ACD32FF,

		Transparent			 = 0x00,
		Clear                = 0x01
	}

	/// <summary>
	/// Universal Color structure
	/// </summary>
	public struct Color : IEquatable<Color>
    {
		#region CTOR
		public Color (int r, int g, int b, int a) :
			this ((uint)r, (uint)g, (uint)b, (uint)a) { }

		public Color(uint r, uint g, uint b, uint a)
		{
			value =
				((r & 0xFF) << 24) +
				((g & 0xFF) << 16) +
				((b & 0xFF) << 8) +
				((a & 0xFF));
		}
		public Color (byte r, byte g, byte b, byte a)
		{
			value = ((uint)r << 24) + ((uint)g << 16) + ((uint)b << 8) + a;
		}
		public Color (double r, double g, double b, double a)
		{
			value =
				(((uint)Math.Round (r * 255.0)) << 24) +
				(((uint)Math.Round (g * 255.0)) << 16) +
				(((uint)Math.Round (b * 255.0)) << 8) +
				(((uint)Math.Round (a * 255.0)));
		}
		public Color (ReadOnlySpan<double> rgba) {
			value =
				(((uint)Math.Round (rgba[0] * 255.0)) << 24) +
				(((uint)Math.Round (rgba[1] * 255.0)) << 16) +
				(((uint)Math.Round (rgba[2] * 255.0)) << 8) +
				(((uint)Math.Round (rgba[3] * 255.0)));
		}
		public Color (UInt32 rgba)
		{
			this.value = rgba;
		}
		public Color (Colors color)
		{
			this.value = (UInt32)color;
		}
		#endregion
		//rgba
		UInt32 value;

		public uint R {
			get => (value & 0xFF000000) >> 24;
			set => this.value = (value & 0x000000FF) << 24;
		}
		public uint G {
			get => (value & 0x00FF0000) >> 16;
			set => this.value = (value & 0x000000FF) << 16;
		}
		public uint B {
			get => (value & 0x0000FF00) >> 8;
			set => this.value = (value & 0x000000FF) << 8;
		}
		public uint A {
			get => (value & 0x000000FF);
			set => this.value = (value & 0x000000FF);
		}




		/*public string Name;
		public string htmlCode;
		internal bool predefinied;*/

		#region Operators
		/*public static implicit operator string(Color c) => c.ToString();

		public static implicit operator UInt32 (Color c) => c.value;*/

		/*public static implicit operator Color(string s)
		{
		}*/

		public static implicit operator Color (Colors c) => new Color ((UInt32)c);
		public static implicit operator Colors (Color c) => (Colors)c.value;
		public static bool operator ==(Color a, Color b) => a.Equals (b);
		public static bool operator != (Color a, Color b) => !a.Equals (b);

		public static Color operator *(Color c, Double f) => new Color(c.R,c.G,c.B,c.A * f);
		public static Color operator +(Color c1, Color c2) => new Color(c1.R + c2.R,c1.G + c2.G,c1.B + c2.B,c1.A + c2.A);
		public static Color operator -(Color c1, Color c2) =>new Color(c1.R - c2.R,c1.G - c2.G,c1.B - c2.B,c1.A - c2.A);
		#endregion

		/// <summary>
		/// compute the hue of the color
		/// </summary>
		public uint Hue {
			get {
				double r = R / 255.0;
				double g = G / 255.0;
				double b = B / 255.0;
				double min = Math.Min (r, Math.Min (g, b));	//Min. value of RGB
				double max = Math.Max (r, Math.Max (g, b));	//Max. value of RGB
				double diff = max - min;							//Delta RGB value

				if ( diff == 0 )//This is a grey, no chroma...
					return 0;

				double h = 0.0, s = diff / max;

				double diffR = (((max - r) / 6.0) + (diff / 2.0)) / diff;
				double diffG = (((max - g) / 6.0) + (diff / 2.0)) / diff;
				double diffB = (((max - b) / 6.0) + (diff / 2.0)) / diff;

				if (r == max)
					h = diffB - diffG;
				else if (g == max)
					h = (1.0 / 3.0) + diffR - diffB;
				else if (b == max)
					h = (2.0 / 3.0) + diffG - diffR;

				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;

				return (uint)(h*255);
			}
		}
		/// <summary>
		/// compute the saturation of the color
		/// </summary>
		public uint Saturation {
			get {
				uint min = Math.Min (R, Math.Min (G, B)); //Min. value of RGB
				uint max = Math.Max (R, Math.Max (G, B)); //Max. value of RGB
				uint diff = max - min;                    //Delta RGB value
				return diff == 0 ? 0 : (uint)(255.0 * diff / max);
			}
		}
		/// <summary>
		/// compute the RGB intensity of the color
		/// </summary>
		/// <value>The value.</value>
		public uint Value => Math.Max (R, Math.Max (G, B));   //Max. value of RGB

		public string HtmlCode {
			get {
				string tmp = $"#{R:X2}{G:X2}{B:X2}";
				return A == 0xFF ? tmp : $"{tmp}{A:X2}";
			}
		}
		public float[] floatArray => new float[]{ R / 255f, G / 255f, B / 255f, A / 255f };
		/// <summary>
		/// return a copy of the color with the alpha component modified
		/// </summary>
		/// <returns>new modified color</returns>
		/// <param name="_A">normalized alpha component</param>
		public Color AdjustAlpha(double _A)
		{
			float[] tmp = floatArray;
			return new Color (tmp[0], tmp[1], tmp[2], _A);
		}

		public override bool Equals (object obj)
			=> obj is Color c && Equals(c);
		/*public bool Equals (Colors other)
			=> value == (UInt32)other;*/

		public bool Equals (Color other)
			=> value == other.value;		
		public override int GetHashCode ()
			=> value.GetHashCode ();

		public override string ToString()
			=> EnumsNET.Enums.IsValid<Colors> ((Colors)value) ? EnumsNET.Enums.GetName((Colors)value) : HtmlCode;
			
		public static Color FromIml (string iml)
		{
			Span<double> components = stackalloc double[4];
			components[3] = 1;//init alpha to 1 so that it can be ommitted
			ReadOnlySpan<char> c = iml.AsSpan ();			
			int i = 0;
			int ioc = c.IndexOf (',');

			while (ioc >= 0) {
				components[i++] = double.Parse (c.Slice (0, ioc));
				c = c.Slice (ioc + 1);
				ioc = c.IndexOf (',');
			}
			components[i++] = double.Parse (c);			
			return new Color (components);
		}

		public static Color Parse(string s)
			=> string.IsNullOrEmpty (s) ? new Color (Colors.White) :
				s[0] == '#' ? s.Length < 8 ?
				new Color (0xff + (UInt32.Parse (s.AsSpan ().Slice (1), System.Globalization.NumberStyles.HexNumber)<<8))
				: new Color (UInt32.Parse (s.AsSpan().Slice (1), System.Globalization.NumberStyles.HexNumber)) :
				char.IsDigit(s[0]) ? FromIml (s) :
				EnumsNET.Enums.TryParse<Colors> (s, out Colors cc) ? new Color(cc) :
				throw new Exception ("Unknown color name: " + s);

		public static Color FromHSV (double _h, double _v = 0xff, double _s = 0xff, double _alpha = 0xff) {
			_h /= 255.0;
			_v /= 255.0;
			_s /= 255.0;

			double H = _h * 360;
			double C = _v * _s;
			//X = C × (1 - | (H / 60°) mod 2 - 1 |)
			double X = C * (1 - Math.Abs ((H / 60.0) % 2 - 1));
			double m = _v - C;

			if (H >= 300)
				return new Color (C + m, m, X + m, _alpha / 255.0);
			else if (H >= 240)
				return new Color (X + m, m, C + m, _alpha / 255.0);
			else if (H >= 180)
				return new Color (m, X + m, C + m, _alpha / 255.0);
			else if (H >= 120)
				return new Color ( m, C + m, X + m, _alpha / 255.0);
			else if (H >= 60)
				return new Color (X + m, C + m,  m, _alpha / 255.0);
			return new Color (C + m, X + m,  m, _alpha / 255.0);
		}
	}
}
