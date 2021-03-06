﻿// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// templated color selector control
	/// </summary>
	public class ColorPicker2 : TemplatedControl
	{
		#region CTOR
		protected ColorPicker2() : base(){}
		public ColorPicker2 (Interface iface) : base(iface){}
		#endregion

		const double div = 255.0;
		const double colDiv = 1.0 / div;

		Color curColor;
		double h,s,v;

		
		public virtual int R {
			get { return curColor.R; }
			set {
				if (R == value)
					return;
				curColor.R = value * colDiv;
				NotifyValueChanged ("R", R);
				hsvFromRGB ();
				notifyCurColorHasChanged ();
			}
		}
		
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

		
		public virtual Fill SelectedColor {
			get { return new SolidColor(curColor); }
			set {
				if (value == null)
					curColor = Color.White;
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
			string tmp = ((int)Math.Round (R)).ToString ("X2") +
			             ((int)Math.Round (G)).ToString ("X2") +
			             ((int)Math.Round (B)).ToString ("X2");
			if (curColor.A < 1.0)
				tmp += ((int)Math.Round (A)).ToString ("X2");
			NotifyValueChanged ("HexColor", tmp);
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

