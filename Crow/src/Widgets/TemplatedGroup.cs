// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Crow.Cairo;
using Crow.IML;

namespace Crow {
	public abstract class TemplatedGroup : TemplatedControl
	{
		#if DESIGN_MODE
		public override void getIML (System.Xml.XmlDocument doc, System.Xml.XmlNode parentElem)
		{
			if (this.design_isTGItem)
				return;
			base.getIML (doc, parentElem);

			if (string.IsNullOrEmpty(_itemTemplate)) {
				foreach (ItemTemplate it in ItemTemplates.Values) 
					it.getIML (doc, parentElem.LastChild);				
			}

			foreach (Widget g in Items) {
				g.getIML (doc, parentElem.LastChild);	
			}
		}
		#endif

		#region CTOR
		protected TemplatedGroup() {}
		protected TemplatedGroup (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		protected Group items;
		string _itemTemplate, dataTest;

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		/// <summary>
		/// raised when root widget of item template is a 'ListItem' and this item is selected.
		/// </summary>
		public event EventHandler<SelectionChangeEventArgs> SelectedItemContainerChanged;
		public event EventHandler Loaded;
		#endregion

		IEnumerable data;

		int itemPerPage = 50;
		CrowThread loadingThread = null;

		bool isPaged = false;

		#region Templating
		//TODO: dont instantiate ItemTemplates if not used
		//but then i should test if null in msil gen
		public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, ItemTemplate>();

		/// <summary>
		/// Keep track of expanded subnodes and closed time to unload
		/// </summary>
		//Dictionary<GraphicObject, Stopwatch> nodes = new Dictionary<GraphicObject, Stopwatch>();
		internal List<Widget> nodes = new List<Widget>();
		/// <summary>
		/// Item templates file path, on disk or embedded.
		/// 
		/// ItemTemplate file may contains either a single template without the
		/// ItemTemplate enclosing tag, or several item templates each enclosed
		/// in a separate tag.
		/// </summary>		
		public string ItemTemplate {
			get { return _itemTemplate; }
			set {
				if (value == _itemTemplate)
					return;

				_itemTemplate = value;

				//TODO:reload list with new template?
				NotifyValueChangedAuto (_itemTemplate);
			}
		}
		protected override void loadTemplate(Widget template = null)
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
		[DefaultValue("TypeOf")]
		public string DataTest {
			get { return dataTest; }
			set {
				if (value == dataTest)
					return;

				dataTest = value;

				NotifyValueChangedAuto (dataTest);
			}
		}
		#endregion

		public virtual List<Widget> Items{
			get {
				return isPaged ? items.Children.SelectMany(x => (x as Group).Children).ToList()
				: items.Children;
			}
		}

		object selectedItem;
		Widget selectedItemContainer = null;

		[XmlIgnore]public virtual object SelectedItem{
			get => selectedItem;
			set {
				if (SelectedItem == value)
					return;
					
				selectedItem = value;

				NotifyValueChanged ("SelectedItem", SelectedItem);
				SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
			}
		}

		[XmlIgnore]public bool HasItems {
			get { return Items.Count > 0; }
		}
		public IEnumerable Data {
			get { return data; }
			set {
				if (value == data)
					return;

				cancelLoadingThread ();

				if (data is IObservableList) {
					IObservableList ol = data as IObservableList;
					ol.ListAdd -= Ol_ListAdd;
					ol.ListRemove -= Ol_ListRemove;
					ol.ListEdit -= Ol_ListEdit;
				}

				data = value;

				if (data is IObservableList) {
					IObservableList ol = data as IObservableList;
					ol.ListAdd += Ol_ListAdd;
					ol.ListRemove += Ol_ListRemove;
					ol.ListEdit += Ol_ListEdit;
				}

				NotifyValueChangedAuto (data);

				lock (IFace.UpdateMutex)
					ClearItems ();

				if (data == null)
					return;

				loadingThread = new CrowThread (this, loading);
				loadingThread.Finished += (object sender, EventArgs e) => (sender as TemplatedGroup).Loaded.Raise (sender, e);
				loadingThread.Start ();

				//NotifyValueChanged ("SelectedIndex", _selectedIndex);
				//NotifyValueChanged ("SelectedItem", SelectedItem);
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
		void Ol_ListEdit (object sender, ListChangedEventArg e) {
			if (this.isPaged) {
				throw new NotImplementedException ();
			} else
				items.Children [e.Index].DataSource = e.Element;

		}


		protected void raiseSelectedItemChanged(){
			SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
		}


		public virtual void AddItem(Widget g){
			items.AddChild (g);
			g.LogicalParent = this;
			NotifyValueChanged ("HasChildren", true);
		}
		public virtual void RemoveItem(Widget g)
		{
			g.LogicalParent = null;
			items.DeleteChild (g);
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
		}

		public virtual void ClearItems()
		{
			selectedItemContainer = null;
			SelectedItem = null;

			items.ClearChildren ();
			NotifyValueChanged ("HasChildren", false);
		}


		#region GraphicObject overrides
		public override Widget FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			foreach (Widget w in Items) {
				Widget r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		public override Widget FindByType<T> ()
		{
			if (this is T)
				return this;

			foreach (Widget w in Items) {
				Widget r = w.FindByType<T> ();
				if (r != null)
					return r;
			}
			return null;
		}
		public override bool Contains (Widget goToFind)
		{
			foreach (Widget w in Items) {
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
#if DEBUG_LOG
			DbgLogger.StartEvent (DbgEvtType.TGLoadingThread, this);
#endif
			try {
				loadPage (data, items, dataTest);
			} catch (Exception ex) {
				if (Monitor.IsEntered (IFace.LayoutMutex))
					Monitor.Exit (IFace.LayoutMutex);
				System.Diagnostics.Debug.WriteLine ("loading thread aborted: " + ex.ToString());
			}
#if DEBUG_LOG
			DbgLogger.EndEvent (DbgEvtType.TGLoadingThread);
#endif
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
			if (loadingThread == null)
				return;
			bool updateMx = Monitor.IsEntered (IFace.UpdateMutex);
			bool layoutMx = Monitor.IsEntered (IFace.LayoutMutex);

#if DEBUG_LOG
			DbgLogger.AddEvent (DbgEvtType.TGCancelLoadingThread, this);
#endif

			if (layoutMx)
				Monitor.Exit (IFace.LayoutMutex);
			if (updateMx)
				Monitor.Exit (IFace.UpdateMutex);

			loadingThread.Cancel ();

			if (layoutMx)
				Monitor.Enter (IFace.LayoutMutex);
			if (updateMx)
				Monitor.Enter (IFace.UpdateMutex);

		}
		void loadPage(IEnumerable _data, Group page, string _dataTest)
		{
			#if DEBUG_LOAD
			Stopwatch loadingTime = Stopwatch.StartNew ();
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

			if (_data == null)
				return;

			foreach (object d in _data) {
				loadItem (d, page, _dataTest);
				if (loadingThread.cancelRequested)
					break;
			}

//			if (page == items)
//				return;
//			lock (CurrentInterface.LayoutMutex)
//				items.AddChild (page);

#if DEBUG_LOAD
			loadingTime.Stop ();
			using (StreamWriter sw = new StreamWriter ("loading.log", true)) {
				sw.WriteLine ($"NEW ;{this.ToString(),-50};{loadingTime.ElapsedTicks,8};{loadingTime.ElapsedMilliseconds,8}");
			}
#endif
		}

		protected void loadItem(object o, Group page, string _dataTest){
			if (o == null)//TODO:surely a threading sync problem
				return;
			Widget g = null;
			ItemTemplate iTemp = null;
			Type dataType = o.GetType ();
			string itempKey = dataType.FullName;

			//if item template selection is not done depending on the type of item
			//dataTest must contains a member name of the item 
			if (_dataTest != "TypeOf") {
				try {
					itempKey = CompilerServices.getValue (dataType, o, _dataTest)?.ToString ();
				} catch {
					itempKey = dataType.FullName;
				}
			}

			if (ItemTemplates.ContainsKey (itempKey))
				iTemp = ItemTemplates [itempKey];
			else {
				foreach (string it in ItemTemplates.Keys) {
					if (it == "default")
						continue;
					Type t = CompilerServices.getTypeFromName (it);
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

			Monitor.Enter (IFace.LayoutMutex);
				g = iTemp.CreateInstance();
				#if DESIGN_MODE
				g.design_isTGItem = true;
				#endif
				page.AddChild (g);
//				if (isPaged)
				g.LogicalParent = this;
				g.MouseClick += itemClick;
			Monitor.Exit (IFace.LayoutMutex);

			if (iTemp.Expand != null) {
				Expandable e = g as Expandable;
				if (e == null)
					e = g.FindByType<Expandable> () as Expandable;
					
				if (e != null) { 
					e.Expand += iTemp.Expand;
					if ((o as ICollection) == null)
						e.GetIsExpandable = new BooleanTestOnInstance ((instance) => true);
					else
						e.GetIsExpandable = iTemp.HasSubItems;
				}
			}

			if (g is ListItem li) {
				li.Selected += Li_Selected;
			}

			g.DataSource = o;
		}

		//void expandable_expandevent (object sender, EventHandler )
		void Li_Selected (object sender, EventArgs e)
		{
			SelectedItemContainerChanged.Raise (this, new SelectionChangeEventArgs (sender));
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
					(data as ICollection)?.Count < itemPerPage ?
					-1:
					(int)Math.Ceiling ((double)page1.Slot.Width / (double)itemPerPage * (double)((data as ICollection)?.Count+1)):
					(data as ICollection)?.Count < itemPerPage ?
					-1:
					(int)Math.Ceiling ((double)page1.Slot.Height / (double)itemPerPage * (double)((data as ICollection)?.Count+1));
			}
		}
		internal virtual void itemClick(object sender, MouseButtonEventArgs e){
			//SelectedIndex = (int)((IList)data)?.IndexOf((sender as Widget).DataSource);
			if (selectedItemContainer is ListItem li)
				li.IsSelected = false;
			selectedItemContainer = sender as Widget;
			if (selectedItemContainer is ListItem nli)
				nli.IsSelected = true;
			if (selectedItemContainer == null)
				return;
			SelectedItem = selectedItemContainer.DataSource;
			//SelectedIndex = items.Children.IndexOf(sender as Widget);
		}

		bool emitHelperIsAlreadyExpanded (Widget go){
			if (nodes.Contains (go))
				return true;
			nodes.Add (go);
			return false;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				cancelLoadingThread ();
			base.Dispose (disposing);
		}

		public override void OnDataSourceChanged (object sender, DataSourceChangeEventArgs e)
		{
			base.OnDataSourceChanged (sender, e);
		}

		public void OnInsertClick (object sender, MouseEventArgs e)
		{
			if (data is IObservableList)
				(data as IObservableList).Insert ();
		}
		public void OnRemoveClick (object sender, MouseEventArgs e)
		{
			if (data is IObservableList)
				(data as IObservableList).Remove ();
		}
		public void OnUpdateClick (object sender, MouseEventArgs e) {
			if (data is IObservableList)
				(data as IObservableList).RaiseEdit ();
		}
	}
}
