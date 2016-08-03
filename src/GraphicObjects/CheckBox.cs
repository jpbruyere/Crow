//
//  CheckBox.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.CheckBox.goml")]
	public class CheckBox : TemplatedControl
	{
		string caption;
		string image;
		bool isChecked;

		#region CTOR
		public CheckBox() : base()
		{}							
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		[XmlAttributeAttribute()][DefaultValue("Checkbox")]
		public string Caption {
			get { return caption; } 
			set {
				if (caption == value)
					return;
				caption = value; 
				NotifyValueChanged ("Caption", caption);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool IsChecked
		{
			get { return isChecked; }
			set
			{
				if (isChecked == value)
					return;
				
				isChecked = value;

				NotifyValueChanged ("IsChecked", value);

				if (isChecked)
					Checked.Raise (this, null);
				else
					Unchecked.Raise (this, null);
			}
		}

		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			base.onMouseClick (sender, e);
		}
	}
}
