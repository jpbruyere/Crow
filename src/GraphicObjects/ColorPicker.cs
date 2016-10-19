//
//  ColorPicker.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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

		Color selectedColor;
		float _red, _green, _blue, _alpha;

		[XmlAttributeAttribute()]
		public virtual double Red {
			get { return selectedColor.R; }
			set {
				if (selectedColor.R == value)
					return;
				selectedColor.R = value; 
				NotifyValueChanged ("Red", selectedColor.R);
				NotifyValueChanged ("SelectedColor", new SolidColor(selectedColor));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double Green {
			get { return selectedColor.G; }
			set {
				if (selectedColor.G == value)
					return;
				selectedColor.G = value;
				NotifyValueChanged ("Green", selectedColor.G);
				NotifyValueChanged ("SelectedColor", new SolidColor(selectedColor));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double Blue {
			get { return selectedColor.B; }
			set {
				if (selectedColor.B == value)
					return;
				selectedColor.B = value;
				NotifyValueChanged ("Blue", selectedColor.B);
				NotifyValueChanged ("SelectedColor", new SolidColor(selectedColor));
			}
		}
		[XmlAttributeAttribute()]
		public virtual double Alpha {
			get { return selectedColor.A; }
			set {
				if (selectedColor.A == value)
					return;
				selectedColor.A = value;
				NotifyValueChanged ("Alpha", selectedColor.A);
				NotifyValueChanged ("SelectedColor", new SolidColor(selectedColor));
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual SolidColor SelectedColor {
			get { return selectedColor; }
			set {				
				if (selectedColor.Equals(value))
					return;
				selectedColor = value; 
				NotifyValueChanged ("SelectedColor", new SolidColor(selectedColor));
				NotifyValueChanged ("Alpha", selectedColor.A);
				NotifyValueChanged ("Blue", selectedColor.B);
				NotifyValueChanged ("Green", selectedColor.G);
				NotifyValueChanged ("Red", selectedColor.R);
			}
		}
	}
}

