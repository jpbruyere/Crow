//
//  TemplatedContainer.cs
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
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Threading;

namespace Crow
{
	public abstract class TemplatedGroup : TemplatedControl
	{
		#region CTOR
		public TemplatedGroup () : base(){}
		#endregion

		protected Group items;
		string _itemTemplate, _dataTest;

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		#endregion

		IList data;
		int _selectedIndex;
		Color selBackground, selForeground;

		int itemPerPage = 50;
		Thread loadingThread = null;
		volatile bool cancelLoading = false;

		#region Templating
		//TODO: dont instantiate ItemTemplates if not used
		//but then i should test if null in msil gen
		public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, Crow.ItemTemplate>();

		/// <summary>
		/// Default item template
		/// </summary>
		[XmlAttributeAttribute][DefaultValue("#Crow.Templates.ItemTemplate.goml")]
		public string ItemTemplate {
			get { return _itemTemplate; }
			set {
				if (value == _itemTemplate)
					return;

				_itemTemplate = value;

				//TODO:reload list with new template?
				NotifyValueChanged("ItemTemplate", _itemTemplate);
			}
		}
		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			items = this.child.FindByName ("ItemsContainer") as Group;
			if (items == null)
				throw new Exception ("TemplatedGroup template Must contain a Group named 'ItemsContainer'");
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
			else
				NotifyValueChanged ("HasChildren", true);
		}
		/// <summary>
		/// Use to define condition on Data item for selecting among ItemTemplates.
		/// Default value is 'TypeOf' for selecting Template depending on Type of Data.
		/// Other possible values are properties of Data
		/// </summary>
		/// <value>The data property test.</value>
		[XmlAttributeAttribute][DefaultValue("TypeOf")]
		public string DataTest {
			get { return _dataTest; }
			set {
				if (value == _dataTest)
					return;

				_dataTest = value;

				NotifyValueChanged("DataTest", _dataTest);
			}
		}
		#endregion

		public virtual List<GraphicObject> Items{ get { return items.Children; }}
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
		[XmlIgnore]public virtual object SelectedItem{
			get { return data == null ? null : _selectedIndex < 0 ? null : data[_selectedIndex]; }
		}
		[XmlIgnore]public bool HasItems {
			get { return Items.Count > 0; }
		}
		[XmlAttributeAttribute]public IList Data {
			get { return data; }
			set {
				if (value == data)
					return;

				cancelLoadingThread ();

				data = value;

				NotifyValueChanged ("Data", data);

				lock (CurrentInterface.UpdateMutex)
					ClearItems ();

				if (data == null)
					return;

				loadingThread = new Thread (loading);
				loadingThread.IsBackground = true;
				loadingThread.Start ();
				//loadPage(1);

				NotifyValueChanged ("SelectedIndex", _selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
				NotifyValueChanged ("HasItems", HasItems);
			}
		}

		[XmlAttributeAttribute][DefaultValue("SteelBlue")]//DeepJungleGreen
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (value == selBackground)
					return;
				selBackground = value;
				NotifyValueChanged ("SelectionBackground", selBackground);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute][DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (value == selForeground)
					return;
				selForeground = value;
				NotifyValueChanged ("SelectionForeground", selForeground);
				RegisterForRedraw ();
			}
		}

		protected void raiseSelectedItemChanged(){
			SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
		}
			

		public virtual void AddItem(GraphicObject g){
			items.AddChild (g);
			g.LogicalParent = this;
			NotifyValueChanged ("HasChildren", true);
		}
		public virtual void RemoveItem(GraphicObject g)
		{
			g.LogicalParent = null;
			items.RemoveChild (g);
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
		}

		public virtual void ClearItems()
		{
			items.ClearChildren ();
			NotifyValueChanged ("HasChildren", false);
		}


		#region GraphicObject overrides
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			foreach (GraphicObject w in Items) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		public override bool Contains (GraphicObject goToFind)
		{
			foreach (GraphicObject w in Items) {
				if (w == goToFind)
					return true;
				if (w.Contains (goToFind))
					return true;
			}
			return base.Contains(goToFind);
		}
//		public override void ClearBinding ()
//		{
//			if (items != null)
//				items.ClearBinding ();
//
//			base.ClearBinding ();
//		}
//		public override void ResolveBindings ()
//		{
//			base.ResolveBindings ();
//			if (items != null)
//				items.ResolveBindings ();
//		}
		#endregion

		#region IXmlSerialisation Overrides
		public override void ReadXml(System.Xml.XmlReader reader)
		{
			using (System.Xml.XmlReader subTree = reader.ReadSubtree ()) {
				subTree.Read ();
				string tmp = subTree.ReadOuterXml ();

				//seek for template tag
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml (xr);
				}
				//process content
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read (); //skip current node

					while (!xr.EOF) {
						xr.Read (); //read first child

						if (!xr.IsStartElement ())
							continue;

						if (xr.Name == "Template" || Name == "ItemTemplate"){
							xr.Skip ();
							if (!xr.IsStartElement ())
								continue;
						}

						Type t = Type.GetType ("Crow." + xr.Name);
						if (t == null) {
							Assembly a = Assembly.GetEntryAssembly ();
							foreach (Type expT in a.GetExportedTypes ()) {
								if (expT.Name == xr.Name)
									t = expT;
							}
						}
						if (t == null)
							throw new Exception (xr.Name + " type not found");

						GraphicObject go = (GraphicObject)Activator.CreateInstance (t);

						(go as IXmlSerializable).ReadXml (xr);

						AddItem (go);

						xr.Read (); //closing tag
					}

				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			throw new NotImplementedException ();
		}
		#endregion

		void loading(){
			if (ItemTemplates == null)
				ItemTemplates = new Dictionary<string, ItemTemplate> ();
			if (!ItemTemplates.ContainsKey ("default"))
				ItemTemplates ["default"] = Interface.GetItemTemplate (ItemTemplate);

			for (int i = 1; i <= (data.Count / itemPerPage) + 1; i++) {
				if (cancelLoading)
					return;
				loadPage (i);
				Thread.Sleep (1);
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

			Group page;
			if (typeof(Wrapper).IsAssignableFrom (items.GetType ())){
				page = items;
				itemPerPage = int.MaxValue;
			}else if (typeof(GenericStack).IsAssignableFrom (items.GetType ())) {
				GenericStack gs = new GenericStack ();
				gs.CurrentInterface = items.CurrentInterface;
				gs.initialize ();
				gs.Orientation = (items as GenericStack).Orientation;
				gs.Width = items.Width;
				gs.Height = items.Height;
				gs.VerticalAlignment = items.VerticalAlignment;
				gs.HorizontalAlignment = items.HorizontalAlignment;
				page = gs;

			}else
				page = Activator.CreateInstance (items.GetType ()) as Group;			

			page.Name = "page" + pageNum;

			for (int i = (pageNum - 1) * itemPerPage; i < pageNum * itemPerPage; i++) {
				if (i >= data.Count)
					break;
				if (cancelLoading)
					return;

				loadItem (i, page);
			}

			if (page == items)
				return;
			lock (CurrentInterface.LayoutMutex)
				items.AddChild (page);

			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine("Listbox {2} Loading: {0} ticks \t, {1} ms",
			loadingTime.ElapsedTicks,
			loadingTime.ElapsedMilliseconds, this.ToString());
			#endif
		}
		string getItempKey(Type dataType, object o){
			try {
				return dataType.GetProperty (_dataTest).GetGetMethod ().Invoke (o, null).ToString();
			} catch (Exception ex) {
				return dataType.FullName;
			}
		}
		protected void loadItem(int i, Group page){
			GraphicObject g = null;
			ItemTemplate iTemp = null;
			Type dataType = data [i].GetType ();
			string itempKey = dataType.FullName;

			if (_dataTest != "TypeOf")
				itempKey = getItempKey (dataType, data [i]);

			if (ItemTemplates.ContainsKey (itempKey))
					iTemp = ItemTemplates [itempKey];
			else
				iTemp = ItemTemplates ["default"];

			lock (CurrentInterface.LayoutMutex) {
				g = iTemp.CreateInstance(CurrentInterface);
				page.AddChild (g);
				//g.LogicalParent = this;
				registerItemClick (g);
			}

			if (iTemp.Expand != null && g is Expandable) {
				(g as Expandable).Expand += iTemp.Expand;
				(g as Expandable).GetIsExpandable = iTemp.HasSubItems;
			}

			g.DataSource = data [i];
		}
		protected virtual void registerItemClick(GraphicObject g){
			g.MouseClick += itemClick;
		}
//		protected void _list_LayoutChanged (object sender, LayoutingEventArgs e)
//		{
//			#if DEBUG_LAYOUTING
//			Debug.WriteLine("list_LayoutChanged");
//			#endif
//			if (_gsList.Orientation == Orientation.Horizontal) {
//				if (e.LayoutType == LayoutingType.Width)
//					_gsList.Width = approxSize;
//			} else if (e.LayoutType == LayoutingType.Height)
//				_gsList.Height = approxSize;
//		}
		int approxSize
		{
			get {
				if (data == null)
					return -1;
				GenericStack page1 = items.FindByName ("page1") as GenericStack;
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
		internal virtual void itemClick(object sender, MouseButtonEventArgs e){
			SelectedIndex = data.IndexOf((sender as GraphicObject).DataSource);
		}
	}
}
