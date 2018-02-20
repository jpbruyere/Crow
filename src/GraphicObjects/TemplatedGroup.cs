//
// TemplatedGroup.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using System.Linq;
using Crow.IML;
using System.Diagnostics;

namespace Crow
{
	public abstract class TemplatedGroup : TemplatedControl
	{
		#region CTOR
		protected TemplatedGroup() : base(){}
		public TemplatedGroup (Interface iface) : base(iface){}
		#endregion

		protected Group items;
		string _itemTemplate, dataTest;

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		public event EventHandler Loaded;
		#endregion

		IList data;
		int _selectedIndex = -1;
		Color selBackground, selForeground;

		int itemPerPage = 50;
		CrowThread loadingThread = null;

		bool isPaged = false;

		#region Templating
		//TODO: dont instantiate ItemTemplates if not used
		//but then i should test if null in msil gen
		public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, Crow.ItemTemplate>();

		/// <summary>
		/// Keep track of expanded subnodes and closed time to unload
		/// </summary>
		//Dictionary<GraphicObject, Stopwatch> nodes = new Dictionary<GraphicObject, Stopwatch>();
		internal List<GraphicObject> nodes = new List<GraphicObject>();
		/// <summary>
		/// Item templates file path, on disk or embedded.
		/// 
		/// ItemTemplate file may contains either a single template without the
		/// ItemTemplate enclosing tag, or several item templates each enclosed
		/// in a separate tag.
		/// </summary>
		[XmlAttributeAttribute]
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
			get { return dataTest; }
			set {
				if (value == dataTest)
					return;

				dataTest = value;

				NotifyValueChanged("DataTest", dataTest);
			}
		}
		#endregion

		public virtual List<GraphicObject> Items{
			get {
				return isPaged ? items.Children.SelectMany(x => (x as Group).Children).ToList()
				: items.Children;
			}
		}
		[XmlAttributeAttribute][DefaultValue(-1)]public int SelectedIndex{
			get { return _selectedIndex; }
			set {
				if (value == _selectedIndex)
					return;

				if (_selectedIndex >= 0 && Items.Count > _selectedIndex) {
					Items[_selectedIndex].Foreground = Color.Transparent;
					Items[_selectedIndex].Background = Color.Transparent;
				}

				_selectedIndex = value;

				if (_selectedIndex >= 0 && Items.Count > _selectedIndex) {
					Items[_selectedIndex].Foreground = SelectionForeground;
					Items[_selectedIndex].Background = SelectionBackground;
				}

				NotifyValueChanged ("SelectedIndex", _selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
			}
		}
		[XmlIgnore]public virtual object SelectedItem{
			get { return data == null ? null : _selectedIndex < 0 ? data.GetDefaultValue() : data[_selectedIndex]; }
			set {
				if (data == null) {
					SelectedIndex = -1;
					return;
				}
				//TODO:double check if value type will be notified to binding sys
				if (value == SelectedItem)
					return;
				
				SelectedIndex = data.IndexOf (value);
			}
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

				if (data is IObservableList) {
					IObservableList ol = data as IObservableList;
					ol.ListAdd -= Ol_ListAdd;
					ol.ListRemove -= Ol_ListRemove;
				}

				data = value;

				if (data is IObservableList) {
					IObservableList ol = data as IObservableList;
					ol.ListAdd += Ol_ListAdd;
					ol.ListRemove += Ol_ListRemove;
				}

				NotifyValueChanged ("Data", data);

				//lock (CurrentInterface.LayoutMutex)
					ClearItems ();

				if (data == null)
					return;

				loadingThread = new CrowThread (this, loading);
				loadingThread.Finished += (object sender, EventArgs e) => (sender as TemplatedGroup).Loaded.Raise (sender, e);
				loadingThread.Start ();

				NotifyValueChanged ("SelectedIndex", _selectedIndex);
				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
				NotifyValueChanged ("HasItems", HasItems);
			}
		}

		void Ol_ListRemove (object sender, ListChangedEventArg e)
		{
			if (this.isPaged) {
				int p = e.Index / itemPerPage;
				int i = e.Index % itemPerPage;
				(items.Children [p] as Group).DeleteChild (i);
			} else
				items.DeleteChild (e.Index);
		}

		void Ol_ListAdd (object sender, ListChangedEventArg e)
		{
			if (this.isPaged) {
				throw new NotImplementedException();
//				int p = e.Index / itemPerPage;
//				int i = e.Index % itemPerPage;
//				(items.Children [p] as Group).InsertChild (i, e.Element);
			} else
				loadItem (e.Element, items, dataTest);
		}

		[XmlAttributeAttribute][DefaultValue("SteelBlue")]
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
			items.DeleteChild (g);
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
		}

		public virtual void ClearItems()
		{
			_selectedIndex = -1;
			NotifyValueChanged ("SelectedIndex", _selectedIndex);
			NotifyValueChanged ("SelectedItem", null);

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

		/// <summary>
		/// Items loading thread
		/// </summary>
		void loading(){
			loadPage (data, items, dataTest);
		}
//			//if (!ItemTemplates.ContainsKey ("default"))
//			//	ItemTemplates ["default"] = Interface.GetItemTemplate (ItemTemplate);
//
//			for (int i = 1; i <= (data.Count / itemPerPage) + 1; i++) {
//				if ((bool)loadingThread?.cancelRequested) {
//					this.Dispose ();
//					return;
//				}
//				loadPage (i);
//				Thread.Sleep (1);
//			}
//		}
		void cancelLoadingThread(){
			if (loadingThread != null)
				loadingThread.Cancel ();
		}
		void loadPage(IList _data, Group page, string _dataTest)
		{
			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif


//			if (typeof(TabView).IsAssignableFrom (items.GetType ())||
//				typeof(Menu).IsAssignableFrom (this.GetType())||
//				typeof(Wrapper).IsAssignableFrom (items.GetType ())) {
				//page = items;
				itemPerPage = int.MaxValue;
//			} else if (typeof(GenericStack).IsAssignableFrom (items.GetType ())) {
//				GenericStack gs = new GenericStack (items.CurrentInterface);
//				gs.Orientation = (items as GenericStack).Orientation;
//				gs.Width = items.Width;
//				gs.Height = items.Height;
//				gs.VerticalAlignment = items.VerticalAlignment;
//				gs.HorizontalAlignment = items.HorizontalAlignment;
//				page = gs;
//				page.Name = "page" + pageNum;
//				isPaged = true;
//			} else {
//				page = Activator.CreateInstance (items.GetType ()) as Group;
//				page.CurrentInterface = items.CurrentInterface;
//				page.Initialize ();
//				page.Name = "page" + pageNum;
//				isPaged = true;
//			}

			for (int i = 0; i < _data.Count; i++) {				
				loadItem (_data[i], page, _dataTest);
				if (loadingThread.cancelRequested)
					break;
			}

//			if (page == items)
//				return;
//			lock (CurrentInterface.LayoutMutex)
//				items.AddChild (page);

			#if DEBUG_LOAD
			loadingTime.Stop ();
			Debug.WriteLine("Listbox {2} Loading: {0} ticks \t, {1} ms",
			loadingTime.ElapsedTicks,
			loadingTime.ElapsedMilliseconds, this.ToString());
			#endif
		}
		string getItempKey(Type dataType, object o, string propertyName){
			try {
				return dataType.GetProperty (propertyName).GetGetMethod ().Invoke (o, null)?.ToString();
			} catch  {
				return dataType.FullName;
			}
		}
		protected void loadItem(object o, Group page, string _dataTest){
			if (o == null)//TODO:surely a threading sync problem
				return;
			GraphicObject g = null;
			ItemTemplate iTemp = null;
			Type dataType = o.GetType ();
			string itempKey = dataType.FullName;

			if (_dataTest != "TypeOf")
				itempKey = getItempKey (dataType, o, _dataTest);

			if (ItemTemplates.ContainsKey (itempKey))
				iTemp = ItemTemplates [itempKey];
			else {
				foreach (string it in ItemTemplates.Keys) {
					Type t = CompilerServices.tryGetType (it);
					if (t == null)
						continue;
					if (t.IsAssignableFrom (dataType)) {//TODO:types could be cached
						iTemp = ItemTemplates [it];
						break;
					}
				}
				if (iTemp == null)
					iTemp = ItemTemplates ["default"];
			}

			//lock (CurrentInterface.LayoutMutex) {
				g = iTemp.CreateInstance(CurrentInterface);
				page.AddChild (g);
//				if (isPaged)
				g.LogicalParent = this;
				g.MouseDown += itemClick;
			//}

			if (iTemp.Expand != null && g is Expandable) {
				(g as Expandable).Expand += iTemp.Expand;
				(g as Expandable).GetIsExpandable = iTemp.HasSubItems;
			}

			g.DataSource = o;
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

		bool emitHelperIsAlreadyExpanded (GraphicObject go){
			if (nodes.Contains (go))
				return true;
			nodes.Add (go);
			return false;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && loadingThread != null)
				loadingThread.Cancel ();
			base.Dispose (disposing);
		}
	}
}
