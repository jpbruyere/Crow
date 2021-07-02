// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Crow.Cairo;
using System.Diagnostics;
using Crow.IML;
using System.Threading;
using Glfw;
using System.Linq;


#if DESIGN_MODE
using System.Xml;
using System.IO;
#endif

namespace Crow
{
	/// <summary>
	/// The base class for all the graphic tree elements.
	/// </summary>
	public class Widget : ILayoutable, IValueChange, IDisposable
	{
		internal ReaderWriterLockSlim parentRWLock = new ReaderWriterLockSlim();
#if DEBUG_LOG
		//0 is the main graphic tree, for other obj tree not added to main tree, it range from 1->n
		//useful to track events for obj shown later, not on start, or never added to main tree
		public int treeIndex;
		public int instanceIndex;//index in the GraphicObjects list
		public int yIndex;//absolute index in the graphic tree for debug draw
		public int xLevel;//x increment for debug draw
#endif
#if DEBUG_STATS
		public static long TotalWidgetCreated;
		public static long TotalWidgetDisposed;
		public virtual long ChildCount => 0;
#endif
#if DESIGN_MODE
		static MethodInfo miDesignAddDefLoc = typeof(Widget).GetMethod("design_add_style_location",
			BindingFlags.Instance | BindingFlags.NonPublic);
		static MethodInfo miDesignAddValLoc = typeof(Widget).GetMethod("design_add_iml_location",
			BindingFlags.Instance | BindingFlags.NonPublic);
		
		public volatile bool design_HasChanged = false;
		public string design_id;
		public int design_line;
		public int design_column;
		public string design_imlPath;
		public bool design_isTGItem = false;//true if this is a templated item's root
		public Dictionary<string,string> design_iml_values = new Dictionary<string, string>();
		public Dictionary<string,string> design_style_values = new Dictionary<string, string>();
		//public Dictionary<string,FileLocation> design_iml_locations = new Dictionary<string, FileLocation>();
		public Dictionary<string,FileLocation> design_style_locations = new Dictionary<string, FileLocation>();

		internal void design_add_style_location (string memberName, string path, int line, int col) {			
			if (design_style_locations.ContainsKey(memberName)){
				System.Diagnostics.Debug.WriteLine ("default value localtion already set for {0}{1}.{2}", this.GetType().Name, this.design_id, memberName);
				return;
			}
			design_style_locations.Add(memberName, new FileLocation(path,line,col));
		}
//		internal void design_add_iml_location (string memberName, string path, int line, int col) {
//			if (design_iml_locations.ContainsKey(memberName)){
//				System.Diagnostics.Debug.WriteLine ("IML value localtion already set for {0}{1}.{2}", this.GetType().Name, this.design_id, memberName);
//				return;
//			}
//			design_iml_locations.Add(memberName, new FileLocation(path,line,col));
//		}
			
		public virtual bool FindByDesignID(string designID, out Widget go){
			go = null;
			if (this.design_id == designID){
				go = this;
				return true;
			}
			return false;
		}

		public string GetIML(){
			XmlDocument doc = new XmlDocument( );

			using (StringWriter sw = new StringWriter ()) {
				XmlWriterSettings settings = new XmlWriterSettings {
					Indent = true,
					IndentChars = "\t",
				};
				using (XmlWriter xtw = XmlWriter.Create (sw, settings)) {
					//(1) the xml declaration is recommended, but not mandatory
					XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration ("1.0", "UTF-8", null);
					doc.InsertBefore (xmlDeclaration, null);
					getIML (doc, (XmlNode)doc);
					doc.WriteTo (xtw);
				}
				this.design_HasChanged = false;
				return sw.ToString ();
			}
		}

		public virtual void getIML(XmlDocument doc, XmlNode parentElem) {
			if (this.design_isTGItem)
				return;
			
			XmlElement xe = doc.CreateElement(this.GetType().Name);

			foreach (KeyValuePair<string,string> kv in design_iml_values) {
				XmlAttribute xa = doc.CreateAttribute (kv.Key);
				xa.Value = kv.Value;
				xe.Attributes.Append (xa);
			}

			parentElem.AppendChild (xe);
		}
		public Surface CreateIcon (int dragIconSize = 32) {
			ImageSurface di = new ImageSurface (Format.Argb32, dragIconSize, dragIconSize);
			using (Context ctx = new Context (di)) {
				double div = Math.Max (LastPaintedSlot.Width, LastPaintedSlot.Height);
				double s = (double)dragIconSize / div;
				ctx.Scale (s, s);
				if (bmp == null)
					this.onDraw (ctx);
				else {
					if (LastPaintedSlot.Width>LastPaintedSlot.Height)
						ctx.SetSource (bmp, 0, (LastPaintedSlot.Width-LastPaintedSlot.Height)/2);
					else
						ctx.SetSource (bmp, (LastPaintedSlot.Height-LastPaintedSlot.Width)/2, 0);
					ctx.Paint ();
				}
			}
			return di;
		}
        public string DesignName {
            get { return GetType ().Name + design_id; }
        }
#endif

		#region IDisposable implementation
		protected bool disposed = false;

		public void Dispose(){  
			Dispose(true);  
			GC.SuppressFinalize(this);  
		}  
		~Widget(){			
			Dispose(false);
		}
		protected virtual void Dispose(bool disposing){
			if (disposed){
				DbgLogger.AddEvent (DbgEvtType.AlreadyDisposed, this);
				return;
			}
			DbgLogger.StartEvent (DbgEvtType.Disposing, this);

			if (disposing) {
				unshownPostActions ();

				if (!localDataSourceIsNull)
					DataSource = null;

				//parentRWLock.EnterWriteLock ();
				parent = null;
				//parentRWLock.ExitWriteLock ();
			} else {
				DbgLogger.AddEvent (DbgEvtType.DisposedByGC, this);
			}

			Clipping?.Dispose ();
			bmp?.Dispose ();
			disposed = true;
#if DEBUG_STATS
			TotalWidgetDisposed++;
#endif			

			DbgLogger.EndEvent (DbgEvtType.Disposing);
		}
		#endregion

#if DEBUG_LOG
		internal static List<Widget> GraphicObjects = new List<Widget>();
#endif

		/// <summary>
		/// interface this widget is bound to, this should not be changed once the instance is created
		/// </summary>
		public Interface IFace = null;

		/// <summary>
		/// contains the dirty rectangles in the coordinate system of the cache. those dirty zones
		/// are repeated at each cached levels of the tree with correspondig coordinate system. This is done
		/// in a dedicated step of the update between layouting and drawing.
		/// </summary>
		public Region Clipping;

		#region IValueChange implementation
		/// <summary>
		/// Raise to notify that the value of a property has changed, the binding system
		/// rely mainly on this event. the member name may not be present in the class, this is 
		/// used in **propertyless** bindings, this allow to raise custom named events without needing
		/// to create an new one in the class or a new property.
		/// </summary>
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		/// <summary>
		/// Helper function to raise the value changed event
		/// </summary>
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			if (ValueChanged != null)
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		public void NotifyValueChangedAuto (object _value, [CallerMemberName] string caller = null)
		{
			if (ValueChanged != null)
				NotifyValueChanged (caller, _value);
		}
		#endregion

		#region CTOR
		/// <summary>
		/// default private parameter less constructor use in instantiators, it should not be used
		/// when creating widget from code because widgets has to be bound to an interface before any other
		/// action.
		/// </summary>
		protected Widget () {
			Clipping = new Region ();
#if DEBUG_STATS
			TotalWidgetCreated++;
#endif
#if DEBUG_LOG
			instanceIndex = GraphicObjects.Count;
			GraphicObjects.Add (this);
			DbgLogger.AddEvent(DbgEvtType.GOClassCreation, this);
#endif
		}
		/// <summary>
		/// This constructor **must** be used when creating widget from code.
		///
		/// When creating new widgets derived from GraphicObject, both parameterless and this constructors are
		/// facultatives, the compiler will create the parameterless one automaticaly if no other one exists.
		/// But if you intend to be able to create instances of the new widget in code and override the constructor
		/// with the Interface parameter, you **must** also provide the override of the parameterless constructor because
		/// compiler will not create it automatically because of the presence of the other one.
		/// </summary>
		/// <param name="iface">Iface.</param>
		public Widget (Interface iface, string style = null) : this()
		{
			this.style = style;
			IFace = iface;
			Initialize ();
		}
		#endregion
		//internal bool initialized = false;
		/// <summary>
		/// Initialize this Graphic object instance by setting style and default values and loading template if required
		/// </summary>
		public void Initialize(){
			loadDefaultValues ();
		}
		#region private fields
		LayoutingType registeredLayoutings;// = LayoutingType.Sizing;
		ILayoutable logicalParent;
		ILayoutable parent;
		string name;
		Fill background = Colors.Transparent;
		Fill foreground = Colors.White;
		Font font = "sans, 10";
		protected Measure width, height;
		int left, top;
		double cornerRadius;
		int margin;
		bool focusable ;
		bool hasFocus;
		bool isActive;
		bool isHover;
		bool bubbleMouseEvent;
		bool mouseRepeat;
		bool stickyMouseEnabled;
		int stickyMouse;
		MouseCursor mouseCursor = MouseCursor.top_left_arrow;
		protected bool isVisible = true;
		bool isEnabled = true;
		VerticalAlignment verticalAlignment = VerticalAlignment.Center;
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
		Size maximumSize = "0,0";
		Size minimumSize = "0,0";
		bool cacheEnabled;
		bool clipToClientRect = true;
		Type dataSourceType;
		protected object dataSource;
		bool rootDataLevel;
		string style;
		object tag;
		bool isDragged;
		bool allowDrag;
		bool allowDrop;
		string allowedDropTypes;
		string tooltip;
		CommandGroup contextCommands;
		#endregion

		#region public fields
		/// <summary>
		/// Current size and position computed during layouting pass
		/// </summary>
		public Rectangle Slot = new Rectangle ();
		/// <summary>
		/// keep last slot components for each layouting pass to track
		/// changes and trigger update of other component accordingly
		/// </summary>
		public Rectangle LastSlots;
		/// <summary>
		/// keep last slot painted on screen to clear traces if moved or resized
		/// version to clear effective oldslot if parents have been moved or resized.
		/// IDEA is to add a ScreenCoordinates function that use only lastPaintedSlots
		/// </summary>
		//TODO: we should ensure the whole parsed widget tree is the last painted
		public Rectangle LastPaintedSlot;
		/// <summary>Prevent requeuing multiple times the same widget</summary>
		public bool IsQueueForClipping = false;
		/// <summary>drawing Cache, if null, a redraw is done, cached or not</summary>
		public Surface bmp;
		public bool IsDirty = true;
		/// <summary>
		/// This size is computed on each child' layout changes.
		/// In stacking widget, it is used to compute the remaining space for the stretched
		/// widget inside the stack, which is never added to the contentSize, instead, its size
		/// is deducted from (parent.ClientRectangle - contentSize)
		/// </summary>
		public Size contentSize;
		#endregion

		#region ILayoutable
		[XmlIgnore]public LayoutingType RegisteredLayoutings { get => registeredLayoutings; set => registeredLayoutings = value; }
		//TODO: it would save the recurent cost of a cast in event bubbling if parent type was GraphicObject
		//		or we could add to the interface the mouse events
		/// <summary>
		/// Parent in the graphic tree, used for rendering and layouting
		/// </summary>
		[XmlIgnore]public virtual ILayoutable Parent {
			get => parent;
			set {
				if (parent == value)
					return;
				DataSourceChangeEventArgs e = new DataSourceChangeEventArgs (parent, value);

				parentRWLock.EnterWriteLock();
				parent = value;
				Slot = LastSlots = default(Rectangle);
				parentRWLock.ExitWriteLock();
									
				onParentChanged (this, e);
			}
		}
		/// <summary>
		/// Mouse routing need to go back to logical parent for popups
		/// </summary>
		internal Widget FocusParent => (parent is Interface ? LogicalParent : parent) as Widget; 

		[XmlIgnore]public ILayoutable LogicalParent {
			get { return logicalParent == null ? Parent : logicalParent; }
			set {
				if (logicalParent == value)
					return;
				if (logicalParent is Widget)
					(logicalParent as Widget).DataSourceChanged -= onLogicalParentDataSourceChanged;
				DataSourceChangeEventArgs dsce = new DataSourceChangeEventArgs (LogicalParent, null);
				logicalParent = value;
				dsce.NewDataSource = LogicalParent;
				if (logicalParent is Widget)
					(logicalParent as Widget).DataSourceChanged += onLogicalParentDataSourceChanged;
				onLogicalParentChanged (this, dsce);
			}
		}
		[XmlIgnore]public virtual Rectangle ClientRectangle {
			get {
				Rectangle cb = Slot.Size;
				cb.Inflate ( - margin);
				return cb;
			}
		}
		/// <summary>
		/// Compute rectangle position on surface of the context. It ma be the first cached surface in parenting chain,
		/// or the top backend surface if no cached widget is part of the current widget tree.
		/// </summary>
		/// <returns>A new rectangle with same dimension as the input one with x and y relative to the context surface</returns>
		/// <param name="r">A rectangle to compute the coordinate for.</param>
		public virtual Rectangle ContextCoordinates(Rectangle r){
			return Parent is Widget w ?
				w.CacheEnabled ?
					r + Parent.ClientRectangle.Position : Parent.ContextCoordinates (r)
				: Parent != null ? r + Parent.ClientRectangle.Position : r;
		}

		public virtual Rectangle RelativeSlot (Widget target)
		{
			if (this == target)
				return Slot;
			if (Parent is Widget p)
				return Slot + p.RelativeSlot (target).Position + Margin;
			return Slot + new Point(Margin, Margin);
		}
		public virtual Rectangle ScreenCoordinates (Rectangle r){
			try {
				return
					Parent.ScreenCoordinates(r) + Parent.getSlot().Position + Parent.ClientRectangle.Position;				
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				return default(Rectangle);
			}
		}
		public virtual Rectangle getSlot () { return Slot;}
		#endregion
		public Point ScreenPointToLocal(Point p){
			Point pt = p - ScreenCoordinates (Slot).TopLeft - ClientRectangle.TopLeft;
			/*if (pt.X < 0)
				pt.X = 0;
			if (pt.Y < 0)
				pt.Y = 0;*/
			return pt;
		}

		#region EVENT HANDLERS
		/// <summary>Occurs when mouse wheel is rolled in this object. It bubbles to the root</summary>
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		/// <summary>Occurs when mouse button is released in this object. It bubbles to the root</summary>
		public event EventHandler<MouseButtonEventArgs> MouseUp;
		/// <summary>Occurs when mouse button is pressed in this object. It bubbles to the root</summary>
		public event EventHandler<MouseButtonEventArgs> MouseDown;
		/// <summary>Occurs when mouse button has been pressed then relesed in this object. It bubbles to the root</summary>
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		/// <summary>Occurs when mouse button has been pressed then relesed 2 times in this object. It bubbles to the root</summary>
		public event EventHandler<MouseButtonEventArgs> MouseDoubleClick;
		/// <summary>Occurs when mouse mouve in this object. It bubbles to the root</summary>
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		/// <summary>Occurs when mouse enter the bounding rectangle of this object</summary>
		public event EventHandler<MouseMoveEventArgs> MouseEnter;
		/// <summary>Occurs when mouse leave the bounds of this object</summary>
		public event EventHandler<MouseMoveEventArgs> MouseLeave;
		/// <summary>Occurs when key is pressed when this object is active</summary>
		public event EventHandler<KeyEventArgs> KeyDown;
		/// <summary>Occurs when key is released when this object is active</summary>
		public event EventHandler<KeyEventArgs> KeyUp;
		/// <summary>Occurs when translated key event occurs in the host when this object is active</summary>
		public event EventHandler<KeyPressEventArgs> KeyPress;
		/// <summary>Occurs when this object received focus</summary>
		public event EventHandler Focused;
		/// <summary>Occurs when this object loose focus</summary>
		public event EventHandler Unfocused;
		/// <summary>Occurs when this widget is hovered by the mouse</summary>
		public event EventHandler Hover;
		/// <summary>Occurs when this widget is no longuer the hover one</summary>
		public event EventHandler Unhover;
		/// <summary>Occurs when this widget is enabled</summary>
		public event EventHandler Enabled;
		/// <summary>Occurs when the enabled state this object is set to false</summary>
		public event EventHandler Disabled;

		#region DragAndDrop Events
		public event EventHandler<DragDropEventArgs> StartDrag;
		public event EventHandler<DragDropEventArgs> DragEnter;
		public event EventHandler<DragDropEventArgs> DragLeave;
		public event EventHandler<DragDropEventArgs> EndDrag;
		public event EventHandler<DragDropEventArgs> Drop;
		public event EventHandler<MouseMoveEventArgs> Drag;
		#endregion

		/// <summary>
		/// Occurs when default value and styling are loaded, and for templated control,
		/// template is also loaded. Bindings should be functionnal as well.
		/// </summary>
		public event EventHandler Initialized;

		/// <summary>Occurs when one part of the rendering slot changed</summary>
		public event EventHandler<LayoutingEventArgs> LayoutChanged;
		/// <summary>Occurs when DataSource changed</summary>
		public event EventHandler<DataSourceChangeEventArgs> DataSourceChanged;
		/// <summary>Occurs when the parent has changed</summary>
		public event EventHandler<DataSourceChangeEventArgs> ParentChanged;
		/// <summary>Occurs when the logical parent has changed</summary>
		public event EventHandler<DataSourceChangeEventArgs> LogicalParentChanged;
		public event EventHandler Painted;
		#endregion

		internal bool hasDoubleClick => MouseDoubleClick != null;
		internal bool hasClick => MouseClick != null;

		#region public properties
		/// <summary>Random value placeholder</summary>
		[DesignCategory ("Divers")]
		public object Tag {
			get { return tag; }
			set {
				if (tag == value)
					return;
				tag = value;
				NotifyValueChangedAuto (tag);
			}
		}
		/// <summary>
		/// If enabled, resulting bitmap of graphic object is cached
		/// speeding up rendering of complex object. Default is enabled.
		/// </summary>
		[DesignCategory ("Behavior")][DefaultValue(false)]
		public virtual bool CacheEnabled {
			get => cacheEnabled;
			set {
				if (cacheEnabled == value)
					return;
				cacheEnabled = value;
				NotifyValueChangedAuto (cacheEnabled);
			}
		}
		/// <summary>
		/// If true, rendering of GraphicObject is clipped inside client rectangle
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue(true)]
		public virtual bool ClipToClientRect {
			get => clipToClientRect;
			set {
				if (clipToClientRect == value)
					return;
				clipToClientRect = value;
				NotifyValueChangedAuto (clipToClientRect);
				this.RegisterForRedraw ();
			}
		}
#if DEBUG_LOG
		[XmlIgnore]public string TreePath => this.GetType().Name + GraphicObjects.IndexOf(this).ToString ();
#endif
		/// <summary>
		/// Name is used in binding to reference other GraphicObjects inside the graphic tree
		/// and by template controls to find special element in their template implementation such
		/// as a container or a group to put children in.
		/// </summary>
		[DesignCategory ("Divers")][DefaultValue(null)]
		public virtual string Name {
			get {
#if DEBUG_LOG
				return string.IsNullOrEmpty(name) ? this.GetType().Name + GraphicObjects.IndexOf(this).ToString () : name;
#else
				return name;
#endif
			}
			set {
				if (name == value)
					return;
				name = value;
				NotifyValueChangedAuto (name);
			}
		}
		/// <summary>
		/// Vertical alignment inside parent, disabled if height is stretched
		/// or top coordinate is not null
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(VerticalAlignment.Center)]
		public virtual VerticalAlignment VerticalAlignment {
			get { return verticalAlignment; }
			set {
				if (verticalAlignment == value)
					return;

				verticalAlignment = value;
				NotifyValueChangedAuto (verticalAlignment);
				RegisterForLayouting (LayoutingType.Y);
			}
		}
		/// <summary>
		/// Horizontal alignment inside parent, disabled if width is stretched
		/// or left coordinate is not null
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(HorizontalAlignment.Center)]
		public virtual HorizontalAlignment HorizontalAlignment {
			get { return horizontalAlignment; }
			set {
				if (horizontalAlignment == value)
					return;
				horizontalAlignment = value;
				NotifyValueChangedAuto (horizontalAlignment);
				RegisterForLayouting (LayoutingType.X);
			}
		}
		/// <summary>
		/// x position inside parent
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(0)]
		public virtual int Left {
			get { return left; }
			set {
				if (left == value)
					return;
				left = value;
				NotifyValueChangedAuto (left);
				this.RegisterForLayouting (LayoutingType.X);
			}
		}
		/// <summary>
		/// y position inside parent
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(0)]
		public virtual int Top {
			get { return top; }
			set {
				if (top == value)
					return;
				top = value;
				NotifyValueChangedAuto (top);
				this.RegisterForLayouting (LayoutingType.Y);
			}
		}
		/// <summary>
		/// Helper property used to set width and height to fit in one call
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(false)]
		public virtual bool Fit {
			get { return Width == Measure.Fit && Height == Measure.Fit ? true : false; }
			set {
				if (value == Fit)
					return;

				Width = Height = Measure.Fit;
			}
		}
		/// <summary>
		/// Width of this control, by default inherited from parent. May have special values
		/// such as Stretched or Fit. It may be proportionnal or absolute.
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue("Inherit")]
		public virtual Measure Width {
			get {
				return width.Units == Unit.Inherit ?
					Parent is Widget ? (Parent as Widget).WidthPolicy :
					Measure.Stretched : width;
			}
			set {
				if (width == value)
					return;
				if (value.IsFixed) {
					if (value < minimumSize.Width || (maximumSize.Width > 0 && value > maximumSize.Width))
						return;
				}
				width = value;
				NotifyValueChangedAuto (width);
				RegisterForLayouting (LayoutingType.Width);
			}
		}
		/// <summary>
		/// Height of this control, by default inherited from parent. May have special values
		/// such as Stretched or Fit. It may be proportionnal or absolute.
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue("Inherit")]
		public virtual Measure Height {
			get {
				return height.Units == Unit.Inherit ?
					Parent is Widget ? (Parent as Widget).HeightPolicy :
					Measure.Stretched : height;
			}
			set {
				if (height == value)
					return;
				if (value.IsFixed) {
					if (value < minimumSize.Height || (maximumSize.Height > 0 && value > maximumSize.Height))
						return;
				}
				height = value;
				NotifyValueChangedAuto (height);
				RegisterForLayouting (LayoutingType.Height);
			}
		}
		/// <summary>
		/// Was Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched, **with inherited state implementation, it is not longer used in binding**
		/// </summary>
		[XmlIgnore]public virtual Measure WidthPolicy { get {
				return Width.IsFit ? Measure.Fit : Measure.Stretched; } }
		/// <summary>
		/// Was Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched, **with inherited state implementation, it is not longer used in binding**
		/// </summary>
		[XmlIgnore]public virtual Measure HeightPolicy { get {
				return Height.IsFit ? Measure.Fit : Measure.Stretched; } }
		/// <summary>
		/// Indicate that this object may received focus or not, if not focusable all the descendants are 
		/// affected.
		/// </summary>
		[DesignCategory ("Behaviour")][DefaultValue(false)]
		public virtual bool Focusable {
			get => focusable;
			set {
				if (focusable == value)
					return;
				focusable = value;
				NotifyValueChangedAuto (focusable);
			}
		}
		/// <summary>
		/// True when this control has the focus, only one control per interface may have it.
		/// </summary>
		[XmlIgnore]public virtual bool HasFocus {
			get => hasFocus;
			set {
				if (value == hasFocus)
					return;

				hasFocus = value;
				if (hasFocus) {
					IFace.FocusedWidget = this;
					onFocused (this, null);
				} else
					onUnfocused (this, null);
				NotifyValueChangedAuto (hasFocus);
			}
		}		
		/// <summary>
		/// true if this control is active, this means that mouse has been pressed in it and not yet released. It could 
		/// be used for other two states periferic action.
		/// </summary>
		[XmlIgnore]public virtual bool IsActive {
			get => isActive;
			internal set {
				if (value == isActive)
					return;

				isActive = value;
				NotifyValueChangedAuto (isActive);
			}
		}
		/// <summary>
		/// true if this control has the pointer hover
		/// </summary>
		[XmlIgnore]public virtual bool IsHover {
			get => isHover;
			internal set {
				if (value == isHover)
					return;

				if (isHover & !value)
					Unhover.Raise (this, null);

				isHover = value;

				if (isHover) {
					if (stickyMouseEnabled && stickyMouse > 0) 
						IFace.stickedWidget = this;											
					Hover.Raise (this, null);
				}

				NotifyValueChangedAuto (isHover);
			}
		}
		/// <summary>
		/// if false, prevent mouse events to bubble to the parent in any case.
		/// </summary>
		[DesignCategory ("Behaviour")]
		[DefaultValue (true)]
		public virtual bool BubbleMouseEvent {
			get => bubbleMouseEvent;
			set {
				if (bubbleMouseEvent == value)
					return;
				bubbleMouseEvent = value;
				NotifyValueChangedAuto (bubbleMouseEvent);
			}
		}
		/// <summary>
		/// true if holding mouse button down should trigger multiple click events
		/// </summary>
		[DesignCategory ("Behaviour")][DefaultValue(false)]
		public virtual bool MouseRepeat {
			get => mouseRepeat;
			set {
				if (mouseRepeat == value)
					return;
				mouseRepeat = value;
				NotifyValueChangedAuto (mouseRepeat);
			}
		}
		/// <summary>
		/// When StickyMouse value is greater than zero and StickyMouseEnabled is true, mouse will be sticked over the widget
		/// until x or y delta is greater than the StickyMouse value. This is usefulle for very thin (1 pixel) border that need to
		/// be grabbed with the mouse.
		/// </summary>
		public virtual int StickyMouse {
			get => stickyMouse;
			set {
				if (stickyMouse == value)
					return;
				stickyMouse = value;
				NotifyValueChangedAuto (stickyMouse);
            }
        }
		/// <summary>
		/// Boolean for enabling or not the sticky mouse mechanic
		/// </summary>
 		[DesignCategory ("Behaviour")][DefaultValue(false)]
		public virtual bool StickyMouseEnabled {
			get => stickyMouseEnabled;
			set {
				if (stickyMouseEnabled == value)
					return;
				stickyMouseEnabled = value;
				NotifyValueChangedAuto (stickyMouseEnabled);
            }
        }
		/// <summary>
		/// Determine Cursor when mouse is Hover.
		/// </summary>
		[DesignCategory ("Behaviour")]
		[DefaultValue (MouseCursor.top_left_arrow)]
		public virtual MouseCursor MouseCursor {
			get { return mouseCursor; }
			set {
				if (mouseCursor == value)
					return;
				mouseCursor = value;
				NotifyValueChangedAuto (mouseCursor);
				this.RegisterForRedraw ();

				if (Focusable && IsHover)
					IFace.MouseCursor = mouseCursor;
			}
		}

		bool clearBackground = false;
		/// <summary>
		/// background fill of the control, maybe solid color, gradient, image, or svg
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue("Transparent")]
		public virtual Fill Background {
			get { return background; }
			set {
				if (background == value)
					return;
				clearBackground = false;
				if (value == null)
					return;
				background = value;
				NotifyValueChangedAuto (background);
				RegisterForRedraw ();
				if (background is SolidColor sc && sc.Equals (Colors.Clear))
					clearBackground = true;				
			}
		}
		/// <summary>
		/// Foreground fill of the control, usage may be different among derived controls
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue("White")]
		public virtual Fill Foreground {
			get { return foreground; }
			set {
				if (foreground == value)
					return;
				foreground = value;
				NotifyValueChangedAuto (foreground);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Font being used in many controls, it is defined in the base GraphicObject class.
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue("sans, 12")]
		public virtual Font Font {
			get { return font; }
			set {
				if (value == font)
					return;
				font = value;
				NotifyValueChangedAuto (font);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// to get rounded corners
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue(0.0)]
		public virtual double CornerRadius {
			get { return cornerRadius; }
			set {
				if (value == cornerRadius)
					return;
				cornerRadius = value;
				NotifyValueChangedAuto (cornerRadius);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// This is a single integer for the 4 direction, a gap between the control and it's container,
		/// by default it is filled with the background.
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue(0)]
		public virtual int Margin {
			get { return margin; }
			set {
				if (value == margin)
					return;
				margin = value;
				NotifyValueChangedAuto (margin);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// set the visible state of the control, invisible controls does reserve space in the layouting system.
		/// </summary>
		[Obsolete][DesignCategory ("Appearance")][DefaultValue(true)]
		public virtual bool Visible {
			get => IsVisible;
			set => IsVisible = value;
		}
		/// <summary>
		/// set the visible state of the control, invisible controls does reserve space in the layouting system.
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue(true)]
		public virtual bool IsVisible {
			get => isVisible; 
			set {
				if (value == isVisible)
					return;

				isVisible = value;				
				
				/*if (!isVisible)
					unshownPostActions ();
				RegisterForLayouting (LayoutingType.Sizing);*/

				if (isVisible){										
					IsDirty = true;
				} else {
					unshownPostActions ();					
				}
				RegisterForLayouting(LayoutingType.Sizing);				

				NotifyValueChangedAuto (isVisible);
			}
		}
		/// <summary>
		/// get or set the enabled state, disabling a control will affect focuability and
		/// also it's rendering which will be grayed
		/// </summary>
		[DesignCategory ("Behaviour")][DefaultValue(true)]
		public virtual bool IsEnabled {
			get { return isEnabled; }
			set {
				if (value == isEnabled)
					return;

				isEnabled = value;

				if (IsEnabled)
					onEnable (this, null);
				else
					onDisable (this, null);

				NotifyValueChangedAuto (IsEnabled);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Minimal width and  height for this control
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue("1,1")]
		public virtual Size MinimumSize {
			get { return minimumSize; }
			set {
				if (value == minimumSize)
					return;

				minimumSize = value;

				NotifyValueChangedAuto (minimumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		/// <summary>
		/// Maximum width and  height for this control, unlimited if null.
		/// </summary>
		[DesignCategory ("Layout")][DefaultValue("0,0")]
		public virtual Size MaximumSize {
			get { return maximumSize; }
			set {
				if (value == maximumSize)
					return;

				maximumSize = value;

				NotifyValueChangedAuto (maximumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		/// <summary>
		/// Fully qualify type name of expected data source.
		/// If set, datasource bindings will be speedup by avoiding reflexion in generated dyn methods.
		/// If an object of a different type is set as datasource, bindings will be canceled.
		/// It accepts all derived type.
		/// </summary>
		[DesignCategory ("Data")]
		public Type DataSourceType {
			get => dataSourceType;
			set { dataSourceType = value; }
		}
		/// <summary>
		/// Seek first logical tree upward if logicalParent is set, or seek graphic tree for
		/// a not null dataSource that will be active for all descendants having dataSource=null
		/// </summary>
		[DesignCategory ("Data")]
		public virtual object DataSource {
			set {
				if (DataSource == value)
					return;

				DataSourceChangeEventArgs dse = new DataSourceChangeEventArgs (DataSource, null);
				dataSource = value;
				dse.NewDataSource = DataSource;

				if (dse.NewDataSource == dse.OldDataSource)
					return;

				if (value != null)
					rootDataLevel = true;

				/*DbgLogger.StartEvent(DbgEvtType.GOLockUpdate, this);
				lock (IFace.UpdateMutex) {*/
					OnDataSourceChanged (this, dse);
					NotifyValueChangedAuto (DataSource);
				/*}
				DbgLogger.EndEvent (DbgEvtType.GOLockUpdate);*/
			}
			get {
				return rootDataLevel ? dataSource : dataSource == null ?
					LogicalParent == null ? null :
					LogicalParent is Widget w ? w.DataSource : null :
					dataSource;
			}
		}
		/// <summary>
		/// If true, lock datasource seeking upward in logic or graphic tree to this widget.
		/// </summary>
		[DesignCategory ("Data")][DefaultValue(false)]
		public virtual bool RootDataLevel {
			get { return rootDataLevel; }
			set {
				if (rootDataLevel == value)
					return;
				rootDataLevel = value;
				NotifyValueChangedAuto (rootDataLevel);
				this.RegisterForRedraw ();
			}
		}
		protected virtual void onLogicalParentDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			if (localDataSourceIsNull)
				OnDataSourceChanged (this, e);
		}
		internal bool localDataSourceIsNull => dataSource == null;
		public bool localLogicalParentIsNull => logicalParent == null;

		public virtual void OnDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			DataSourceChanged.Raise (this, e);
			#if DEBUG_LOG
			DbgLogger.AddEvent(DbgEvtType.GONewDataSource, this);
			#endif

			#if DEBUG_BINDING
			Console.WriteLine("New DataSource for => {0} \n\t{1}=>{2}", this.ToString(),e.OldDataSource,e.NewDataSource);
			#endif
		}
		/// <summary>
		/// Style key to use for this control
		/// </summary>
		[DesignCategory ("Appearance")]
		public virtual string Style {
			get => style;
			set {
				if (value == style)
					return;

				style = value;

				NotifyValueChangedAuto (style);
			}
		}
		/// <summary>
		/// Gets or sets a tooltip to show when mouse stay still over the control.
		/// </summary>
		/// <remarks>
		/// By default, the tooltip container widget that will be shown is defined in '#Crow.Tooltip.template' and the widget
		/// tooltip string is interpreted as a single string helper message that may be a binding expression.
		/// If the widget Tooltip property start with a '#', the tooltip string will be interpreted as a resource path of
		/// a custom IML template to show, which will have its datasource set to the widget triggering the tooltip.
		/// </remarks>
		/// <value>A single helpt string that may comes from a binding expression, or by starting with a '#',
		/// You may provide a custom tooltip template resource path.</value>
		[DesignCategory ("Divers")]
		public virtual string Tooltip {
			get { return tooltip; }
			set {
				if (tooltip == value)
					return;
				tooltip = value;
				NotifyValueChangedAuto (tooltip);
			}
		}
		[DesignCategory ("Divers")]
		public CommandGroup ContextCommands {
			get => contextCommands;
			set {
				if (contextCommands == value)
					return;
				contextCommands = value;
				NotifyValueChangedAuto (contextCommands);
			}
		}
		#endregion

		#region Default and Style Values loading
		/// <summary> Loads the default values from XML attributes default </summary>
		public void loadDefaultValues()
		{
			DbgLogger.StartEvent (DbgEvtType.GOInitialization, this);

			Type thisType = this.GetType ();

			if (!string.IsNullOrEmpty (style)) {
				if (IFace.DefaultValuesLoader.ContainsKey (style)) {
					IFace.DefaultValuesLoader [style] (this);
					onInitialized (this, null);
					return;
				}
			} else if (IFace.DefaultValuesLoader.ContainsKey (thisType.FullName)) {
				IFace.DefaultValuesLoader [thisType.FullName] (this);
				onInitialized (this, null);
				return;
			} else 	if (IFace.DefaultValuesLoader.ContainsKey (thisType.Name)) {
				IFace.DefaultValuesLoader [thisType.Name] (this);
				onInitialized (this, null);
				return;
			}

			List<Style> styling = new List<Style>();

			//Search for a style matching :
			//1: Full class name, with full namespace
			//2: class name
			//3: style may have been registered with their ressource ID minus .style extention
			//   those files being placed in a Styles folder
			string styleKey = style;
			if (!string.IsNullOrEmpty (style)) {
				if (IFace.Styling.ContainsKey (style))
					styling.Add (IFace.Styling [style]);				
			}
			//check the whole type hierarchy for styling
			Type styleType = thisType;
			do {
				if (IFace.Styling.ContainsKey (styleType.FullName)) {
					styling.Add (IFace.Styling [styleType.FullName]);
					/*if (string.IsNullOrEmpty (styleKey))
						styleKey = thisType.FullName;*/
				}
				if (IFace.Styling.ContainsKey (styleType.Name)) {
					styling.Add (IFace.Styling [styleType.Name]);
					/*if (string.IsNullOrEmpty (styleKey))
						styleKey = thisType.Name;*/
				}
				styleType = styleType.BaseType;
			} while (styleType != null);

			if (string.IsNullOrEmpty (styleKey))
				styleKey = thisType.FullName;

			//Reflexion being very slow compared to dyn method or delegates,
			//I compile the initial values coded in the CustomAttribs of the class,
			//all other instance of this type would not longer use reflexion to init properly
			//but will fetch the  dynamic initialisation method compiled for this precise type
			//TODO:measure speed gain.
			#region Delfault values Loading dynamic compilation
			DynamicMethod dm = null;
			ILGenerator il = null;

			dm = new DynamicMethod("dyn_loadDefValues", null, new Type[] { typeof (object) }, thisType, true);

            il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof (object));//store root
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);

			foreach (EventInfo ei in thisType.GetEvents(BindingFlags.Public | BindingFlags.Instance)) {
				string expression;
				if (!getDefaultEvent(ei, styling, out expression))
					continue;
				//TODO:dynEventHandler could be cached somewhere, maybe a style instanciator class holding the styling delegate and bound to it.
				foreach (string exp in expression.Split (';')) {					
				//foreach (string exp in CompilerServices.splitOnSemiColumnOutsideAccolades(expression)) {
					
					string trimed = exp.Trim();
					if (trimed.StartsWith ("{", StringComparison.Ordinal)){
						il.Emit (OpCodes.Ldloc_0);//load this as 1st arg of event Add

						//push eventInfo as 1st arg of compile
						il.Emit (OpCodes.Ldloc_0);
						il.Emit (OpCodes.Call, CompilerServices.miGetType);
						il.Emit (OpCodes.Ldstr, ei.Name);//push event name
						il.Emit (OpCodes.Call, CompilerServices.miGetEvent);
						//push expression as 2nd arg of compile
						il.Emit (OpCodes.Ldstr, trimed.Substring (1, trimed.Length - 2));
						//push null as 3rd arg, currentNode, not known when instanciing
						il.Emit (OpCodes.Ldnull);
						il.Emit (OpCodes.Call, CompilerServices.miCompileDynEventHandler);
						il.Emit (OpCodes.Castclass, ei.EventHandlerType);
						il.Emit (OpCodes.Callvirt, ei.AddMethod);
					}else
						Debug.WriteLine("error in styling, event not handled : " + trimed);
				}
			}

			//first set template if it exists
			PropertyInfo piTmp = thisType.GetProperty ("Template");
			if (piTmp != null) {
				//if template has been declared in IML, cancel style or default loading
				System.Reflection.Emit.Label cancelTemplateLoad = il.DefineLabel ();
				il.Emit (OpCodes.Ldloc_0);//load target widget
				il.Emit (OpCodes.Ldfld, typeof (PrivateContainer).GetField ("child", BindingFlags.Instance | BindingFlags.NonPublic));
				il.Emit (OpCodes.Brtrue, cancelTemplateLoad);

				setDefaultValue (il, piTmp, ref styling);

				il.MarkLabel (cancelTemplateLoad);
			}

			foreach (PropertyInfo pi in thisType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.Name == "Template")
					continue;
				setDefaultValue (il, pi, ref styling);
			}
			il.Emit(OpCodes.Ret);
			#endregion

			//try {
				IFace.DefaultValuesLoader[styleKey] = (Interface.LoaderInvoker)dm.CreateDelegate(typeof(Interface.LoaderInvoker));
				IFace.DefaultValuesLoader[styleKey] (this);
			/*} catch (Exception ex) {
				throw new Exception ("Error applying style <" + styleKey + ">:", ex);
			}*/
			onInitialized (this, null);
		}
		void setDefaultValue (ILGenerator il, PropertyInfo pi, ref List<Style> styling)
		{
			if (pi.GetSetMethod () == null)
				return;
			XmlIgnoreAttribute xia = pi.GetCustomAttribute <XmlIgnoreAttribute>();
			if (xia != null)
				return;

			object defaultValue;

			int styleIndex = -1;
			if (styling.Count > 0) {
				for (int i = 0; i < styling.Count; i++) {
					if (styling [i].ContainsKey (pi.Name)) {
						styleIndex = i;
						break;
					}
				}
			}
			if (styleIndex >= 0) {
				if (pi.PropertyType.IsEnum)//maybe should be in parser..
					defaultValue = Enum.Parse (pi.PropertyType, (string)styling [styleIndex] [pi.Name], true);
				else
					defaultValue = styling [styleIndex] [pi.Name];

#if DESIGN_MODE
				if (defaultValue != null) {
					FileLocation fl = styling [styleIndex].Locations [pi.Name];
					il.Emit (OpCodes.Ldloc_0);
					il.Emit (OpCodes.Ldstr, pi.Name);
					il.Emit (OpCodes.Ldstr, fl.FilePath);
					il.Emit (OpCodes.Ldc_I4, fl.Line);
					il.Emit (OpCodes.Ldc_I4, fl.Column);
					il.Emit (OpCodes.Call, miDesignAddDefLoc);

					il.Emit (OpCodes.Ldloc_0);
					il.Emit (OpCodes.Ldfld, typeof (Widget).GetField ("design_style_values"));
					il.Emit (OpCodes.Ldstr, pi.Name);
					il.Emit (OpCodes.Ldstr, defaultValue.ToString ());
					il.Emit (OpCodes.Call, CompilerServices.miDicStrStrAdd);
				}
#endif

			} else {
				DefaultValueAttribute dv = (DefaultValueAttribute)pi.GetCustomAttribute (typeof (DefaultValueAttribute));
				if (dv == null)
					return;
				defaultValue = dv.Value;
			}

			CompilerServices.EmitSetValue (il, pi, defaultValue);
		}

		protected virtual void onInitialized (object sender, EventArgs e){
			Initialized.Raise(sender, e);
			DbgLogger.EndEvent (DbgEvtType.GOInitialization);
		}
		bool getDefaultEvent(EventInfo ei, List<Style> styling,
			out string expression){
			expression = "";
			if (styling.Count > 0){
				for (int i = 0; i < styling.Count; i++) {
					if (styling[i].ContainsKey (ei.Name)){
						expression = (string)styling[i] [ei.Name];
						return true;
					}
				}
			}
			return false;
		}
#endregion

		public virtual Widget FindByName(string nameToFind)
			=> string.Equals(nameToFind, name, StringComparison.Ordinal) ? this : null;		
		public virtual T FindByType<T> () //where T : Widget
			=> this is T t? t : default(T);		
		public virtual bool Contains(Widget goToFind){
			return false;
		}
		/// <summary>
		/// return true if this is contained inside go
		/// </summary>
		public bool IsOrIsInside(Widget go){
			if (this == go)
				return true;
			ILayoutable p = this.Parent;
			while (p != null) {
				if (p == go)
					return true;
				p = p.Parent;
			}
			return false;
		}

		#region Drag&Drop
		/// <summary>
		/// If true, allow widget to be dragged and dropped.
		/// </summary>
		[DesignCategory ("DragAndDrop")][DefaultValue(false)]
		public virtual bool AllowDrag {
			get => allowDrag;
			set {
				if (allowDrag == value)
					return;
				allowDrag = value;
				NotifyValueChanged ("AllowDrag", allowDrag);
			}
		}
		/// <summary>
		/// If true, allow widgets of type listed in 'AllowedDropTypes' to be dropped in this widget
		/// during drag and drop operations.
		/// </summary>
		[DesignCategory ("DragAndDrop")][DefaultValue(false)]
		public virtual bool AllowDrop {
			get => allowDrop;
			set {
				if (allowDrop == value)
					return;
				allowDrop = value;
				NotifyValueChanged ("AllowDrop", allowDrop);
			}
		}		
		/// <summary>
		/// Semicolon separated list of accepted types as dropped widget.
		/// </summary>
		[DesignCategory ("DragAndDrop")][DefaultValue ("Crow.Widget")]
		public virtual string AllowedDropTypes {
			get => allowedDropTypes;
			set {
				if (allowedDropTypes == value)
					return;
				allowedDropTypes = value;
				NotifyValueChanged ("AllowedDropTypes", allowedDropTypes);
			}
		}
		//		public List<Type> AllowedDroppedTypes;
		//		public void AddAllowedDroppedType (Type newType){
		//			if (AllowedDroppedTypes == null)
		//				AllowedDroppedTypes = new List<Type> ();
		//			AllowedDroppedTypes.Add (newType);
		//			NotifyValueChanged ("AllowDrop", AllowDrop);
		//		}
		//		[XmlIgnore]public virtual bool AllowDrop {
		//			get { return AllowedDroppedTypes?.Count>0; }
		//		}
		[XmlIgnore]public virtual bool IsDragged {
			get => isDragged;
			set {
				if (isDragged == value)
					return;
				isDragged = value;

				NotifyValueChanged ("IsDragged", IsDragged);
			}
		}
		public bool AcceptDrop (Widget droppedWidget) =>
			string.IsNullOrEmpty(AllowedDropTypes) || droppedWidget == null ? false :
			AllowedDropTypes.Split (';').Contains (droppedWidget.GetType ().FullName);
		/// <summary>
		/// equivalent to mouse move for a dragged widget, no bubbling.
		/// </summary>
		public virtual void onDrag (object sender, MouseMoveEventArgs e) {
			if (Drag != null)
				Drag.Invoke (this, e);
#if DEBUG_DRAGNDROP
			Debug.WriteLine (this.ToString () + " : DRAG => " + e.ToString ());
#endif
		}

		/// <summary>
		/// fired when drag and drop operation start
		/// </summary>
		protected virtual void onStartDrag (object sender, DragDropEventArgs e){
			IFace.DragAndDropOperation = e;
			IFace.dragndropHover = e.DropTarget;
			IsDragged = true;
			StartDrag.Raise (this, IFace.DragAndDropOperation);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : START DRAG => " + e.ToString());			
			#endif
		}
		protected virtual void onDragEnter (object sender, DragDropEventArgs e){			
			e.DropTarget = this;			
			DragEnter.Raise (this, e);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : DRAG Enter => " + e.ToString());
			#endif
		}
		public virtual void onDragLeave (object sender, DragDropEventArgs e){			
			DragLeave.Raise (this, e);
#if DEBUG_DRAGNDROP
			Debug.WriteLine (this.ToString () + " : DRAG Leave => " + e.ToString ());
#endif
			e.DropTarget = null;
		}
		/// <summary>
		///  Occured when dragging ends without dropping
		/// </summary>
		public virtual void onEndDrag (object sender, DragDropEventArgs e) {
			IsDragged = false;
			EndDrag.Raise (this, e);
#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : END DRAG => " + e.ToString());
#endif
		}
		public virtual void onDrop (object sender, DragDropEventArgs e){			
			IsDragged = false;
			Drop.Raise (this, e);
			//e.DropTarget.onDragLeave (this, e);//raise drag leave in target
#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : DROP => " + e.ToString());
#endif
		}
		public bool IsDropTarget => IFace.DragAndDropOperation?.DropTarget == this;		
		#endregion

		#region Queuing
		/// <summary>
		/// Register old and new slot for clipping
		/// </summary>
		public virtual void ClippingRegistration(){
			DbgLogger.StartEvent (DbgEvtType.GOClippingRegistration, this);

			parentRWLock.EnterReadLock ();
			if (parent != null) {					
				parent.RegisterClip (LastPaintedSlot);
				parent.RegisterClip (Slot);
			}//else
				//Console.WriteLine ($"clipping reg canceled (no parent): {this.ToString()}");
			parentRWLock.ExitReadLock ();

			DbgLogger.EndEvent (DbgEvtType.GOClippingRegistration);
		}
		/// <summary>
		/// Add clip rectangle to this.clipping and propagate up to root
		/// </summary>
		/// <param name="clip">Clip rectangle</param>
		public virtual void RegisterClip(Rectangle clip){
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.AlreadyDisposed | DbgEvtType.GORegisterClip, this);
				return;
			}
			//we register clip in the parent, if it's dirty, all children will be redrawn
			if (IsDirty && CacheEnabled) {
				//Console.WriteLine ($"regclip canceled Dirty:{IsDirty} Cached:{CacheEnabled}: {this.ToString()}");
				return;			
			}
			DbgLogger.StartEvent(DbgEvtType.GORegisterClip, this);
			try {
				Rectangle cb = ClientRectangle;
				Rectangle  r = clip + cb.Position;
				/*if (r.Right > cb.Right)
					r.Width -= r.Right - cb.Right;
				if (r.Bottom > cb.Bottom)
					r.Height -= r.Bottom - cb.Bottom;*/
				if (r.Width < 0 || r.Height < 0){
					//Console.WriteLine ($"regclip canceled size w:{r.Width} h:{r.Height}: {this.ToString()}");
					return;			
				}
				if (cacheEnabled)
					Clipping.UnionRectangle (r);
				if (Parent == null){
					//Console.WriteLine ($"clip chain aborded (no parent): {this.ToString()}");
					return;			
				}
				/*Widget p = Parent as Widget;
				if (p?.IsDirty == true && p?.CacheEnabled == true){
					Console.WriteLine ($"parent.regclip canceled p.Dirty:{p?.IsDirty} Cached:{p?.CacheEnabled}: {this.ToString()}");
					return;			
				}*/
				Parent.RegisterClip (r + Slot.Position);
			} finally {
				DbgLogger.EndEvent (DbgEvtType.GORegisterClip);
			}
		}
		/// <summary> Full update, if width or height is 'Fit' a layouting is requested, and a redraw is done in any case. </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForGraphicUpdate ()
		{
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.GORegisterForGraphicUpdate | DbgEvtType.AlreadyDisposed, this);
				return;
			}
			DbgLogger.StartEvent(DbgEvtType.GORegisterForGraphicUpdate, this);

			IsDirty = true;			
			if (Width.IsFit || Height.IsFit)
				RegisterForLayouting (LayoutingType.Sizing);
			else if (RegisteredLayoutings == LayoutingType.None)
				IFace.EnqueueForRepaint (this);

			DbgLogger.EndEvent(DbgEvtType.GORegisterForGraphicUpdate);
		}
		/// <summary> query an update of the content without layouting changes</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForRedraw ()
		{
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.GORegisterForRedraw | DbgEvtType.AlreadyDisposed, this);
				return;
			}
			IsDirty = true;
			if (RegisteredLayoutings == LayoutingType.None)
				IFace.EnqueueForRepaint (this);
		}
		/// <summary>
		/// query a repaint, if control is cached, cache will not be updated and simply repainted.
		/// if not cached, repaint will trigger the onDraw method.
		/// </summary>
		/// <remark>
		/// This could be usefull in widget with complex drawing, that need some markers on top: the main part
		/// of the drawing could take place in the onDraw method, and the markers (single line, rectangle, ...)
		/// could be drawn in the Paint method. Such widget must have 'CacheEnabled=true' and to simply update the
		/// markers without a full redraw, just call 'RegisterForRepaint'.
		/// 
		/// </remark>
		public void RegisterForRepaint () {
			if (RegisteredLayoutings == LayoutingType.None && !IsDirty)
				IFace.EnqueueForRepaint (this);
		}
		#endregion

		#region Layouting

		/// <summary> return size of content + margins </summary>
		public virtual int measureRawSize (LayoutingType lt) {
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);

			int tmp = lt == LayoutingType.Width ?
				contentSize.Width + 2 * margin: contentSize.Height + 2 * margin;

			DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			return tmp;
		}

		internal bool firstUnresolvedFitWidth (out  Widget ancestorInUnresolvedFit)
		{
			ancestorInUnresolvedFit = this.Parent as Widget;

			while (ancestorInUnresolvedFit != null) {
				if (ancestorInUnresolvedFit.width.IsFit)
					return true;
				if (!ancestorInUnresolvedFit.Width.IsRelativeToParent || ancestorInUnresolvedFit.Parent is Interface)
					return false;
				ancestorInUnresolvedFit = ancestorInUnresolvedFit.Parent as Widget;
			}
			return false;
		}

		public virtual bool ArrangeChildren { get { return false; } }
		/// <summary>
		/// Used to prevent some layouting type in children. For example, in the GenericStack,
		/// x layouting is dismissed in the direction of the stacking to let the parent
		/// arrange children in the x direction.
		/// </summary>
		/// <param name="layoutable">The children that is calling the constraints</param>
		/// <param name="layoutType">The currently registering layouting types</param>		
		public virtual void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType){	}
		/// <summary> Query a layouting for the type pass as parameter, redraw only if layout changed. </summary>
		public virtual void RegisterForLayouting(LayoutingType layoutType){
			if (disposed) {
				DbgLogger.AddEvent (DbgEvtType.AlreadyDisposed, this);
				return;
			}

			if (Parent == null)
				return;
			DbgLogger.StartEvent (DbgEvtType.GOLockLayouting, this);
			try {
				lock (IFace.LayoutMutex) {
					//prevent queueing same LayoutingType for this
					layoutType &= (~RegisteredLayoutings);

					if (layoutType == LayoutingType.None)
						return;
					
					//dont set position for stretched item
					if (Width == Measure.Stretched)
						layoutType &= (~LayoutingType.X);
					if (Height == Measure.Stretched)
						layoutType &= (~LayoutingType.Y);

					if (!ArrangeChildren)
						layoutType &= (~LayoutingType.ArrangeChildren);

					//apply constraints depending on parent type
					Parent.ChildrenLayoutingConstraints (this, ref layoutType);

	//				//prevent queueing same LayoutingType for this
					layoutType &= (~RegisteredLayoutings);

					if (layoutType == LayoutingType.None)
						return;

					//enqueue LQI LayoutingTypes separately
					if (layoutType.HasFlag (LayoutingType.Width))
						IFace.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Width, this));
					if (layoutType.HasFlag (LayoutingType.Height))
						IFace.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Height, this));
					if (layoutType.HasFlag (LayoutingType.X))
						IFace.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.X, this));
					if (layoutType.HasFlag (LayoutingType.Y))
						IFace.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Y, this));
					if (layoutType.HasFlag (LayoutingType.ArrangeChildren))
						IFace.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.ArrangeChildren, this));
				}
			} finally {
				DbgLogger.EndEvent (DbgEvtType.GOLockLayouting);
			}
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:
				/*if (Parent is Widget p) {
					if (p.Width.IsFit)
						p.RegisterForLayouting (LayoutingType.Width);
				}*/
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				/*if (Parent is Widget pp) {
					if (pp.Height.IsFit)
						pp.RegisterForLayouting (LayoutingType.Height);
				}*/
				RegisterForLayouting (LayoutingType.Y);
				break;
			}
			if (LayoutChanged != null)
				LayoutChanged.Invoke (this, new LayoutingEventArgs (layoutType));
		}
		internal protected void raiseLayoutChanged(LayoutingType layoutingType){
			if (LayoutChanged != null)
				LayoutChanged.Raise (this, new LayoutingEventArgs(layoutingType));
		}
		/// <summary> Update layout component only one at a time, this is where the computation of alignement
		/// and size take place.
		/// The redrawing will only be triggered if final slot size has changed </summary>
		/// <returns><c>true</c>, if layouting was possible, <c>false</c> if conditions were not
		/// met and LQI has to be re-queued</returns>
		public virtual bool UpdateLayout (LayoutingType layoutType)
		{
			//unset bit, it would be reset if LQI is re-queued
			registeredLayoutings &= (~layoutType);

			switch (layoutType) {
			case LayoutingType.X:
				if (left == 0) {

					if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Width) ||
					    RegisteredLayoutings.HasFlag (LayoutingType.Width))
						return false;

					switch (horizontalAlignment) {
					case HorizontalAlignment.Left:
						Slot.X = 0;
						break;
					case HorizontalAlignment.Right:
						Slot.X = Parent.ClientRectangle.Width - Slot.Width;
						break;
					case HorizontalAlignment.Center:
						Slot.X = Parent.ClientRectangle.Width / 2 - Slot.Width / 2;
						break;
					}
				} else
					Slot.X = left;

				if (LastSlots.X == Slot.X)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				LastSlots.X = Slot.X;
				break;
			case LayoutingType.Y:
				if (top == 0) {

					if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Height) ||
					    RegisteredLayoutings.HasFlag (LayoutingType.Height))
						return false;

					switch (verticalAlignment) {
					case VerticalAlignment.Top://this could be processed even if parent Height is not known
						Slot.Y = 0;
						break;
					case VerticalAlignment.Bottom:
						Slot.Y = Parent.ClientRectangle.Height - Slot.Height;
						break;
					case VerticalAlignment.Center:
						Slot.Y = Parent.ClientRectangle.Height / 2 - Slot.Height / 2;
						break;
					}
				} else
					Slot.Y = top;

				if (LastSlots.Y == Slot.Y)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				LastSlots.Y = Slot.Y;
				break;
			case LayoutingType.Width:
				if (isVisible) {
					if (Width.IsFixed)
						Slot.Width = Width;
					else if (Width == Measure.Fit) {
						Slot.Width = measureRawSize (LayoutingType.Width);
					} else if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Width))
						return false;
					else if (Width == Measure.Stretched)
						Slot.Width = Parent.ClientRectangle.Width;
					else
  						Slot.Width = (int)Math.Round ((double)(Parent.ClientRectangle.Width * Width) / 100.0);

					if (Slot.Width < 0)
						return false;

					//size constrain
					if (Slot.Width < minimumSize.Width)
						Slot.Width = minimumSize.Width;						
					else if (maximumSize.Width > 0 && Slot.Width > maximumSize.Width)
						Slot.Width = maximumSize.Width;
					
				} else
					Slot.Width = 0;

				if (LastSlots.Width == Slot.Width)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				LastSlots.Width = Slot.Width;
				break;
			case LayoutingType.Height:
				if (isVisible) {
					if (Height.IsFixed)
						Slot.Height = Height;
					else if (Height == Measure.Fit) {
						Slot.Height = measureRawSize (LayoutingType.Height);
					} else if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Height))
						return false;
					else if (Height == Measure.Stretched)
						Slot.Height = Parent.ClientRectangle.Height;
					else
						Slot.Height = (int)Math.Round ((double)(Parent.ClientRectangle.Height * Height) / 100.0);

					if (Slot.Height < 0)
						return false;

					//size constrain
					if (Slot.Height < minimumSize.Height)
						Slot.Height = minimumSize.Height;
					 else if (maximumSize.Height > 0 && Slot.Height > maximumSize.Height)
						Slot.Height = maximumSize.Height;
					
				} else
					Slot.Height = 0;

				if (LastSlots.Height == Slot.Height)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				LastSlots.Height = Slot.Height;
				break;
			}

			//if no layouting remains in queue for item, registre for redraw
			if (this.registeredLayoutings == LayoutingType.None && IsDirty)
				IFace.EnqueueForRepaint (this);

			return true;
		}
		#endregion

		protected void setFontForContext (Context gr) {
			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;
		}

		#region Rendering
		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		protected virtual void onDraw(Context gr)
		{			
			DbgLogger.StartEvent(DbgEvtType.GODraw, this);

			Rectangle rBack = new Rectangle (Slot.Size);

			background.SetAsSource (IFace, gr, rBack);
			CairoHelpers.CairoRectangle (gr, rBack, cornerRadius);
			gr.Fill ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}

		/// <summary>
		/// Internal drawing context creation on a cached surface limited to slot size
		/// this trigger the effective drawing routine </summary>
		protected virtual void RecreateCache ()
		{
			DbgLogger.StartEvent (DbgEvtType.GORecreateCache, this);

			/*if (bmp == null)
				bmp = IFace.surf.CreateSimilar (Content.ColorAlpha, Slot.Width, Slot.Height);
			else if (LastPaintedSlot.Width != Slot.Width || LastPaintedSlot.Height != Slot.Height)
				bmp.SetSize (Slot.Width, Slot.Height);*/
			bmp?.Dispose ();
			bmp = IFace.surf.CreateSimilar (Content.ColorAlpha, Slot.Width, Slot.Height);
			//bmp = new ImageSurface(Format.Argb32, Slot.Width, Slot.Height);

			using (Context gr = new Context (bmp)) {
				gr.Antialias = Interface.Antialias;
				onDraw (gr);
			}

			IsDirty = false;			

			DbgLogger.EndEvent (DbgEvtType.GORecreateCache);
		}
		protected void paintCache(Context ctx, Rectangle rb) {
			DbgLogger.StartEvent(DbgEvtType.GOPaintCache, this);	
			if (clearBackground) {
				ctx.Operator = Operator.Clear;
				ctx.Rectangle (rb);
				ctx.Fill ();
				ctx.Operator = Operator.Over;
			}

			ctx.SetSource (bmp, rb.X, rb.Y);
			ctx.Paint ();
			DbgLogger.EndEvent(DbgEvtType.GOPaintCache);	
		}
		protected virtual void UpdateCache(Context ctx){
			DbgLogger.StartEvent(DbgEvtType.GOUpdateCache, this);			
			paintCache (ctx, Slot + Parent.ClientRectangle.Position);
			DbgLogger.AddEvent (DbgEvtType.GOResetClip, this);
			Clipping.Reset ();			
			DbgLogger.EndEvent (DbgEvtType.GOUpdateCache);
		}
		/// <summary> Chained painting routine on the parent context of the actual cached version
		/// of the widget </summary>
		public virtual void Paint (Context ctx)
		{
			/*if (!IsVisible)
				return;*/

			DbgLogger.StartEvent (DbgEvtType.GOPaint, this);

			//TODO:this test should not be necessary

			if (disposed || Slot.Height < 0 || Slot.Width < 0 || parent == null){
#if DEBUG
				Console.ForegroundColor = ConsoleColor.Red;
				if (disposed)
					System.Diagnostics.Debug.WriteLine ($"Paint disposed widget: {this}");
				Console.ForegroundColor = ConsoleColor.DarkRed;
				if (Slot.Height < 0 || Slot.Width < 0)
					System.Diagnostics.Debug.WriteLine ($"Paint slot invalid ({Slot}): {this}");
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				if (parent == null)
					System.Diagnostics.Debug.WriteLine ($"Paint with parent == null: {this}");
				Console.ForegroundColor = ConsoleColor.Magenta;
				if (!isVisible)
					System.Diagnostics.Debug.WriteLine ($"Paint invisible widget: {this}");
				Console.ResetColor ();
#endif
				DbgLogger.AddEvent (DbgEvtType.Warning);
				DbgLogger.EndEvent (DbgEvtType.GOPaint);
				return; 
			}
			//lock (this) {
				if (cacheEnabled) {
					if (Slot.Width > Interface.MaxCacheSize || Slot.Height > Interface.MaxCacheSize)
						cacheEnabled = false;
				}				

				if (cacheEnabled) {
					if (IsDirty) {
						RecreateCache ();
						paintCache (ctx, Slot + Parent.ClientRectangle.Position);
					}else
						UpdateCache (ctx);
					if (!IsEnabled)						
						paintDisabled (ctx, Slot + Parent.ClientRectangle.Position);					
				} else {
					Rectangle rb = Slot + Parent.ClientRectangle.Position;
					ctx.Save ();

					ctx.Translate (rb.X, rb.Y);

					onDraw (ctx);

					ctx.Restore ();

					if (!IsEnabled)
						paintDisabled (ctx, rb);

				}				

				LastPaintedSlot = Slot;
			//}
			Painted.Raise (this, null);

			DbgLogger.EndEvent (DbgEvtType.GOPaint);
		}
		void paintDisabled(Context gr, Rectangle rb){
			//gr.Operator = Operator.Xor;
			gr.SetSource (0.2, 0.2, 0.2, 0.8);
			gr.Rectangle (rb);
			gr.Fill ();
			//gr.Operator = Operator.Over;
		}
		#endregion

        #region Keyboard handling
		public virtual void onKeyDown(object sender, KeyEventArgs e){
			if (KeyDown != null)
				KeyDown.Invoke (this, e);
			else if (!e.Handled)
				FocusParent?.onKeyDown (sender, e);
		}
		public virtual void onKeyUp(object sender, KeyEventArgs e){
			if (KeyUp != null)
				KeyUp.Invoke (this, e);
			else if (!e.Handled)
				FocusParent?.onKeyUp (sender, e);
		}
		public virtual void onKeyPress(object sender, KeyPressEventArgs e){
			if (KeyPress != null)
				KeyPress.Invoke (this, e);
			else if (!e.Handled)
				FocusParent?.onKeyPress (sender, e);
		}
        #endregion

		#region Mouse handling
		/// <summary>
		/// Recursive local coordinate point test.
		/// After test on parent, point m is in local coord system.
		/// </summary>
		/// <returns>return true, if point is in the bounds of this control</returns>
		/// <param name="m">by ref point to test, init value is not kept</param>
		public virtual bool PointIsIn(ref Point m)
		{
			if (parent == null)
				return false;
			if (!(isVisible & IsEnabled))
				return false;
			if (!parent.PointIsIn(ref m))
				return false;
			m -= (parent.getSlot().Position + parent.ClientRectangle.Position) ;
			return Slot.ContainsOrIsEqual (m);					
		}
		public virtual bool MouseIsIn(Point m)
			=> (!(isVisible & IsEnabled) || IsDragged) ? false : PointIsIn (ref m);
		public virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			if (IFace.DragAndDropInProgress) {
				if (IFace.dragndropHover != this) {
					IFace.dragndropHover = this;
#if DEBUG_DRAGNDROP
					Debug.WriteLine($"DragNDropHover = {this.ToString()} AllowDrop:{AllowDrop}, {IFace.DragAndDropOperation.DragSource.AllowedDropTypes}");			
#endif

					if (AllowDrop && AcceptDrop (IFace.DragAndDropOperation.DragSource))
						onDragEnter (this, IFace.DragAndDropOperation);
				}
			} else if (IFace.HoverWidget != this) {
				onMouseEnter (this, e);
				IFace.HoverWidget = this;
			}			
		}
		public virtual void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			if (AllowDrag & IFace.IsDown (MouseButton.Left)) {				
				onStartDrag (this, new DragDropEventArgs (this, FocusParent));
				return;
			}

			if (MouseMove != null)
				MouseMove.Invoke (this, e);			
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseMove (sender, e);
		}
		/// <summary>
		/// Default mouse button press method. The `MouseDown` event is raised from withing it.
		/// </summary>
		/// <remarks>
		/// See `CrowEventArgs` for details on interface event handling.
		/// </remarks>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">mouse button pressed event arguments</param>
		public virtual void onMouseDown(object sender, MouseButtonEventArgs e){
			if (Focusable) {
				IFace.FocusedWidget = this;
				e.Handled = true;
			}

			if (e.Button == MouseButton.Right && contextCommands != null) {
				IFace.ShowContextMenu (this);
				e.Handled = true;
			}

			if (MouseDown != null)
				MouseDown?.Invoke (this, e);
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseDown (sender, e);
		}
		/// <summary>
		/// Default mouse button release method. The `MouseUp` event is raised from withing it.
		/// </summary>
		/// <remarks>
		/// See `CrowEventArgs` for details on interface event handling.
		/// </remarks>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">mouse button release event arguments</param>
		public virtual void onMouseUp(object sender, MouseButtonEventArgs e){

			if (MouseUp != null)
				MouseUp.Invoke (this, e);
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseUp (sender, e);
		}
		/// <summary>
		/// Default mouse click method. A click is a press and release without mouving combination.
		/// </summary>
		/// <param name="sender">The Sender of the event</param>
		/// <param name="e">event arguments</param>
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){
			if (MouseClick != null)
				MouseClick.Invoke (this, e);
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseClick (sender, e);
		}
		/// <summary>
		/// Default mouse double click method. A double click is two consecutive press and release without mouving combination.
		/// within a delay defined by `Interface.DOUBLECLICK_TRESHOLD`
		/// </summary>
		/// <param name="sender">The Sender of the event</param>
		/// <param name="e">event arguments</param>
		public virtual void onMouseDoubleClick(object sender, MouseButtonEventArgs e){
			if (MouseDoubleClick != null)			
				MouseDoubleClick.Invoke (this, e);
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseDoubleClick (sender, e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
			if (MouseWheelChanged != null)
				MouseWheelChanged.Invoke (this, e);
			else if (!e.Handled && BubbleMouseEvent)
				FocusParent?.onMouseWheel (sender, e);
		}
		public virtual void onMouseEnter(object sender, MouseMoveEventArgs e)
		{
			IFace.MouseCursor = MouseCursor;
			/*if (IFace.DragAndDropOperation != null) {
				Widget g = this;
				while (g != null) {
					if (g.AllowDrop) {
						if (IFace.DragAndDropOperation.DragSource != this && IFace.DragAndDropOperation.DropTarget != this) {
							if (IFace.DragAndDropOperation.DropTarget != null)
								IFace.DragAndDropOperation.DropTarget.onDragLeave (this, IFace.DragAndDropOperation);
							g.onDragEnter (this, IFace.DragAndDropOperation);
						}
						break;
					}
					g = g.FocusParent;
				}
			}*/

			MouseEnter.Raise (this, e);
		}
		public virtual void onMouseLeave(object sender, MouseMoveEventArgs e)
		{			
			MouseLeave.Raise (this, e);
		}

		#endregion

		protected virtual void onFocused(object sender, EventArgs e){
			DbgLogger.AddEvent (DbgEvtType.FocusedWidget, this);			
			Focused.Raise (this, e);
		}
		protected virtual void onUnfocused(object sender, EventArgs e){
			DbgLogger.AddEvent (DbgEvtType.UnfocusedWidget, this);
			Unfocused.Raise (this, e);
		}

		public virtual void onEnable(object sender, EventArgs e){
			Enabled.Raise (this, e);
		}
		public virtual void onDisable(object sender, EventArgs e){
			Disabled.Raise (this, e);
		}
		protected virtual void onParentChanged(object sender, DataSourceChangeEventArgs e) {
			DbgLogger.AddEvent (DbgEvtType.GONewParent, this, e);
			ParentChanged.Raise (this, e);
			if (logicalParent == null)
				LogicalParentChanged.Raise (this, e);
		}
		protected virtual void onLogicalParentChanged(object sender, DataSourceChangeEventArgs e) {
			DbgLogger.AddEvent (DbgEvtType.GONewLogicalParent, this, e);
			LogicalParentChanged.Raise (this, e);
		}
		internal void ClearTemplateBinding(){
#if DEBUG_UPDATE
			Debug.WriteLine (string.Format("ClearTemplateBinding: {0}", this.ToString()));
#endif
			if (ValueChanged == null)
				return;
			EventInfo eiEvt = this.GetType().GetEvent ("ValueChanged");
			foreach (Delegate d in ValueChanged.GetInvocationList()) {
				if (d.Method.Name == "dyn_tmpValueChanged") {
					eiEvt.RemoveEventHandler (this, d);
					#if DEBUG_BINDING
					Debug.WriteLine ("\t{0} template binding handler removed in {1} for: {2}", d.Method.Name, this, "ValueChanged");
					#endif
				}
			}
		}
		protected virtual string LogName => GetType().Name;
		public override string ToString ()
		{
			string tmp ="";

			if (Parent != null)
				tmp = Parent.ToString () + tmp;			
			return string.IsNullOrEmpty(Name) ? tmp + "." + LogName : tmp + "." + Name;
			//#endif
		}
		/// <summary>
		/// Checks to handle when widget is removed from the visible graphic tree
		/// </summary>
		void unshownPostActions () {
			IsDirty = true;			

			/*if (parent is Widget p)
				p.RegisterForGraphicUpdate();
			else*/
			try
			{
				if (parent != null)
					parent.RegisterClip (ContextCoordinates(LastPaintedSlot));
			}
			catch (System.Exception e)
			{
				Debug.WriteLine($"[ERR]:unshownPostActions:{e}");
			}
				

			if (IFace.ActiveWidget != null) {
				if (IFace.ActiveWidget.IsOrIsInside (this))
					IFace.ActiveWidget = null;
			}
			if (IFace.FocusedWidget != null) {
				if (IFace.FocusedWidget.IsOrIsInside (this))
					IFace.FocusedWidget = null;
			}
			if (IFace.HoverWidget != null) {
				if (IFace.HoverWidget.IsOrIsInside (this)) {
					Widget w = IFace.HoverWidget;
					MouseMoveEventArgs e = new MouseMoveEventArgs (IFace.MousePosition.X, IFace.MousePosition.Y, 0, 0);
					while (w != this) {
						w.onMouseLeave (this, e);
						w = w.FocusParent;
					}
					this.onMouseLeave (this, e);
					IFace.HoverWidget = null;
					IFace.OnMouseMove (IFace.MousePosition.X, IFace.MousePosition.Y);
				}
			}
			
			/*Slot = default;
			try
			{
				if (LastSlots.Width > 0)
					OnLayoutChanges (LayoutingType.Width);
				if (LastSlots.Height > 0)
					OnLayoutChanges (LayoutingType.Height);
				OnLayoutChanges (LayoutingType.X);
				OnLayoutChanges (LayoutingType.Y);				
			}
			catch (System.Exception ex)
			{
				Console.WriteLine($"[ERROR]Unshown post actions: {ex}");
			}
			LastSlots = default;
			LastPaintedSlot = default;*/
			//Slot = LastSlots = default;			
		}
	}
}
