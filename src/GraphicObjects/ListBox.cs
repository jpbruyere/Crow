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
using System.Xml;
using System.Collections.Generic;
using System.Threading;

namespace go
{
	[DefaultTemplate("#go.Templates.Listbox.goml")]
	//[DefaultTemplate("#go.Templates.ItemTemplate.goml")]
	public class ListBox : TemplatedControl, IXmlSerializable
	{
		#region CTOR
		public ListBox () : base() {}
		#endregion

		Group _list;
		IList data;
		int _selectedIndex;
		string _itemTemplate;

		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;

		#region implemented abstract members of TemplatedControl
		protected override void loadTemplate (GraphicObject template = null)
		{
			base.loadTemplate (template);
			_list = this.child.FindByName ("List") as Group;
		}
		#endregion

		[XmlAttributeAttribute][DefaultValue("#go.Templates.ItemTemplate.goml")]
		public string ItemTemplate {
			get { return _itemTemplate; }
			set { 
				//TODO:reload list with new template?
				_itemTemplate = value; 
			}
		}
		[XmlAttributeAttribute][DefaultValue(-1)]
		public int SelectedIndex{
			get { return _selectedIndex; }
			set { _selectedIndex = value; }
		}
		public object SelectedItem{
			get { return data == null ? null : data[_selectedIndex]; }
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

				pendingChildrenAddition = new Queue<GraphicObject> ();
				threadedLoadingFinished = false;

				Thread t = new Thread (loadingThread);
				t.Start ();
				t.Join ();

			}
		}
		public override void UpdateLayout (LayoutingType layoutType)
		{
			CheckPendingChildrenAddition ();
			base.UpdateLayout (layoutType);
		}
		internal void CheckPendingChildrenAddition()
		{
			if (pendingChildrenAddition == null)
				return;
			lock (pendingChildrenAddition) {
				if (!threadedLoadingFinished && pendingChildrenAddition.Count < 50)
					return;
				while (pendingChildrenAddition.Count > 0)
					_list.addChild (pendingChildrenAddition.Dequeue ());
			}
		}

		volatile Queue<GraphicObject> pendingChildrenAddition;
		volatile bool threadedLoadingFinished = false;

		void loadingThread()
		{
			#if DEBUG_LOAD_TIME
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif

			MemoryStream ms = new MemoryStream ();
			lock (ItemTemplate) {
				using (Stream stream = Interface.GetStreamFromPath (ItemTemplate))
					stream.CopyTo (ms);
			}

			Type t = Interface.GetTopContainerOfGOMLStream (ms);

			foreach (var item in data) {
				ms.Seek(0,SeekOrigin.Begin);
				GraphicObject g = Interface.Load (ms, t);
				g.DataSource = item;
				g.MouseClick += itemClick;

				lock (pendingChildrenAddition) {
					pendingChildrenAddition.Enqueue (g);
				}
			}

			ms.Dispose ();			

			threadedLoadingFinished = true;

			#if DEBUG_LOAD_TIME
			loadingTime.Stop ();
			Debug.WriteLine("Listbox {2} Loading: {0} ticks \t, {1} ms",
				loadingTime.ElapsedTicks,
				loadingTime.ElapsedMilliseconds, this.ToString());
			#endif
		}

		void itemClick(object sender, OpenTK.Input.MouseButtonEventArgs e){
			SelectedItemChanged.Raise (sender, new SelectionChangeEventArgs((sender as GraphicObject).DataSource));
			NotifyValueChanged ("SelectedItem", (sender as GraphicObject).DataSource);
			//Debug.WriteLine ((sender as GraphicObject).DataSource);
		}

		#region IXmlSerializable
		public override System.Xml.Schema.XmlSchema GetSchema(){ return null; }
		public override void ReadXml(System.Xml.XmlReader reader)
		{
			//Template could be either an attribute containing path or expressed inlined
			//as a Template Element
			using (System.Xml.XmlReader subTree = reader.ReadSubtree())
			{
				subTree.Read ();

				string template = reader.GetAttribute ("Template");
				string tmp = subTree.ReadOuterXml ();

				//Load template from path set as attribute in templated control
				if (string.IsNullOrEmpty (template)) {					
					//seek for template tag first
					using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
						//load template first if inlined

						xr.Read (); //skip current node

						while (!xr.EOF) {
							xr.Read (); //read first child
							if (!xr.IsStartElement ())
								continue;
							if (xr.Name == "Template") {
								xr.Read ();

								Type t = Type.GetType ("go." + xr.Name);
								GraphicObject go = (GraphicObject)Activator.CreateInstance (t);                                
								(go as IXmlSerializable).ReadXml (xr);

								loadTemplate (go);

								xr.Read ();//go close tag
								xr.Read ();//Template close tag
								break;
							} else {
								xr.ReadInnerXml ();
							}
						}
					}				
				} else
					loadTemplate (Interface.Load (template, this, !Interface.DontResoveGOML));


				//normal xml read
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml(xr);
				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			//TODO:
			throw new NotImplementedException();
		}
		#endregion
	}
}

