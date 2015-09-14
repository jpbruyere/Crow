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
//TODO: implement ItemTemplate node in xml
using System.IO;
using System.Diagnostics;


namespace go
{
	[DefaultTemplate("#go.Templates.Listbox.goml")]
	//[DefaultTemplate("#go.Templates.ItemTemplate.goml")]
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

		IList data;
		int _selectedIndex;
		string _itemTemplate;

		[XmlAttributeAttribute][DefaultValue("#go.Templates.ItemTemplate.goml")]
		public string ItemTemplate {
			get { return _itemTemplate; }
			set { _itemTemplate = value; }
		}
		public int SelectedIndex{
			get { return _selectedIndex; }
			set { _selectedIndex = value; }
		}
		public object SelectedItem{
			get { return data[_selectedIndex]; }
		}
		[XmlAttributeAttribute][DefaultValue(null)]
		public IList Data {
			get {
				return data;
			}
			set {				
				data = value;

				foreach (GraphicObject c in _list.Children) {
					c.ClearBinding ();
				}
				_list.Children.Clear ();
				_list.registerForGraphicUpdate ();
				if (data == null)
					return;

				#if DEBUG
				Stopwatch loadingTime = new Stopwatch ();
				loadingTime.Start ();
				#endif

				MemoryStream ms = new MemoryStream ();
				using (Stream stream = Interface.GetStreamFromPath (ItemTemplate)) {
					
					stream.CopyTo (ms);
				}
					
				Type t = Interface.GetTopContainerOfGOMLStream (ms);

				foreach (var item in data) {
					ms.Seek(0,SeekOrigin.Begin);
					GraphicObject g = Interface.Load (ms, t, item);
					g.MouseClick += itemClick;
					_list.addChild(g);

				}
				ms.Dispose ();

				#if DEBUG
				loadingTime.Stop ();
				Debug.WriteLine("Listbox Loading: {0} ticks \t, {1} ms",
					loadingTime.ElapsedTicks,
					loadingTime.ElapsedMilliseconds);
				#endif
			}
		}
		void itemClick(object sender, OpenTK.Input.MouseButtonEventArgs e){
			NotifyValueChanged ("SelectedItem", (sender as GraphicObject).DataSource);
			//Debug.WriteLine ((sender as GraphicObject).DataSource);
		}
	}
}

