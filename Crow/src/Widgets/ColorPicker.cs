// Copyright (c) 2013-2025  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Drawing2D;

namespace Crow
{
	/// <summary>
	/// templated color selector control
	/// </summary>
	public class ColorPicker : TemplatedControl
	{
		#region CTOR
		protected ColorPicker() {}
		public ColorPicker (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		Color currentColor;

		[DefaultValue("Black")]
		public virtual Color CurrentColor {
			get => currentColor;
			set {
				if (currentColor.Equals(value))
					return;
				//Color prev = currentColor;
				currentColor = value;
				NotifyValueChangedAuto (currentColor);
				
			}
		}
		public int Red {
			get => (int)currentColor.R;
			set {
				if ((uint)value == currentColor.R)
					return;
				CurrentColor = new Color((uint)value, currentColor.G, currentColor.B, currentColor.A);
				NotifyValueChangedAuto(currentColor.R);
			}
		}
		public int Green {
			get => (int)currentColor.G;
			set {
				if ((uint)value == currentColor.G)
					return;
				CurrentColor = new Color(currentColor.R, (uint)value, currentColor.B, currentColor.A);
				NotifyValueChangedAuto(currentColor.G);
			}
		}		
		public int Blue {
			get => (int)currentColor.B;
			set {
				if ((uint)value == currentColor.B)
					return;
				CurrentColor = new Color(currentColor.R, currentColor.G, (uint)value, currentColor.A);
				NotifyValueChangedAuto(currentColor.B);
			}
		}
		public int Alpha {
			get => (int)currentColor.A;
			set {
				if ((uint)value == currentColor.A)
					return;
				CurrentColor = new Color(currentColor.R, currentColor.G, currentColor.B, (uint)value);
				NotifyValueChangedAuto(currentColor.A);
			}
		}
		public int Hue {
			get => (int)currentColor.B;
			set {
				if ((uint)value == currentColor.Hue)
					return;
				Color c = Color.FromHSV ((uint)value, currentColor.Value, currentColor.Saturation, currentColor.A);
				for (int i = 0; i < 2; i++) {
					c = Color.FromHSV ((uint)value, c.Value, c.Saturation, c.A);
				}
				CurrentColor = Color.FromHSV ((uint)value, c.Value, c.Saturation, c.A);
				NotifyValueChangedAuto(currentColor.Hue);
			}
		}
		public int Saturation {
			get => (int)currentColor.B;
			set {
				if ((uint)value == currentColor.Saturation)
					return;
				CurrentColor = Color.FromHSV (currentColor.Hue, currentColor.Value, (uint)value, currentColor.A);
				NotifyValueChangedAuto(currentColor.Saturation);
			}
		}
		public int Value {
			get => (int)currentColor.B;
			set {
				if ((uint)value == currentColor.Value)
					return;
				CurrentColor = Color.FromHSV (currentColor.Hue, (uint)value, currentColor.Saturation, currentColor.A);												
				NotifyValueChangedAuto(currentColor.Value);
			}
		}
			

		public IList<Colors> AvailableColors => //Enum.GetValues (typeof (Color)).ToList<Color> ();// Colors. ColorDic.Values.OrderBy (c => c.Hue).ToList ();
			EnumsNET.Enums.GetValues<Colors> ().ToList<Colors> ();
			//EnumsNET.Enums.GetValues<Colors> ().OrderBy(c=>(((Color)c).Value << 8) + (((Color)c).Hue << 16) + (((Color)c).Saturation)).ToList<Colors> ();

		public void onSelectedItemChanged(object sender, SelectionChangeEventArgs e) {
			CurrentColor = (Color)(Colors)e.NewValue;
		}
    }
}

