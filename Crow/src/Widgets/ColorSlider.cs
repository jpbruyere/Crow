// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

using Glfw;

using Drawing2D;

namespace Crow
{
	/// <summary>
	/// Color component slider with background gradient ranging from 0 to 1 for this component value.
	/// </summary>
	[DesignIgnore]
	public class ColorSlider : Slider
	{
		#region CTOR
		protected ColorSlider() {}
		public ColorSlider (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		Color currentColor = Colors.Black;
		ColorComponent component;

		public Color CurrentColor {
			get => currentColor;
			set {
				if (currentColor == value)
					return;

				currentColor = value;

				switch (component) {
				case ColorComponent.Red:
					Value = currentColor.R;
					break;
				case ColorComponent.Green:
					Value = currentColor.G;
					break;
				case ColorComponent.Blue:
					Value = currentColor.B;
					break;
				case ColorComponent.Alpha:
					Value = currentColor.A;
					break;
				case ColorComponent.Hue:
					Value = currentColor.Hue;
					break;
				case ColorComponent.Saturation:
					Value = currentColor.Saturation;
					break;
				case ColorComponent.Value:
					Value = currentColor.Value;
					break;
				}				

				NotifyValueChangedAuto (currentColor);
				RegisterForRedraw ();
			}
		}
		[DefaultValue (ColorComponent.Value)]
		public ColorComponent Component {
			get => component;
			set {
				if (component == value)
					return;
				component = value;
				NotifyValueChangedAuto (component);
				RegisterForRedraw ();
			}
		}		
	}
}

