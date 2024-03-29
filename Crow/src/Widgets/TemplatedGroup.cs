﻿// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Drawing2D;

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

		protected Group itemsContainer;
		//if scroller name 'ItemsScroller' is found in template, scroll will adapt to selected items change.
		protected Scroller scroller;
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
		bool useLoadingThread;
		/// <summary>
		/// Use anothred thread for loading items, default value is true.
		/// </summary>
		[DefaultValue(true)]
		public bool UseLoadingThread {
			get => useLoadingThread;
			set {
				if (useLoadingThread == value)
					return;
				useLoadingThread = value;
				NotifyValueChangedAuto (useLoadingThread);
			}
		}

		#region Templating
		//TODO: dont instantiate ItemTemplates if not used
		//but then i should test if null in msil gen
		public Dictionary<string, ItemTemplate> ItemTemplates = new Dictionary<string, ItemTemplate>();
		/// <summary>
		/// True if this templated group contains at least one item template.
		/// </summary>
		public bool HasItemTemplates => ItemTemplates.Count > 0;

		/// <summary>
		/// Keep track of expanded subnodes and closed time to unload
		/// </summary>
		//Dictionary<Widget, Stopwatch> nodes = new Dictionary<Widget, Stopwatch>();
		internal List<Widget> nodes = new List<Widget>();//TODO:close time tracking
		/// <summary>
		/// Item templates file path, on disk or embedded.
		///
		/// ItemTemplate file may contains either a single template without the
		/// ItemTemplate enclosing tag, or several item templates each enclosed
		/// in a separate tag.
		/// </summary>
		public string ItemTemplate {
			get => _itemTemplate;
			set {
				if (value == _itemTemplate)
					return;

				_itemTemplate = value;

				//TODO:reload list with new template?
				NotifyValueChangedAuto (_itemTemplate);

				loadItemTemplateFromPropertyValue ();
			}
		}
		/// <summary>
		/// load ItemTemplate(s) from ItemTemplate property
		/// </summary>
		void loadItemTemplateFromPropertyValue () {
			ItemTemplates.Clear();
			if (string.IsNullOrEmpty (_itemTemplate))
				return;
			if (_itemTemplate.Trim().StartsWith('<')) {//iml fragment in property
				using (Stream stream = new MemoryStream (System.Text.Encoding.UTF8.GetBytes (_itemTemplate))) {
					foreach	(string[] itempIds in Instantiator.loadItemTemplatesFromTemplatedGroupProperty (IFace, stream)) {
						ItemTemplate itemp = IFace.GetItemTemplate (itempIds[1]);
						ItemTemplates.Add (itempIds[0], itemp);
						if (string.IsNullOrEmpty (itempIds[2]))
							continue;
						itemp.CreateExpandDelegate (this);
					}
				}
			} else {
				foreach	(string[] itempIds in Instantiator.loadItemTemplatesFromTemplatedGroupProperty (IFace, _itemTemplate)) {
					ItemTemplate itemp = IFace.GetItemTemplate (itempIds[1]);
					ItemTemplates.Add (itempIds[0], itemp);
					if (string.IsNullOrEmpty (itempIds[2]))
						continue;
					itemp.CreateExpandDelegate (this);
				}
			}
			realoadDatas ();
		}
		void realoadDatas () {
			if (data == null)
				return;
			IEnumerable dataSave = data;
			Data = null;
			Data = dataSave;
		}
		protected override void loadTemplate(Widget template = null)
		{
			base.loadTemplate (template);

			itemsContainer = this.child.FindByName ("ItemsContainer") as Group;
			if (itemsContainer == null)
				throw new Exception ("TemplatedGroup template Must contain a Group named 'ItemsContainer'");
			scroller = child.FindByName ("ItemsScroller") as Scroller;
			NotifyValueChanged ("Items", Items);
			if (itemsContainer.Children.Count == 0)
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
			get => dataTest;
			set {
				if (value == dataTest)
					return;

				dataTest = value;

				NotifyValueChangedAuto (dataTest);
			}
		}
		#endregion

		public virtual IList<Widget> Items{
			get {
				return isPaged ? (IList<Widget>)itemsContainer?.Children.SelectMany(x => (x as Group).Children).ToList()
				: (IList<Widget>)itemsContainer?.Children;
			}
		}

		object selectedItem;
		protected Widget selectedItemContainer = null;

		[XmlIgnore]public virtual object SelectedItem{
			get => selectedItem;
			set {
				if (selectedItem == value)
					return;

				if (selectedItem is ISelectable oldItem)
					oldItem.IsSelected = false;

				selectedItem = value;

				if (selectedItem is ISelectable newItem)
					newItem.IsSelected = true;

				NotifyValueChanged ("SelectedItem", SelectedItem);
				NotifyValueChanged ("SelectedIndex", SelectedIndex);
				onSelectedItemChanged (this, new SelectionChangeEventArgs (SelectedItem));
			}
		}
		public virtual void onSelectedItemChanged (object sender, SelectionChangeEventArgs e) {
			SelectedItemChanged.Raise (sender, e);
		}
		[XmlIgnore]public virtual int SelectedIndex{
			get => selectedItemContainer == null ? -1 : itemsContainer.Children.IndexOf (selectedItemContainer);
			set {
				if (SelectedIndex == value)
					return;

				if (value < 0 || itemsContainer.Children.Count == 0)
					Li_Selected (null, null);
				if (value < itemsContainer.Children.Count)
					Li_Selected (itemsContainer.Children[value], null);
				else
					Li_Selected (itemsContainer.Children[itemsContainer.Children.Count - 1], null);
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
					ol.ListClear -= Ol_ListClear;

				}

				data = value;

				if (data is IObservableList) {
					IObservableList ol = data as IObservableList;
					ol.ListAdd += Ol_ListAdd;
					ol.ListRemove += Ol_ListRemove;
					ol.ListEdit += Ol_ListEdit;
					ol.ListClear += Ol_ListClear;
				}

				NotifyValueChangedAuto (data);

				lock (IFace.UpdateMutex)
					ClearItems ();

				if (data == null) {
					NotifyValueChanged ("HasItems", false);
					return;
				}

				if (data is ICollection c) {
					if (c.Count == 0) {
						NotifyValueChanged ("HasItems", false);
						return;
					}
				}

				if (useLoadingThread) {
					loadingThread = new CrowThread (this, loading);
					loadingThread.Finished += (object sender, EventArgs e) => (sender as TemplatedGroup).Loaded.Raise (sender, e);
					loadingThread.Start ();
				} else {
					loading();
					Loaded.Raise (this, null);
				}

				//NotifyValueChanged ("SelectedIndex", _selectedIndex);
				//NotifyValueChanged ("SelectedItem", SelectedItem);
				NotifyValueChanged ("HasItems", HasItems);
			}
		}

		void Ol_ListRemove (object sender, ListChangedEventArg e)
		{
			cancelLoadingThread ();
			if (this.isPaged) {
				int p = e.Index / itemPerPage;
				int i = e.Index % itemPerPage;
				(itemsContainer.Children [p] as Group).DeleteChild (i);
			} else
				itemsContainer.DeleteChild (e.Index);
		}

		void Ol_ListAdd (object sender, ListChangedEventArg e)
		{
			cancelLoadingThread ();
			if (this.isPaged) {
				throw new NotImplementedException();
//				int p = e.Index / itemPerPage;
//				int i = e.Index % itemPerPage;
//				(items.Children [p] as Group).InsertChild (i, e.Element);
			} else
				loadItem (e.Element, itemsContainer, dataTest);
		}
		void Ol_ListEdit (object sender, ListChangedEventArg e) {
			if (this.isPaged) {
				throw new NotImplementedException ();
			} else if (e.Index<itemsContainer.Children.Count)
				itemsContainer.Children [e.Index].DataSource = e.Element;

		}
		void Ol_ListClear (object sender, ListClearEventArg e) {
			cancelLoadingThread ();
			if (this.isPaged) {
				throw new NotImplementedException ();
			} else {
				lock (IFace.UpdateMutex)
					itemsContainer.ClearChildren ();
			}

		}
		protected void raiseSelectedItemChanged(){
			SelectedItemChanged.Raise (this, new SelectionChangeEventArgs (SelectedItem));
		}
		public virtual void AddItem(Widget g){

			itemsContainer.AddChild (g);
			g.LogicalParent = this;
			NotifyValueChanged ("HasChildren", true);
		}
		public virtual void RemoveItem(Widget g, bool disposeChild = true)
		{
			if (disposeChild)
				itemsContainer.DeleteChild (g);
			else
				itemsContainer.RemoveChild (g);
			if (itemsContainer.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
		}

		public virtual void ClearItems()
		{
			/*selectedItemContainer = null;
			SelectedItem = null;*/

			itemsContainer.ClearChildren ();
			NotifyValueChanged ("HasChildren", false);
		}


		#region Widget overrides
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
		public override T FindByType<T> ()
		{
			if (this is T t)
				return t;

			foreach (Widget w in Items) {
				T r = w.FindByType<T> ();
				if (r != null)
					return r;
			}
			return default(T);
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
			DbgLogger.StartEvent (DbgEvtType.TGLoadingThread, this);

			try {
				loadPage (data, itemsContainer, dataTest);
			} catch (Exception ex) {
/*				while (Monitor.IsEntered (IFace.UpdateMutex))
					Monitor.Exit (IFace.UpdateMutex);
				while (Monitor.IsEntered (IFace.LayoutMutex))
					Monitor.Exit (IFace.LayoutMutex);*/
				System.Diagnostics.Debug.WriteLine ("loading thread aborted: " + ex.ToString());
			} finally {
				DbgLogger.EndEvent (DbgEvtType.TGLoadingThread);
			}
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

			DbgLogger.StartEvent (DbgEvtType.TGCancelLoadingThread, this);

			/*int updateMx = 0, layoutMx = 0;

			while (Monitor.IsEntered (IFace.UpdateMutex)) {
				Monitor.Exit (IFace.UpdateMutex);
				updateMx++;
			}
			while (Monitor.IsEntered (IFace.LayoutMutex)) {
				Monitor.Exit (IFace.LayoutMutex);
				layoutMx++;
			}*/

			loadingThread.Cancel ();

			/*for (int i = 0; i < layoutMx; i++)
				Monitor.Enter (IFace.LayoutMutex);
			for (int i = 0; i < updateMx; i++)
				Monitor.Enter (IFace.UpdateMutex);*/

			loadingThread = null;

			DbgLogger.EndEvent (DbgEvtType.TGCancelLoadingThread);
		}
		void loadPage(IEnumerable _data, Group page, string _dataTest)
		{
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
				if (loadingThread != null && loadingThread.cancelRequested)
					break;
			}

//			if (page == items)
//				return;
//			lock (CurrentInterface.LayoutMutex)
//				items.AddChild (page);

		}

		protected void loadItem(object o, Group page, string _dataTest){
			if (o == null) {//TODO:surely a threading sync problem
				DbgLogger.AddEventWithMsg (DbgEvtType.TGLoadingThread|DbgEvtType.Warning, "loadItem called with 'null' item.");
				return;
			}
			Widget g = null;
			ItemTemplate iTemp = null;
			Type dataType = o.GetType ();

			//First, a template for this item has to be choosen.

			//By default, the item template selection is done on the full type name of the item.
			//This is controled by the 'DataTest' attribute of the 'ItemTemplate' IML element.
			//Its default value is 'TypeOf'.
			string itempKey = dataType.FullName;

			//If 'DataTest' is not 'TypeOf', the item template selection will be done on the value of
			//a member of the item which name is given in the 'DataTest' attribute.
			//if item template selection is not done depending on the type of item
			//dataTest must contains a member name of the item to test for.
			if (_dataTest != "TypeOf") {
				try {
					itempKey = CompilerServices.getValue (dataType, o, _dataTest)?.ToString ();
				} catch {
					DbgLogger.AddEventWithMsg (DbgEvtType.TGLoadingThread|DbgEvtType.Warning, "dataTest fallback to full type name.");
					itempKey = dataType.FullName;//fallback to full type name
				}
			}
			if (!string.IsNullOrEmpty (itempKey) && ItemTemplates.ContainsKey (itempKey))
				iTemp = ItemTemplates [itempKey];
			else {
				if (_dataTest == "TypeOf") {//item template selection on full type name
					//search ItemTemplates for an existing parent class
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
				}
				//if no item template is found, load the local then the global default one.
				if (iTemp == null) {
					if (ItemTemplates.ContainsKey ("default"))
						iTemp = ItemTemplates ["default"];
					else
						iTemp = IFace.GetItemTemplate ("#Crow.DefaultItem.template");
				}
			}
			if (loadingThread == null)
				Monitor.Enter(IFace.UpdateMutex);
			else {
				while (!Monitor.TryEnter(IFace.UpdateMutex)) {
					if (loadingThread.cancelRequested)
						return;
					Thread.Sleep(1);
				}
			}

			try {
				g = iTemp.CreateInstance();
				#if DESIGN_MODE
				g.design_isTGItem = true;
				#endif
				page.AddChild (g);
//				if (isPaged)
				g.LogicalParent = this;
				g.MouseClick += itemClick;
			} finally {
				Monitor.Exit (IFace.UpdateMutex);
			}

			if (g is ISelectable li)
				li.Selected += Li_Selected;

			if (iTemp.Expand != null) {
				IToggle toggle = g as IToggle;

				if (toggle == null)
					toggle = g.FindByType<IToggle> ();

				if (toggle != null) {
					toggle.ToggleOn += iTemp.Expand;
					toggle.IsToggleable = iTemp.HasSubItems;
				}
			}

			g.DataSource = o;
		}

		//void expandable_expandevent (object sender, EventHandler )
		void Li_Selected (object sender, EventArgs e)
		{
			if (sender == selectedItemContainer)
				return;
			if (selectedItemContainer is ISelectable lo)
				lo.IsSelected = false;
			selectedItemContainer = sender as Widget;
			if (selectedItemContainer is ISelectable ln)
				ln.IsSelected = true;
			SelectedItem = selectedItemContainer?.DataSource;

			if (scroller != null && selectedItemContainer != null && itemsContainer is GenericStack gs) {
				Rectangle scrollerCb = scroller.ClientRectangle;
				Rectangle cb = gs.Slot;
				Rectangle rItem = selectedItemContainer.Slot + new Point (gs.Margin);
				if (gs.Orientation == Orientation.Vertical) {
					if (rItem.Y - scroller.ScrollY < 0)
						scroller.ScrollY = rItem.Y;
					else if (rItem.Bottom - scroller.ScrollY > scrollerCb.Height)
						scroller.ScrollY = rItem.Bottom - scrollerCb.Height;
				} else if (rItem.X - scroller.ScrollX < 0)
					scroller.ScrollX = rItem.X;
				else if (rItem.Right - scroller.ScrollX > scrollerCb.Width)
					scroller.ScrollX = rItem.Right - scrollerCb.Width;
			}
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
				GenericStack page1 = itemsContainer.FindByName ("page1") as GenericStack;
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

			if (sender is ISelectable nli) {
				nli.IsSelected = true;
				return;
			}
			selectedItemContainer = sender as Widget;
			if (selectedItemContainer == null)
				return;
			SelectedItem = selectedItemContainer.DataSource;
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


		void registerSubData (IEnumerable datas, string dataTest, Group itemsContainer){//, object dataParent) {
			/*if (dataParent is IValueChange vc) {

			}*/
			/*if (datas is IObservableList ol) {
				ol.ListAdd += (sender, e) => loadItem (e.Element, itemsContainer, dataTest);
				ol.ListRemove += (sender, e) => itemsContainer.DeleteChild (e.Index);
				ol.ListEdit += (sender, e) => itemsContainer.Children [e.Index].DataSource = e.Element;
				ol.ListClear += (sender, e) => {	lock (IFace.UpdateMutex)
														itemsContainer.ClearChildren ();};

			}*/
		}
		void onDatasChanged (object sender, ValueChangeEventArgs e) {

		}

		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key) {
			case Glfw.Key.Up:
				if (SelectedIndex > 0)
					SelectedIndex--;
				break;
			case Glfw.Key.Down:
				if (SelectedIndex < Items.Count - 1)
					SelectedIndex++;
				break;
			case Glfw.Key.PageUp:
				pagedSelection (true);
				break;
			case Glfw.Key.PageDown:
				pagedSelection (false);
				break;
			case Glfw.Key.Home:
				SelectedIndex = 0;
				break;
			case Glfw.Key.End:
				SelectedIndex = Items.Count - 1;
				break;
			default:
				base.onKeyDown(sender, e);
				break;
			}
		}

		void pagedSelection (bool up) {
			if (scroller != null && selectedItemContainer != null  && itemsContainer is GenericStack gs) {
				Rectangle scrollerCb = scroller.ClientRectangle;
				Rectangle itemCb = selectedItemContainer.Slot;
				int itemsPerPage = gs.Orientation == Orientation.Vertical ? scrollerCb.Height / itemCb.Height : scrollerCb.Width / itemCb.Width;
				if (up)
					SelectedIndex = Math.Max (0, SelectedIndex - itemsPerPage);
				else
					SelectedIndex = Math.Min (Items.Count - 1, SelectedIndex + itemsPerPage);
			}
		}
	}
}
