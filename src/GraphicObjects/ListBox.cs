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
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Threading;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.ListBox.goml")]
	public class ListBox : TemplatedControl
	{
		#region CTOR
		public ListBox () : base() {}
		#endregion

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		#endregion

		Group _list;
		GenericStack _gsList;
		IList data;
		int _selectedIndex;
		string _itemTemplate;
		int itemPerPage = 50;
		Thread loadingThread = null;
		volatile bool cancelLoading = false;

		IMLStream templateStream = null;

		[XmlAttributeAttribute]public IList Data {
			get {
				return data;
			}
			set {
				if (value == data)
					return;
				
				cancelLoadingThread ();

				data = value;

				NotifyValueChanged ("Data", data);

				lock (Interface.CurrentInterface.UpdateMutex)
					_list.ClearChildren ();
				if (_gsList.Orientation == Orientation.Horizontal)
					_gsList.Width = -1;
				else
					_gsList.Height = -1;

				if (data == null)
					return;

				loadingThread = new Thread (loading);
				loadingThread.IsBackground = true;
				loadingThread.Start ();
				//loadPage(1);

				NotifyValueChanged ("SelectedIndex", _selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
			}
		}
		[XmlAttributeAttribute][DefaultValue("#Crow.Templates.ItemTemplate.goml")]
		public string ItemTemplate {
			get { return _itemTemplate; }
			set { 
				if (value == _itemTemplate)
					return;

				_itemTemplate = value;

				if (templateStream != null) {
					templateStream.Dispose ();
					templateStream = null;
				}

				//TODO:reload list with new template?
				NotifyValueChanged("ItemTemplate", _itemTemplate);
			}
		}
		[XmlAttributeAttribute][DefaultValue(-1)]public int SelectedIndex{
			get { return _selectedIndex; }
			set { 
				if (value == _selectedIndex)
					return;

				_selectedIndex = value; 

				NotifyValueChanged ("SelectedIndex", _selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
			}
		}
		[XmlIgnore]public object SelectedItem{
			get { return data == null ? null : _selectedIndex < 0 ? null : data[_selectedIndex]; }
		}
			
		#region implemented abstract members of TemplatedControl
		protected override void loadTemplate (GraphicObject template = null)
		{
			base.loadTemplate (template);
			_list = this.child.FindByName ("List") as Group;
			if (_list == null)
				throw new Exception ("ListBox Template MUST contain a Goup widget named 'List'.");
			_gsList = _list as GenericStack;
		}
		#endregion
		void loading(){			
			templateStream = new IMLStream (ItemTemplate);
			for (int i = 1; i <= (data.Count / itemPerPage) + 1; i++) {
				if (cancelLoading)
					return;
				loadPage (i);
			}
		}
		void cancelLoadingThread(){
			if (loadingThread == null)
				return;
			if (!loadingThread.IsAlive)
				return;			
			cancelLoading = true;
			loadingThread.Join ();
			cancelLoading = false;
		}
		void loadPage(int pageNum)
		{
			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif

			Group page = _list.Clone () as Group;
			
			page.Name = "page" + pageNum;

			//reset size to fit in the dir of the stacking
			//because _list total size is forced to approx size
			if (_gsList.Orientation == Orientation.Horizontal) {
				page.Width = Measure.Fit;
				page.BindMember ("Height", "../HeightPolicy");
			} else {
				page.Height = Measure.Fit;
				page.BindMember ("Width", "../WidthPolicy");
			}

			for (int i = (pageNum - 1) * itemPerPage; i < pageNum * itemPerPage; i++) {
				if (i >= data.Count)
					break;
				if (cancelLoading)
					return;

				GraphicObject g = Interface.Load (templateStream);
				g.MouseClick += itemClick;

				lock (Interface.CurrentInterface.UpdateMutex)
					page.AddChild (g);
				g.DataSource = data [i];
				//g.LogicalParent = this;
			}

			lock (Interface.CurrentInterface.UpdateMutex)
				_list.AddChild (page);
				
			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine("Listbox {2} Loading: {0} ticks \t, {1} ms",
			loadingTime.ElapsedTicks,
			loadingTime.ElapsedMilliseconds, this.ToString());
			#endif
		}
		protected void _list_LayoutChanged (object sender, LayoutingEventArgs e)
		{
#if DEBUG_LAYOUTING
			Debug.WriteLine("list_LayoutChanged");
#endif
			if (_gsList.Orientation == Orientation.Horizontal) {
				if (e.LayoutType == LayoutingType.Width)
					_gsList.Width = approxSize;
			} else if (e.LayoutType == LayoutingType.Height)
				_gsList.Height = approxSize;
		}
		int approxSize
		{
			get {
				if (data == null)
					return -1;
				GenericStack page1 = _list.FindByName ("page1") as GenericStack;
				if (page1 == null)
					return -1;
				
				return page1.Orientation == Orientation.Horizontal ?
					data.Count < itemPerPage ?
						-1:
					(int)Math.Ceiling ((double)page1.Slot.Width / (double)itemPerPage * (double)(data.Count+1)):
					data.Count < itemPerPage ?
						-1:
					(int)Math.Ceiling ((double)page1.Slot.Height / (double)itemPerPage * (double)(data.Count+1));
			}
		}
		void itemClick(object sender, MouseButtonEventArgs e){
			SelectedIndex = data.IndexOf((sender as GraphicObject).DataSource);
		}

		#region IXmlSerializable
//		public override System.Xml.Schema.XmlSchema GetSchema(){ return null; }
//		public override void ReadXml(System.Xml.XmlReader reader)
//		{
//			//Template could be either an attribute containing path or expressed inlined
//			//as a Template Element
//			using (System.Xml.XmlReader subTree = reader.ReadSubtree())
//			{
//				subTree.Read ();
//
//				string template = reader.GetAttribute ("Template");
//				string tmp = subTree.ReadOuterXml ();
//
//				//Load template from path set as attribute in templated control
//				if (string.IsNullOrEmpty (template)) {					
//					//seek for template tag first
//					using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
//						//load template first if inlined
//
//						xr.Read (); //skip current node
//
//						while (!xr.EOF) {
//							xr.Read (); //read first child
//							if (!xr.IsStartElement ())
//								continue;
//							if (xr.Name == "Template") {
//								xr.Read ();
//
//								Type t = Type.GetType ("Crow." + xr.Name);
//								GraphicObject go = (GraphicObject)Activator.CreateInstance (t);                                
//								(go as IXmlSerializable).ReadXml (xr);
//
//								loadTemplate (go);
//
//								xr.Read ();//go close tag
//								xr.Read ();//Template close tag
//								break;
//							} else {
//								xr.ReadInnerXml ();
//							}
//						}
//					}				
//				} else
//					loadTemplate (Interface.Load (template, this));
//
//
//				//normal xml read
//				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
//					xr.Read ();
//					base.ReadXml(xr);
//				}
//			}
//		}
//		public override void WriteXml(System.Xml.XmlWriter writer)
//		{
//			//TODO:
//			throw new NotImplementedException();
//		}
		#endregion
	}
}

