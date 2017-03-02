//
// ColorPicker.cs
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
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public class ColorPicker : TemplatedControl
	{
		public ColorPicker () : base ()
		{
		}

		const double div = 255.0;
		const double colDiv = 1.0 / div;

		Color curColor;
		double h,s,v;

		[XmlAttributeAttribute()]
		public virtual double R {
			get { return Math.Round(curColor.R * div); }
			set {
				if (R == value)
					return;
				curColor.R = value * colDiv;
				NotifyValueChanged ("R", R);
				hsvFromRGB ();
				notifyCurColorHasChanged ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double G {
			get { return Math.Round(curColor.G * div); }
			set {
				if (G == value)
					return;
				curColor.G = value * colDiv;
				NotifyValueChanged ("G", G);
				notifyCurColorHasChanged ();
				hsvFromRGB ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double B {
			get { return Math.Round(curColor.B * div); }
			set {
				if (B == value)
					return;
				curColor.B = value * colDiv;
				NotifyValueChanged ("B", B);
				notifyCurColorHasChanged ();
				hsvFromRGB ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double A {
			get { return Math.Round(curColor.A * div); }
			set {
				if (A == value)
					return;
				curColor.A = value * colDiv;
				NotifyValueChanged ("A", A);
				notifyCurColorHasChanged ();
				hsvFromRGB ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double H {
			get { return Math.Round (h, 3); }
			set {
				if (H == value)
					return;
				h = value;
				NotifyValueChanged ("H", H);
				rgbFromHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double S {
			get { return Math.Round (s, 2); }
			set {
				if (s == value)
					return;
				s = value;
				NotifyValueChanged ("S", S);
				rgbFromHSV ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual double V {
			get { return Math.Round (v, 2); }
			set {
				if (v == value)
					return;
				v = value;
				NotifyValueChanged ("V", V);
				rgbFromHSV ();
			}
		}

		[XmlAttributeAttribute]
		public virtual Fill SelectedColor {
			get { return new SolidColor(curColor); }
			set {
				if (value == null)
					curColor = default(Color);
				else if (value is SolidColor) {
					Color c = (value as SolidColor).color;
					if (curColor == c)
						return;
					curColor = c;
				}
				notifyCurColorHasChanged ();
				notifyRGBAHasChanged ();
				hsvFromRGB ();
			}
		}
		[XmlAttributeAttribute]
		public virtual Color SelectedRawColor {
			get { return curColor; }
			set {
				if (curColor == value)
					return;
				curColor = value;
				notifyCurColorHasChanged ();
				notifyRGBAHasChanged ();
				hsvFromRGB ();
			}
		}
		void notifyCurColorHasChanged(){
			NotifyValueChanged ("SelectedColor", SelectedColor);
			NotifyValueChanged ("SelectedRawColor", curColor);
			string n = curColor.ToString ();
			if (char.IsLetter(n[0]))
				NotifyValueChanged ("SelectedColorName", n);
			else
				NotifyValueChanged ("SelectedColorName", "-");
			NotifyValueChanged ("HexColor", ((int)R).ToString ("X2") + ((int)G).ToString ("X2") + ((int)B).ToString ("X2") + ((int)A).ToString ("X2"));
		}
		void notifyRGBAHasChanged(){
			NotifyValueChanged ("R", R);
			NotifyValueChanged ("G", G);
			NotifyValueChanged ("B", B);
			NotifyValueChanged ("A", A);
		}
		void notifyHSVHasChanged(){
			NotifyValueChanged ("H", H);
			NotifyValueChanged ("S", S);
			NotifyValueChanged ("V", V);
		}
		void hsvFromRGB(){
			Color c = curColor;
			c.ResetName ();
			double min = Math.Min (c.R, Math.Min (c.G, c.B));	//Min. value of RGB
			double max = Math.Max (c.R, Math.Max (c.G, c.B));	//Max. value of RGB
			double diff = max - min;							//Delta RGB value

			v = max;

			if ( diff == 0 )//This is a gray, no chroma...
			{
				h = 0;
				s = 0;
			}else{//Chromatic data...
				s = diff / max;

				double diffR = (((max - c.R) / 6.0) + (diff / 2.0)) / diff;
				double diffG = (((max - c.G) / 6.0) + (diff / 2.0)) / diff;
				double diffB = (((max - c.B) / 6.0) + (diff / 2.0)) / diff;

				if (c.R == max)
					h = diffB - diffG;
				else if (c.G == max)
					h = (1.0 / 3.0) + diffR - diffB;
				else if (c.B == max)
					h = (2.0 / 3.0) + diffG - diffR;

				if (h < 0)
					h += 1;
				if (h > 1)
					h -= 1;

			}
			notifyHSVHasChanged ();
		}
		void rgbFromHSV(){
			curColor = Color.FromHSV (h, v, s, curColor.A);
			notifyCurColorHasChanged ();
			notifyRGBAHasChanged ();
		}
	}
}

