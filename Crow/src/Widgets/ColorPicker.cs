// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

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
				currentColor = value;
				NotifyValueChangedAuto (currentColor);
				NotifyValueChanged ("CurrentColor2", Color.FromHSV (currentColor.Hue, currentColor.Value, currentColor.Saturation, currentColor.A));
			}
		}
		public IList<Colors> AvailableColors => //Enum.GetValues (typeof (Color)).ToList<Color> ();// Colors. ColorDic.Values.OrderBy (c => c.Hue).ToList ();
			EnumsNET.Enums.GetValues<Colors> ().ToList<Colors> ();

		public void onSelectedItemChanged(object sender, SelectionChangeEventArgs e) {
			CurrentColor = (Color)(Colors)e.NewValue;
		}
	}
}

