//
// Colors.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;



namespace Crow
{
	/// <summary>
	/// Universal Color structure
	/// </summary>
	public struct Color
    {
		internal static Type TColor = typeof(Color);

		#region CTOR
		public Color(double _R, double _G, double _B, double _A)
		{
			A = _A.Clamp(0,1);
			R = _R.Clamp(0,1);
			G = _G.Clamp(0,1);
			B = _B.Clamp(0,1);
			predefinied = false;
			htmlCode = "";
			Name = "";
		}
		internal Color(double _R, double _G, double _B, double _A, string _name)
		{
			A = _A;
			R = _R;
			G = _G;
			B = _B;
			predefinied = false;
			Name = _name;
			htmlCode = "";
			ColorDic.Add(_name,this);
		}
		internal Color(int _R, int _G, int _B, string _name, string _code)
		{
			A = 1.0;
			R = _R / 255.0;
			G = _G / 255.0;
			B = _B / 255.0;
			Name = _name;
			htmlCode = _code;
			predefinied = true;
			ColorDic.Add(_name,this);
		}
		#endregion

		/// <summary>
		/// color names dictionary
		/// </summary>
		public static Dictionary<string, Color> ColorDic = new Dictionary<string, Color>();

		public string Name;
		public string htmlCode;
		internal bool predefinied;

		#region public fields
        public double A;
        public double R;
        public double G;
		public double B;
		#endregion

		#region Operators
        public static implicit operator string(Color c)
        {
            return c.ToString();
        }
		public static implicit operator Color(string s)
		{
			if (string.IsNullOrEmpty(s))
				return White;
			Color cc = default(Color);
			if (s.StartsWith ("#")) {
				cc.R = int.Parse (s.Substring (1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
				cc.G = int.Parse (s.Substring (3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
				cc.B = int.Parse (s.Substring (5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
				if (s.Length > 7)
					cc.A = int.Parse (s.Substring (7, 2), System.Globalization.NumberStyles.HexNumber) / 255.0;
				return cc;
			}

			if (char.IsLetter(s[0])){
				if (ColorDic.ContainsKey (s))
					return ColorDic[s];
				throw new Exception ("Unknown color name: " + s);
			}

			string[] c = s.Split(new char[] { ',' });
							
			return new Color(
				double.Parse(c[0]),
				double.Parse(c[1]),
				double.Parse(c[2]),
				double.Parse(c[3]));
		}

//		public static implicit operator OpenTK.Vector4(Color c)
//		{
//			return new OpenTK.Vector4 ((float)c.R, (float)c.G, (float)c.B, (float)c.A);
//		}
//		public static implicit operator Color(OpenTK.Vector4 v)
//		{
//			return new Color (v.X, v.Y, v.Z, v.W);
//		}


		public static bool operator ==(Color left, Color right)
		{
			if (left.predefinied & right.predefinied)
				return left.htmlCode == right.htmlCode;
			return left.A != right.A ? false :
				left.R != right.R ? false :
				left.G != right.G ? false :
				left.B != right.B ? false : true;
		}
		public static bool operator !=(Color left, Color right)
		{
			if (left.predefinied & right.predefinied)
				return left.htmlCode != right.htmlCode;
			return left.A == right.A ? false :
				left.R == right.R ? false :
				left.G == right.G ? false :
				left.B == right.B ? false : true;

		}
		public static bool operator ==(Color c, string n)
		{
			return n.StartsWith("#") ?
				string.Equals(c.HtmlCode, n, StringComparison.Ordinal) :
				string.Equals(c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator !=(Color c, string n)
		{
			return n.StartsWith("#") ?
				!string.Equals(c.HtmlCode, n, StringComparison.Ordinal) :
				!string.Equals(c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator ==(string n, Color c)
		{
			return string.Equals (c.Name, n, StringComparison.Ordinal);
		}
		public static bool operator !=(string n, Color c)
		{
			return !string.Equals (c.Name, n, StringComparison.Ordinal);
		}
		public static Color operator *(Color c, Double f)
		{
			return new Color(c.R,c.G,c.B,c.A * f);
		}
		public static Color operator +(Color c1, Color c2)
		{
			return new Color(c1.R + c2.R,c1.G + c2.G,c1.B + c2.B,c1.A + c2.A);
		}
		public static Color operator -(Color c1, Color c2)
		{
			return new Color(c1.R - c2.R,c1.G - c2.G,c1.B - c2.B,c1.A - c2.A);
		}
		#endregion

		/// <summary>
		/// compute the hue of the color
		/// </summary>
		public double Hue {
			get {
				double min = Math.Min (R, Math.Min (G, B));	//Min. value of RGB
				double max = Math.Max (R, Math.Max (G, B));	//Max. value of RGB
				double diff = max - min;							//Delta RGB value

				if ( diff == 0 )//This is a gray, no chroma...
					return 0;

				double h = 0.0, s = diff / max;

				double diffR = (((max - R) / 6.0) + (diff / 2.0)) / diff;
				double diffG = (((max - G) / 6.0) + (diff / 2.0)) / diff;
				double diffB = (((max - B) / 6.0) + (diff / 2.0)) / diff;

				if (R == max)
					h = diffB - diffG;
				else if (G == max)
					h = (1.0 / 3.0) + diffR - diffB;
				else if (B == max)
					h = (2.0 / 3.0) + diffG - diffR;

				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;

				return h;
			}
		}
		/// <summary>
		/// compute the saturation of the color
		/// </summary>
		public double Saturation {
			get {
				return Math.Max (R, Math.Max (G, B));	//Max. value of RGB
			}
		}
		/// <summary>
		/// compute the RGB intensity of the color
		/// </summary>
		/// <value>The value.</value>
		public double Value {
			get {
				double min = Math.Min (R, Math.Min (G, B));	//Min. value of RGB
				double max = Math.Max (R, Math.Max (G, B));	//Max. value of RGB
				double diff = max - min;							//Delta RGB value
				return diff == 0 ? 0 : diff / max;
			}
		}
		public string HtmlCode {
			get { 
				string tmp = "#" +
					((int)Math.Round (R * 255.0)).ToString ("X2") +
					((int)Math.Round (G * 255.0)).ToString ("X2") +
					((int)Math.Round (B * 255.0)).ToString ("X2");
				if (A < 1.0)
					tmp += ((int)Math.Round(A * 255.0)).ToString ("X2");
				return tmp;
			}
		}
		public float[] floatArray
		{
			get { return new float[]{ (float)R, (float)G, (float)B, (float)A }; }
		}
		/// <summary>
		/// return a copy of the color with the alpha component modified
		/// </summary>
		/// <returns>new modified color</returns>
		/// <param name="_A">normalized alpha component</param>
		public Color AdjustAlpha(double _A)
		{
			return new Color (this.R, this.G, this.B, _A);
		}
		public void ResetName(){
			Name = "";
			htmlCode = "";
		}

		#region Predefined colors
        public static readonly Color Transparent = new Color(0, 0, 0, 0, "Transparent");
		public static readonly Color Clear = new Color(-1, -1, -1, -1, "Clear");
		public static readonly Color AliceBlue = new Color(240,248,255,"AliceBlue","#F0F8FF");
		public static readonly Color AntiqueWhite = new Color(250,235,215,"AntiqueWhite","#FAEBD7");
		public static readonly Color Aqua = new Color(0,255,255,"Aqua","#00FFFF");
		public static readonly Color Aquamarine = new Color(127,255,212,"Aquamarine","#7FFFD4");
		public static readonly Color Azure = new Color(240,255,255,"Azure","#F0FFFF");
		public static readonly Color Beige = new Color(245,245,220,"Beige","#F5F5DC");
		public static readonly Color Bisque = new Color(255,228,196,"Bisque","#FFE4C4");
		public static readonly Color Black = new Color(0,0,0,"Black","#000000");
		public static readonly Color BlanchedAlmond = new Color(255,235,205,"BlanchedAlmond","#FFEBCD");
		public static readonly Color Blue = new Color(0,0,255,"Blue","#0000FF");
		public static readonly Color BlueViolet = new Color(138,43,226,"BlueViolet","#8A2BE2");
		public static readonly Color Brown = new Color(165,42,42,"Brown","#A52A2A");
		public static readonly Color BurlyWood = new Color(222,184,135,"BurlyWood","#DEB887");
		public static readonly Color CadetBlue = new Color(95,158,160,"CadetBlue","#5F9EA0");
		public static readonly Color Chartreuse = new Color(127,255,0,"Chartreuse","#7FFF00");
		public static readonly Color Chocolate = new Color(210,105,30,"Chocolate","#D2691E");
		public static readonly Color Coral = new Color(255,127,80,"Coral","#FF7F50");
		public static readonly Color CornflowerBlue = new Color(100,149,237,"CornflowerBlue","#6495ED");
		public static readonly Color Cornsilk = new Color(255,248,220,"Cornsilk","#FFF8DC");
		public static readonly Color Crimson = new Color(220,20,60,"Crimson","#DC143C");
		public static readonly Color Cyan = new Color(0,255,255,"Cyan","#00FFFF");
		public static readonly Color DarkBlue = new Color(0,0,139,"DarkBlue","#00008B");
		public static readonly Color DarkCyan = new Color(0,139,139,"DarkCyan","#008B8B");
		public static readonly Color DarkGoldenRod = new Color(184,134,11,"DarkGoldenRod","#B8860B");
		public static readonly Color DarkGray = new Color(169,169,169,"DarkGray","#A9A9A9");
		public static readonly Color DarkGrey = new Color(169,169,169,"DarkGrey","#A9A9A9");
		public static readonly Color DarkGreen = new Color(0,100,0,"DarkGreen","#006400");
		public static readonly Color DarkKhaki = new Color(189,183,107,"DarkKhaki","#BDB76B");
		public static readonly Color DarkMagenta = new Color(139,0,139,"DarkMagenta","#8B008B");
		public static readonly Color DarkOliveGreen = new Color(85,107,47,"DarkOliveGreen","#556B2F");
		public static readonly Color DarkOrange = new Color(255,140,0,"DarkOrange","#FF8C00");
		public static readonly Color DarkOrchid = new Color(153,50,204,"DarkOrchid","#9932CC");
		public static readonly Color DarkRed = new Color(139,0,0,"DarkRed","#8B0000");
		public static readonly Color DarkSalmon = new Color(233,150,122,"DarkSalmon","#E9967A");
		public static readonly Color DarkSeaGreen = new Color(143,188,143,"DarkSeaGreen","#8FBC8F");
		public static readonly Color DarkSlateBlue = new Color(72,61,139,"DarkSlateBlue","#483D8B");
		public static readonly Color DarkSlateGray = new Color(47,79,79,"DarkSlateGray","#2F4F4F");
		public static readonly Color DarkSlateGrey = new Color(47,79,79,"DarkSlateGrey","#2F4F4F");
		public static readonly Color DarkTurquoise = new Color(0,206,209,"DarkTurquoise","#00CED1");
		public static readonly Color DarkViolet = new Color(148,0,211,"DarkViolet","#9400D3");
		public static readonly Color DeepPink = new Color(255,20,147,"DeepPink","#FF1493");
		public static readonly Color DeepSkyBlue = new Color(0,191,255,"DeepSkyBlue","#00BFFF");
		public static readonly Color DimGray = new Color(105,105,105,"DimGray","#696969");
		public static readonly Color DimGrey = new Color(105,105,105,"DimGrey","#696969");
		public static readonly Color DodgerBlue = new Color(30,144,255,"DodgerBlue","#1E90FF");
		public static readonly Color FireBrick = new Color(178,34,34,"FireBrick","#B22222");
		public static readonly Color FloralWhite = new Color(255,250,240,"FloralWhite","#FFFAF0");
		public static readonly Color ForestGreen = new Color(34,139,34,"ForestGreen","#228B22");
		public static readonly Color Fuchsia = new Color(255,0,255,"Fuchsia","#FF00FF");
		public static readonly Color Gainsboro = new Color(220,220,220,"Gainsboro","#DCDCDC");
		public static readonly Color GhostWhite = new Color(248,248,255,"GhostWhite","#F8F8FF");
		public static readonly Color Gold = new Color(255,215,0,"Gold","#FFD700");
		public static readonly Color GoldenRod = new Color(218,165,32,"GoldenRod","#DAA520");
		public static readonly Color Gray = new Color(128,128,128,"Gray","#808080");
		public static readonly Color Grey = new Color(128,128,128,"Grey","#808080");
		public static readonly Color Green = new Color(0,128,0,"Green","#008000");
		public static readonly Color GreenYellow = new Color(173,255,47,"GreenYellow","#ADFF2F");
		public static readonly Color HoneyDew = new Color(240,255,240,"HoneyDew","#F0FFF0");
		public static readonly Color HotPink = new Color(255,105,180,"HotPink","#FF69B4");
		public static readonly Color IndianRed = new Color(205,92,92,"IndianRed","#CD5C5C");
		public static readonly Color Indigo = new Color(75,0,130,"Indigo","#4B0082");
		public static readonly Color Ivory = new Color(255,255,240,"Ivory","#FFFFF0");
		public static readonly Color Jet = new Color(52,52,52,"Jet","#343434");
		public static readonly Color Khaki = new Color(240,230,140,"Khaki","#F0E68C");
		public static readonly Color Lavender = new Color(230,230,250,"Lavender","#E6E6FA");
		public static readonly Color LavenderBlush = new Color(255,240,245,"LavenderBlush","#FFF0F5");
		public static readonly Color LawnGreen = new Color(124,252,0,"LawnGreen","#7CFC00");
		public static readonly Color LemonChiffon = new Color(255,250,205,"LemonChiffon","#FFFACD");
		public static readonly Color LightBlue = new Color(173,216,230,"LightBlue","#ADD8E6");
		public static readonly Color LightCoral = new Color(240,128,128,"LightCoral","#F08080");
		public static readonly Color LightCyan = new Color(224,255,255,"LightCyan","#E0FFFF");
		public static readonly Color LightGoldenRodYellow = new Color(250,250,210,"LightGoldenRodYellow","#FAFAD2");
		public static readonly Color LightGray = new Color(211,211,211,"LightGray","#D3D3D3");
		public static readonly Color LightGrey = new Color(211,211,211,"LightGrey","#D3D3D3");
		public static readonly Color LightGreen = new Color(144,238,144,"LightGreen","#90EE90");
		public static readonly Color LightPink = new Color(255,182,193,"LightPink","#FFB6C1");
		public static readonly Color LightSalmon = new Color(255,160,122,"LightSalmon","#FFA07A");
		public static readonly Color LightSeaGreen = new Color(32,178,170,"LightSeaGreen","#20B2AA");
		public static readonly Color LightSkyBlue = new Color(135,206,250,"LightSkyBlue","#87CEFA");
		public static readonly Color LightSlateGray = new Color(119,136,153,"LightSlateGray","#778899");
		public static readonly Color LightSlateGrey = new Color(119,136,153,"LightSlateGrey","#778899");
		public static readonly Color LightSteelBlue = new Color(176,196,222,"LightSteelBlue","#B0C4DE");
		public static readonly Color LightYellow = new Color(255,255,224,"LightYellow","#FFFFE0");
		public static readonly Color Lime = new Color(0,255,0,"Lime","#00FF00");
		public static readonly Color LimeGreen = new Color(50,205,50,"LimeGreen","#32CD32");
		public static readonly Color Linen = new Color(250,240,230,"Linen","#FAF0E6");
		public static readonly Color Magenta = new Color(255,0,255,"Magenta","#FF00FF");
		public static readonly Color Maroon = new Color(128,0,0,"Maroon","#800000");
		public static readonly Color MediumAquaMarine = new Color(102,205,170,"MediumAquaMarine","#66CDAA");
		public static readonly Color MediumBlue = new Color(0,0,205,"MediumBlue","#0000CD");
		public static readonly Color MediumOrchid = new Color(186,85,211,"MediumOrchid","#BA55D3");
		public static readonly Color MediumPurple = new Color(147,112,219,"MediumPurple","#9370DB");
		public static readonly Color MediumSeaGreen = new Color(60,179,113,"MediumSeaGreen","#3CB371");
		public static readonly Color MediumSlateBlue = new Color(123,104,238,"MediumSlateBlue","#7B68EE");
		public static readonly Color MediumSpringGreen = new Color(0,250,154,"MediumSpringGreen","#00FA9A");
		public static readonly Color MediumTurquoise = new Color(72,209,204,"MediumTurquoise","#48D1CC");
		public static readonly Color MediumVioletRed = new Color(199,21,133,"MediumVioletRed","#C71585");
		public static readonly Color MidnightBlue = new Color(25,25,112,"MidnightBlue","#191970");
		public static readonly Color MintCream = new Color(245,255,250,"MintCream","#F5FFFA");
		public static readonly Color MistyRose = new Color(255,228,225,"MistyRose","#FFE4E1");
		public static readonly Color Moccasin = new Color(255,228,181,"Moccasin","#FFE4B5");
		public static readonly Color NavajoWhite = new Color(255,222,173,"NavajoWhite","#FFDEAD");
		public static readonly Color Navy = new Color(0,0,128,"Navy","#000080");
		public static readonly Color OldLace = new Color(253,245,230,"OldLace","#FDF5E6");
		public static readonly Color Olive = new Color(128,128,0,"Olive","#808000");
		public static readonly Color OliveDrab = new Color(107,142,35,"OliveDrab","#6B8E23");
		public static readonly Color Onyx = new Color(53,56,57,"Onyx","#353839");
		public static readonly Color Orange = new Color(255,165,0,"Orange","#FFA500");
		public static readonly Color OrangeRed = new Color(255,69,0,"OrangeRed","#FF4500");
		public static readonly Color Orchid = new Color(218,112,214,"Orchid","#DA70D6");
		public static readonly Color PaleGoldenRod = new Color(238,232,170,"PaleGoldenRod","#EEE8AA");
		public static readonly Color PaleGreen = new Color(152,251,152,"PaleGreen","#98FB98");
		public static readonly Color PaleTurquoise = new Color(175,238,238,"PaleTurquoise","#AFEEEE");
		public static readonly Color PaleVioletRed = new Color(219,112,147,"PaleVioletRed","#DB7093");
		public static readonly Color PapayaWhip = new Color(255,239,213,"PapayaWhip","#FFEFD5");
		public static readonly Color PeachPuff = new Color(255,218,185,"PeachPuff","#FFDAB9");
		public static readonly Color Peru = new Color(205,133,63,"Peru","#CD853F");
		public static readonly Color Pink = new Color(255,192,203,"Pink","#FFC0CB");
		public static readonly Color Plum = new Color(221,160,221,"Plum","#DDA0DD");
		public static readonly Color PowderBlue = new Color(176,224,230,"PowderBlue","#B0E0E6");
		public static readonly Color Purple = new Color(128,0,128,"Purple","#800080");
		public static readonly Color RebeccaPurple = new Color(102,51,153,"RebeccaPurple","#663399");
		public static readonly Color Red = new Color(255,0,0,"Red","#FF0000");
		public static readonly Color RosyBrown = new Color(188,143,143,"RosyBrown","#BC8F8F");
		public static readonly Color RoyalBlue = new Color(65,105,225,"RoyalBlue","#4169E1");
		public static readonly Color SaddleBrown = new Color(139,69,19,"SaddleBrown","#8B4513");
		public static readonly Color Salmon = new Color(250,128,114,"Salmon","#FA8072");
		public static readonly Color SandyBrown = new Color(244,164,96,"SandyBrown","#F4A460");
		public static readonly Color SeaGreen = new Color(46,139,87,"SeaGreen","#2E8B57");
		public static readonly Color SeaShell = new Color(255,245,238,"SeaShell","#FFF5EE");
		public static readonly Color Sienna = new Color(160,82,45,"Sienna","#A0522D");
		public static readonly Color Silver = new Color(192,192,192,"Silver","#C0C0C0");
		public static readonly Color SkyBlue = new Color(135,206,235,"SkyBlue","#87CEEB");
		public static readonly Color SlateBlue = new Color(106,90,205,"SlateBlue","#6A5ACD");
		public static readonly Color SlateGray = new Color(112,128,144,"SlateGray","#708090");
		public static readonly Color SlateGrey = new Color(112,128,144,"SlateGrey","#708090");
		public static readonly Color Snow = new Color(255,250,250,"Snow","#FFFAFA");
		public static readonly Color SpringGreen = new Color(0,255,127,"SpringGreen","#00FF7F");
		public static readonly Color SteelBlue = new Color(70,130,180,"SteelBlue","#4682B4");
		public static readonly Color Tan = new Color(210,180,140,"Tan","#D2B48C");
		public static readonly Color Teal = new Color(0,128,128,"Teal","#008080");
		public static readonly Color Thistle = new Color(216,191,216,"Thistle","#D8BFD8");
		public static readonly Color Tomato = new Color(255,99,71,"Tomato","#FF6347");
		public static readonly Color Turquoise = new Color(64,224,208,"Turquoise","#40E0D0");
		public static readonly Color Violet = new Color(238,130,238,"Violet","#EE82EE");
		public static readonly Color Wheat = new Color(245,222,179,"Wheat","#F5DEB3");
		public static readonly Color White = new Color(255,255,255,"White","#FFFFFF");
		public static readonly Color WhiteSmoke = new Color(245,245,245,"WhiteSmoke","#F5F5F5");
		public static readonly Color Yellow = new Color(255,255,0,"Yellow","#FFFF00");
		public static readonly Color YellowGreen = new Color(154,205,50,"YellowGreen","#9ACD32");

		#endregion

		public override int GetHashCode ()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + A.GetHashCode();
				hash = hash * 23 + R.GetHashCode();
				hash = hash * 23 + G.GetHashCode();
				hash = hash * 23 + B.GetHashCode();
				return hash;
			}
		}
		public override bool Equals (object obj)
		{
			return (obj == null || obj.GetType() != TColor) ?
				false :
				this == (Color)obj;
		}
		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Name))
				return Name;
			if (!string.IsNullOrEmpty (htmlCode))
				return htmlCode;
			string hc = HtmlCode;
			Color pc = ColorDic.Values.FirstOrDefault (c => c.htmlCode == hc);
			return pc.predefinied ? pc.Name : hc;
		}

        public static object Parse(string s)
        {
            return (Color)s;
        }
		public static Color FromHSV(double _h, double _v = 1.0, double _s = 1.0, double _alpha = 1.0){
			Color c = Color.Black;
			c.ResetName ();
			c.A = _alpha;
			if (_s == 0) {//HSV from 0 to 1
				c.R = _v;
				c.G = _v;
				c.B = _v;
			}else{
				double var_h = _h * 6.0;

				if (var_h == 6.0)
					var_h = 0;	//H must be < 1
				double var_i = Math.Floor( var_h );	//Or ... var_i = floor( var_h )
				double var_1 = _v * ( 1.0 - _s );
				double var_2 = _v * (1.0 - _s * (var_h - var_i));
				double var_3 = _v * (1.0 - _s * (1.0 - (var_h - var_i)));

				if (var_i == 0.0) {
					c.R = _v;
					c.G = var_3;
					c.B = var_1;
				}else if ( var_i == 1.0 ) { c.R = var_2 ; c.G = _v     ; c.B = var_1; }
				else if ( var_i == 2 ) { c.R = var_1 ; c.G = _v     ; c.B = var_3; }
				else if ( var_i == 3 ) { c.R = var_1 ; c.G = var_2 ; c.B = _v;     }
				else if ( var_i == 4 ) { c.R = var_3 ; c.G = var_1 ; c.B = _v;    }
				else                   { c.R = _v     ; c.G = var_1 ; c.B = var_2; }
			}
			return c;
		}
    }
}
