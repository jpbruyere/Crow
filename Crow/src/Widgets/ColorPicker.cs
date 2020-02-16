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
		protected ColorPicker() : base(){}
		public ColorPicker (Interface iface) : base(iface){}
		#endregion

		Color currentColor;

		[DefaultValue("Black")]
		public virtual Color CurrentColor {
			get => currentColor;
			set {
				if (currentColor == value)
					return;
				currentColor = value;
				NotifyValueChanged ("CurrentColor", currentColor);
			}
		}

		public IList<Color> ColorList => Color.ColorDic.Values.OrderBy (c => c.Hue).ToList ();
	}
}

