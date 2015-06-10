//
//  ListBox.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Collections;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	[DefaultTemplate("#go.Templates.Listbox.goml")]
	public class ListBox : TemplatedControl
	{
		Group _list;

		public ListBox () : base()
		{
		}

		#region implemented abstract members of TemplatedControl
		protected override void loadTemplate (GraphicObject template = null)
		{
			base.loadTemplate (template);
			_list = this.child.FindByName ("List") as Group;
		}
		#endregion

		ICollection data;
		int _selectedIndex;

		public int SelectedIndex{
			get { return _selectedIndex; }
			set { _selectedIndex = value; }
		}

		[XmlAttributeAttribute()][DefaultValue(null)]
		public ICollection Data {
			get {
				return data;
			}
			set {				
				data = value;

				_list.Children.Clear ();
				if (data == null)
					return;
				foreach (var item in data) {
					GraphicObject g = Interface.Load ("#go.Templates.ItemTemplate.goml", item);
//					g.Tag = item;
//					g.MouseClick += (object sender, OpenTK.Input.MouseButtonEventArgs e) => {
//						
//					};
					_list.addChild(g);

				}
			}
		}
	}
}

