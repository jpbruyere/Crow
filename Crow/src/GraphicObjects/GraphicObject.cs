//
// GraphicObject.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cairo;
using System.Diagnostics;
using Crow.IML;
using System.Threading;


#if DESIGN_MODE
using System.Xml;
using System.IO;
#endif

namespace Crow
{
	/// <summary>
	/// The base class for all the graphic tree elements.
	/// </summary>
	public class GraphicObject : ILayoutable, IValueChange, IDisposable
	{
		internal ReaderWriterLockSlim parentRWLock = new ReaderWriterLockSlim();
		#if DEBUG_LOG
		//0 is the main graphic tree, for other obj tree not added to main tree, it range from 1->n
		//useful to track events for obj shown later, not on start, or never added to main tree
		public int treeIndex;
		public int yIndex;//absolute index in the graphic tree for debug draw
		public int xLevel;//x increment for debug draw
		#endif
		#if DESIGN_MODE
		static MethodInfo miDesignAddDefLoc = typeof(GraphicObject).GetMethod("design_add_style_location",
			BindingFlags.Instance | BindingFlags.NonPublic);
		static MethodInfo miDesignAddValLoc = typeof(GraphicObject).GetMethod("design_add_iml_location",
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
				Console.WriteLine ("default value localtion already set for {0}{1}.{2}", this.GetType().Name, this.design_id, memberName);
				return;
			}
			design_style_locations.Add(memberName, new FileLocation(path,line,col));
		}
//		internal void design_add_iml_location (string memberName, string path, int line, int col) {
//			if (design_iml_locations.ContainsKey(memberName)){
//				Console.WriteLine ("IML value localtion already set for {0}{1}.{2}", this.GetType().Name, this.design_id, memberName);
//				return;
//			}
//			design_iml_locations.Add(memberName, new FileLocation(path,line,col));
//		}
			
		public virtual bool FindByDesignID(string designID, out GraphicObject go){
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
						ctx.SetSourceSurface (bmp, 0, (LastPaintedSlot.Width-LastPaintedSlot.Height)/2);
					else
						ctx.SetSourceSurface (bmp, (LastPaintedSlot.Height-LastPaintedSlot.Width)/2, 0);
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
		~GraphicObject(){
			Debug.WriteLine(this.ToString() + " not disposed by user");
			Dispose(false);
		}
		protected virtual void Dispose(bool disposing){
			if (disposed){
				#if DEBUG_DISPOSE
				Debug.WriteLine ("Trying to dispose already disposed obj: {0}", this.ToString());
				#endif
				return;
			}

			if (disposing) {
				#if DEBUG_DISPOSE
				Debug.WriteLine ("Disposing: {0}", this.ToString());
				if (IsQueueForRedraw)
				throw new Exception("Trying to dispose an object queued for Redraw: " + this.ToString());
				#endif

				unshownPostActions ();

				if (!localDataSourceIsNull)
					DataSource = null;

				parentRWLock.EnterWriteLock();
				parent = null;
				parentRWLock.ExitWriteLock();
			} else
				Debug.WriteLine ("!!! Finalized by GC: {0}", this.ToString ());
			Clipping?.Dispose ();
			bmp?.Dispose ();
			disposed = true;
		}  
		#endregion

		#if DEBUG_LOG
		internal static List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		#endif

		internal bool isPopup = false;
		public GraphicObject focusParent {
			get { return (isPopup ? LogicalParent : parent) as GraphicObject; }
		}

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
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region CTOR
		/// <summary>
		/// default private parameter less constructor use in instantiators, it should not be used
		/// when creating widget from code because widgets has to be bound to an interface before any other
		/// action.
		/// </summary>
		protected GraphicObject () {
			Clipping = new Region ();
			#if DEBUG_LOG
			GraphicObjects.Add (this);
			DebugLog.AddEvent(DbgEvtType.GOClassCreation, this);
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
		public GraphicObject (Interface iface) : this()
		{
			IFace = iface;
			Initialize ();
		}
		#endregion
		//internal bool initialized = false;
		/// <summary>
		/// Initialize this Graphic object instance by setting style and default values and loading template if required
		/// </summary>
		public virtual void Initialize(){
			loadDefaultValues ();
		}
		#region private fields
		LayoutingType registeredLayoutings = LayoutingType.All;
		ILayoutable logicalParent;
		ILayoutable parent;
		string name;
		Fill background = Color.Transparent;
		Fill foreground = Color.White;
		Font font = "sans, 10";
		protected Measure width, height;
		int left, top;
		double cornerRadius = 0;
		int margin = 0;
		bool focusable = false;
		bool hasFocus = false;
		bool isActive = false;
		//bool isHover = false;
		bool mouseRepeat;
		protected bool isVisible = true;
		bool isEnabled = true;
		VerticalAlignment verticalAlignment = VerticalAlignment.Center;
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
		Size maximumSize = "0,0";
		Size minimumSize = "0,0";
		bool cacheEnabled = false;
		bool clipToClientRect = true;
		protected object dataSource;
		bool rootDataLevel;
		string style;
		object tag;
		bool isDragged;
		bool allowDrag;
		bool allowDrop;
		string tooltip;
		IList<Command> contextCommands;
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
		internal Size contentSize;
		#endregion

		#region ILayoutable
		[XmlIgnore]public LayoutingType RegisteredLayoutings { get { return registeredLayoutings; } set { registeredLayoutings = value; } }
		//TODO: it would save the recurent cost of a cast in event bubbling if parent type was GraphicObject
		//		or we could add to the interface the mouse events
		/// <summary>
		/// Parent in the graphic tree, used for rendering and layouting
		/// </summary>
		[XmlIgnore]public virtual ILayoutable Parent {
			get { return parent; }
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
		[XmlIgnore]public ILayoutable LogicalParent {
			get { return logicalParent == null ? Parent : logicalParent; }
			set {
				if (logicalParent == value)
					return;
				if (logicalParent != null)
					(logicalParent as GraphicObject).DataSourceChanged -= onLogicalParentDataSourceChanged;
				DataSourceChangeEventArgs dsce = new DataSourceChangeEventArgs (LogicalParent, null);
				logicalParent = value;
				dsce.NewDataSource = LogicalParent;
				if (logicalParent != null)
					(logicalParent as GraphicObject).DataSourceChanged += onLogicalParentDataSourceChanged;
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
		public virtual Rectangle ContextCoordinates(Rectangle r){
			GraphicObject go = Parent as GraphicObject;
			if (go == null)
				return r + Parent.ClientRectangle.Position;
			return go.CacheEnabled ?
				r + Parent.ClientRectangle.Position :
				Parent.ContextCoordinates (r);
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
			Point pt = p - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;
			if (pt.X < 0)
				pt.X = 0;
			if (pt.Y < 0)
				pt.Y = 0;
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
		/// <summary>Occurs when mouse enter this object</summary>
		public event EventHandler<MouseMoveEventArgs> MouseEnter;
		/// <summary>Occurs when mouse leave this object</summary>
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
		/// <summary>Occurs when mouse is over</summary>
		//public event EventHandler Hover;
		/// <summary>Occurs when this control is no longer the Hover one</summary>
		//public event EventHandler UnHover;
		/// <summary>Occurs when this object loose focus</summary>
		public event EventHandler Enabled;
		/// <summary>Occurs when the enabled state this object is set to false</summary>
		public event EventHandler Disabled;

		#region DragAndDrop Events
		public event EventHandler<DragDropEventArgs> StartDrag;
		public event EventHandler<DragDropEventArgs> DragEnter;
		public event EventHandler<DragDropEventArgs> DragLeave;
		public event EventHandler<DragDropEventArgs> EndDrag;
		public event EventHandler<DragDropEventArgs> Drop;
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
		#endregion

		#region public properties
		/// <summary>Random value placeholder</summary>
		[DesignCategory ("Divers")]
		public object Tag {
			get { return tag; }
			set {
				if (tag == value)
					return;
				tag = value;
				NotifyValueChanged ("Tag", tag);
			}
		}
		/// <summary>
		/// If enabled, resulting bitmap of graphic object is cached
		/// speeding up rendering of complex object. Default is enabled.
		/// </summary>
		[DesignCategory ("Behavior")][DefaultValue(true)]
		public virtual bool CacheEnabled {
			get { return cacheEnabled; }
			set {
				if (cacheEnabled == value)
					return;
				cacheEnabled = value;
				NotifyValueChanged ("CacheEnabled", cacheEnabled);
			}
		}
		/// <summary>
		/// If true, rendering of GraphicObject is clipped inside client rectangle
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue(true)]
		public virtual bool ClipToClientRect {
			get { return clipToClientRect; }
			set {
				if (clipToClientRect == value)
					return;
				clipToClientRect = value;
				NotifyValueChanged ("ClipToClientRect", clipToClientRect);
				this.RegisterForRedraw ();
			}
		}
		#if DEBUG_LOG
		[XmlIgnore]public string TreePath {
			get { return this.GetType().Name + GraphicObjects.IndexOf(this).ToString ();	}
		}
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
				NotifyValueChanged("Name", name);
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
				NotifyValueChanged("VerticalAlignment", verticalAlignment);
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
				NotifyValueChanged("HorizontalAlignment", horizontalAlignment);
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
				NotifyValueChanged ("Left", left);
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
				NotifyValueChanged ("Top", top);
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
					Parent is GraphicObject ? (Parent as GraphicObject).WidthPolicy :
					Measure.Stretched : width;
			}
			set {
				if (width == value)
					return;
				if (value.IsFixed) {
					if (value < minimumSize.Width || (value > maximumSize.Width && maximumSize.Width > 0))
						return;
				}
				Measure old = width;
				width = value;
				NotifyValueChanged ("Width", width);
				if (width == Measure.Stretched || old == Measure.Stretched) {
					//NotifyValueChanged ("WidthPolicy", width.Policy);
					//contentSize in Stacks are only update on childLayoutChange, and the single stretched
					//child of the stack is not counted in contentSize, so when changing size policy of a child
					//we should adapt contentSize
					//TODO:check case when child become stretched, and another stretched item already exists.
					GenericStack gs = Parent as GenericStack;
					if (gs != null){ //TODO:check if I should test Group instead
						if (gs.Orientation == Orientation.Horizontal) {
							if (width == Measure.Stretched)
								gs.contentSize.Width -= this.LastSlots.Width;
							else
								gs.contentSize.Width += this.LastSlots.Width;
						}
					}							
				}

				this.RegisterForLayouting (LayoutingType.Width);
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
					Parent is GraphicObject ? (Parent as GraphicObject).HeightPolicy :
					Measure.Stretched : height;
			}
			set {
				if (height == value)
					return;
				if (value.IsFixed) {
					if (value < minimumSize.Height || (value > maximumSize.Height && maximumSize.Height > 0))
						return;
				}
				Measure old = height;
				height = value;
				NotifyValueChanged ("Height", height);
				if (height == Measure.Stretched || old == Measure.Stretched) {
					//NotifyValueChanged ("HeightPolicy", HeightPolicy);
					GenericStack gs = Parent as GenericStack;
					if (gs != null){ //TODO:check if I should test Group instead
						if (gs.Orientation == Orientation.Vertical) {
							if (height == Measure.Stretched)
								gs.contentSize.Height -= this.LastSlots.Height;
							else
								gs.contentSize.Height += this.LastSlots.Height;
						}
					}
				}

				this.RegisterForLayouting (LayoutingType.Height);
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
			get { return focusable; }
			set {
				if (focusable == value)
					return;
				focusable = value;
				NotifyValueChanged ("Focusable", focusable);
			}
		}
		/// <summary>
		/// True when this control has the focus, only one control per interface may have it.
		/// </summary>
		[XmlIgnore]public virtual bool HasFocus {
			get { return hasFocus; }
			set {
				if (value == hasFocus)
					return;

				hasFocus = value;
				if (hasFocus)
					onFocused (this, null);
				else
					onUnfocused (this, null);
				NotifyValueChanged ("HasFocus", hasFocus);
			}
		}
		/// <summary>
		/// true if this control is active, this means that mouse has been pressed in it and not yet released. It could 
		/// be used for other two states periferic action.
		/// </summary>
		[XmlIgnore]public virtual bool IsActive {
			get { return isActive; }
			set {
				if (value == isActive)
					return;

				isActive = value;
				NotifyValueChanged ("IsActive", isActive);
			}
		}
		/// <summary>
		/// true if this control has the pointer hover
		/// </summary>
		/*[XmlIgnore]public virtual bool IsHover {
			get { return isHover; }
			set {
				if (value == isHover)
					return;

				isHover = value;

				if (isHover)
					onHover (this, null);
				else
					onUnHover (this, null);

				NotifyValueChanged ("IsHover", isHover);
			}
		}*/
		/// <summary>
		/// true if holding mouse button down should trigger multiple click events
		/// </summary>
		[DesignCategory ("Behaviour")][DefaultValue(false)]
		public virtual bool MouseRepeat {
			get { return mouseRepeat; }
			set {
				if (mouseRepeat == value)
					return;
				mouseRepeat = value;
				NotifyValueChanged ("MouseRepeat", mouseRepeat);
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
				NotifyValueChanged ("Background", background);
				RegisterForRedraw ();
				if (background is SolidColor) {
					if ((background as SolidColor).Equals (Color.Clear))
						clearBackground = true;
				}
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
				NotifyValueChanged ("Foreground", foreground);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Font being used in many controls, it is defined in the base GraphicObject class.
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue("sans, 10")]
		public virtual Font Font {
			get { return font; }
			set {
				if (value == font)
					return;
				font = value;
				NotifyValueChanged ("Font", font);
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
				NotifyValueChanged ("CornerRadius", cornerRadius);
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
				NotifyValueChanged ("Margin", margin);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// set the visible state of the control, invisible controls does reserve space in the layouting system.
		/// </summary>
		[DesignCategory ("Appearance")][DefaultValue(true)]
		public virtual bool Visible {
			get { return isVisible; }
			set {
				if (value == isVisible)
					return;

				isVisible = value;

				RegisterForLayouting (LayoutingType.Sizing);

				if (!isVisible && IFace.HoverWidget != null) {					
					if (IFace.HoverWidget.IsOrIsInside (this)) {
						//IFace.HoverWidget = null;
						IFace.ProcessMouseMove (IFace.Mouse.X, IFace.Mouse.Y);
					}
				}

				NotifyValueChanged ("Visible", isVisible);
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

				if (isEnabled)
					onEnable (this, null);
				else
					onDisable (this, null);

				NotifyValueChanged ("IsEnabled", isEnabled);
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

				NotifyValueChanged ("MinimumSize", minimumSize);
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

				NotifyValueChanged ("MaximumSize", maximumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
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

				#if DEBUG_LOG
				DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GOLockLayouting, this);
				#endif
				lock (IFace.LayoutMutex) {
					OnDataSourceChanged (this, dse);
					NotifyValueChanged ("DataSource", DataSource);
				}
				#if DEBUG_LOG
				dbgEvt.end = DebugLog.chrono.ElapsedTicks;
				#endif
			}
			get {
				return rootDataLevel ? dataSource : dataSource == null ?
					LogicalParent == null ? null :
					LogicalParent is GraphicObject ? (LogicalParent as GraphicObject).DataSource : null :
					dataSource;
			}
		}
		/// <summary>
		/// If true, rendering of GraphicObject is clipped inside client rectangle
		/// </summary>
		[DesignCategory ("Data")][DefaultValue(false)]
		public virtual bool RootDataLevel {
			get { return rootDataLevel; }
			set {
				if (rootDataLevel == value)
					return;
				rootDataLevel = value;
				NotifyValueChanged ("RootDataLevel", rootDataLevel);
				this.RegisterForRedraw ();
			}
		}
		protected virtual void onLogicalParentDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			if (localDataSourceIsNull)
				OnDataSourceChanged (this, e);
		}
		internal bool localDataSourceIsNull { get { return dataSource == null; } }
		public bool localLogicalParentIsNull { get { return logicalParent == null; } }

		public virtual void OnDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			DataSourceChanged.Raise (this, e);
			#if DEBUG_LOG
			DebugLog.AddEvent(DbgEvtType.GONewDataSource, this);
			#endif

			#if DEBUG_BINDING
			Debug.WriteLine("New DataSource for => {0} \n\t{1}=>{2}", this.ToString(),e.OldDataSource,e.NewDataSource);
			#endif
		}
		/// <summary>
		/// Style key to use for this control
		/// </summary>
		[DesignCategory ("Appearance")]
		public virtual string Style {
			get { return style; }
			set {
				if (value == style)
					return;

				style = value;

				NotifyValueChanged ("Style", style);
			}
		}
		[DesignCategory ("Divers")]
		public virtual string Tooltip {
			get { return tooltip; }
			set {
				if (tooltip == value)
					return;
				tooltip = value;
				NotifyValueChanged("Tooltip", tooltip);
			}
		}
		[DesignCategory ("Divers")]
		public IList<Command> ContextCommands {
			get { return contextCommands; }
			set {
				if (contextCommands == value)
					return;
				contextCommands = value;
				NotifyValueChanged("ContextCommands", contextCommands);
			}
		}
		#endregion

		#region Default and Style Values loading
		/// <summary> Loads the default values from XML attributes default </summary>
		public void loadDefaultValues()
		{
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GOInitialization, this);
			#endif

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
				if (IFace.Styling.ContainsKey (style)) {
					styling.Add (IFace.Styling [style]);
				}
			}
			if (IFace.Styling.ContainsKey (thisType.FullName)) {
				styling.Add (IFace.Styling [thisType.FullName]);
				if (string.IsNullOrEmpty (styleKey))
					styleKey = thisType.FullName;
			}
			if (IFace.Styling.ContainsKey (thisType.Name)) {
				styling.Add (IFace.Styling [thisType.Name]);
				if (string.IsNullOrEmpty (styleKey))
					styleKey = thisType.Name;
			}

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

            /*dm = new DynamicMethod("dyn_loadDefValues",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {CompilerServices.TObject}, thisType, true);*/

            dm = new DynamicMethod("dyn_loadDefValues", null, new Type[] {CompilerServices.TObject}, thisType, true);

            il = dm.GetILGenerator(256);
			il.DeclareLocal(CompilerServices.TObject);
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);

			foreach (EventInfo ei in thisType.GetEvents(BindingFlags.Public | BindingFlags.Instance)) {
				string expression;
				if (!getDefaultEvent(ei, styling, out expression))
					continue;
				//TODO:dynEventHandler could be cached somewhere, maybe a style instanciator class holding the styling delegate and bound to it.
				foreach (string exp in CompilerServices.splitOnSemiColumnOutsideAccolades(expression)) {
					string trimed = exp.Trim();
					if (trimed.StartsWith ("{", StringComparison.OrdinalIgnoreCase)){
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
						il.Emit (OpCodes.Call, ei.AddMethod);
					}else
						Debug.WriteLine("error in styling, event not handled : " + trimed);
				}
			}

			foreach (PropertyInfo pi in thisType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;
				XmlIgnoreAttribute xia = (XmlIgnoreAttribute)pi.GetCustomAttribute (typeof(XmlIgnoreAttribute));
				if (xia != null)
					continue;

				object defaultValue;

				int styleIndex = -1;
				if (styling.Count > 0){
					for (int i = 0; i < styling.Count; i++) {
						if (styling[i].ContainsKey (pi.Name)){
							styleIndex = i;
							break;
						}
					}
				}
				if (styleIndex >= 0){
					if (pi.PropertyType.IsEnum)//maybe should be in parser..
						defaultValue = Enum.Parse(pi.PropertyType, (string)styling[styleIndex] [pi.Name], true);
					else
						defaultValue = styling[styleIndex] [pi.Name];

					#if DESIGN_MODE
					if (defaultValue != null){
						FileLocation fl = styling[styleIndex].Locations[pi.Name];
						il.Emit (OpCodes.Ldloc_0);
						il.Emit (OpCodes.Ldstr, pi.Name);
						il.Emit (OpCodes.Ldstr, fl.FilePath);
						il.Emit (OpCodes.Ldc_I4, fl.Line);
						il.Emit (OpCodes.Ldc_I4, fl.Column);
						il.Emit (OpCodes.Call, miDesignAddDefLoc);

						il.Emit (OpCodes.Ldloc_0);
						il.Emit (OpCodes.Ldfld, typeof(GraphicObject).GetField("design_style_values"));
						il.Emit (OpCodes.Ldstr, pi.Name);
						il.Emit (OpCodes.Ldstr, defaultValue.ToString());
						il.Emit (OpCodes.Call, CompilerServices.miDicStrStrAdd);
					}
					#endif

				}else {
					DefaultValueAttribute dv = (DefaultValueAttribute)pi.GetCustomAttribute (typeof (DefaultValueAttribute));
					if (dv == null)
						continue;
					defaultValue = dv.Value;
				}

				CompilerServices.EmitSetValue (il, pi, defaultValue);
			}
			il.Emit(OpCodes.Ret);
			#endregion

			try {
				IFace.DefaultValuesLoader[styleKey] = (Interface.LoaderInvoker)dm.CreateDelegate(typeof(Interface.LoaderInvoker));
				IFace.DefaultValuesLoader[styleKey] (this);
			} catch (Exception ex) {
				throw new Exception ("Error applying style <" + styleKey + ">:", ex);
			}

			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif

			onInitialized (this, null);
		}
		protected virtual void onInitialized (object sender, EventArgs e){
			Initialized.Raise(sender, e);
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

		public virtual GraphicObject FindByName(string nameToFind){
			return string.Equals(nameToFind, name, StringComparison.Ordinal) ? this : null;
		}
		public virtual bool Contains(GraphicObject goToFind){
			return false;
		}
		/// <summary>
		/// return true if this is contained inside go
		/// </summary>
		public bool IsOrIsInside(GraphicObject go){
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
		[DesignCategory ("DragAndDrop")][DefaultValue(false)]
		public virtual bool AllowDrag {
			get { return allowDrag; }
			set {
				if (allowDrag == value)
					return;
				allowDrag = value;
				NotifyValueChanged ("AllowDrag", allowDrag);
			}
		}
		[DesignCategory ("DragAndDrop")][DefaultValue(false)]
		public virtual bool AllowDrop {
			get { return allowDrop; }
			set {
				if (allowDrop == value)
					return;
				allowDrop = value;
				NotifyValueChanged ("AllowDrop", allowDrop);
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
			get { return isDragged; }
			set {
				if (isDragged == value)
					return;
				isDragged = value;

				NotifyValueChanged ("IsDragged", IsDragged);
			}
		}
		/// <summary>
		/// fired when drag and drop operation start
		/// </summary>
		protected virtual void onStartDrag (object sender, DragDropEventArgs e){
			IFace.HoverWidget = null;
			IsDragged = true;
			StartDrag.Raise (this, e);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : START DRAG => " + e.ToString());
			#endif
		}
		/// <summary>
		///  Occured when dragging ends without dropping
		/// </summary>
		protected virtual void onEndDrag (object sender, DragDropEventArgs e){			
			IsDragged = false;
			EndDrag.Raise (this, e);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : END DRAG => " + e.ToString());
			#endif
		}
		protected virtual void onDragEnter (object sender, DragDropEventArgs e){
			e.DropTarget = this;
			DragEnter.Raise (this, e);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : DRAG Enter => " + e.ToString());
			#endif
		}
		protected virtual void onDragLeave (object sender, DragDropEventArgs e){			
			e.DropTarget = null;
			DragLeave.Raise (this, e);
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : DRAG Leave => " + e.ToString());
			#endif
		}
		protected virtual void onDrop (object sender, DragDropEventArgs e){			
			IsDragged = false;
			Drop.Raise (this, e);
			//e.DropTarget.onDragLeave (this, e);//raise drag leave in target
			#if DEBUG_DRAGNDROP
			Debug.WriteLine(this.ToString() + " : DROP => " + e.ToString());
			#endif
		}
		public bool IsDropTarget {
			get { return IFace.DragAndDropOperation?.DropTarget == this; }
		}

		#endregion

		#region Queuing
		/// <summary>
		/// Register old and new slot for clipping
		/// </summary>
		public virtual void ClippingRegistration(){
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GOClippingRegistration, this);
			#endif	
			parentRWLock.EnterReadLock ();
			if (parent != null) {					
				Parent.RegisterClip (LastPaintedSlot);
				Parent.RegisterClip (Slot);
			}
			parentRWLock.ExitReadLock ();
			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif
		}
		/// <summary>
		/// Add clip rectangle to this.clipping and propagate up to root
		/// </summary>
		/// <param name="clip">Clip rectangle</param>
		public virtual void RegisterClip(Rectangle clip){
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GORegisterClip, this);
			#endif
			Rectangle cb = ClientRectangle;
			Rectangle  r = clip + cb.Position;
			if (r.Right > cb.Right)
				r.Width -= r.Right - cb.Right;
			if (r.Bottom > cb.Bottom)
				r.Height -= r.Bottom - cb.Bottom;
			if (cacheEnabled && !IsDirty)
				Clipping.UnionRectangle (r);
			if (Parent == null)
				return;
			GraphicObject p = Parent as GraphicObject;
			if (p?.IsDirty == true && p?.CacheEnabled == true)
				return;
			Parent.RegisterClip (r + Slot.Position);
			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif
		}
		/// <summary> Full update, content and layouting, taking care of sizing policy </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForGraphicUpdate ()
		{
			IsDirty = true;
			if (Width.IsFit || Height.IsFit)
				RegisterForLayouting (LayoutingType.Sizing);
			else if (RegisteredLayoutings == LayoutingType.None)
				IFace.EnqueueForRepaint (this);
		}
		/// <summary> query an update of the content without layouting changes</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForRedraw ()
		{
			IsDirty = true;
			if (RegisteredLayoutings == LayoutingType.None)
				IFace.EnqueueForRepaint (this);
		}
		#endregion

		#region Layouting

		/// <summary> return size of content + margins </summary>
		protected virtual int measureRawSize (LayoutingType lt) {
			return lt == LayoutingType.Width ?
				contentSize.Width + 2 * margin: contentSize.Height + 2 * margin;
		}
		/// <summary> By default in groups, LayoutingType.ArrangeChildren is reset </summary>
		public virtual void ChildrenLayoutingConstraints(ref LayoutingType layoutType){
		}
		public virtual bool ArrangeChildren { get { return false; } }
		public virtual void RegisterForLayouting(LayoutingType layoutType){
			if (Parent == null)
				return;
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
				if (Parent is GraphicObject)
					(Parent as GraphicObject).ChildrenLayoutingConstraints (ref layoutType);

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
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			switch (layoutType) {
			case LayoutingType.Width:
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				RegisterForLayouting (LayoutingType.Y);
				break;
			}
			LayoutChanged.Raise (this, new LayoutingEventArgs (layoutType));
		}
		internal protected void raiseLayoutChanged(LayoutingEventArgs e){
			LayoutChanged.Raise (this, e);
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
					if (Slot.Width < minimumSize.Width) {
						Slot.Width = minimumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					} else if (Slot.Width > maximumSize.Width && maximumSize.Width > 0) {
						Slot.Width = maximumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					}
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
					if (Slot.Height < minimumSize.Height) {
						Slot.Height = minimumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					} else if (Slot.Height > maximumSize.Height && maximumSize.Height > 0) {
						Slot.Height = maximumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					}
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

		#region Rendering
		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		protected virtual void onDraw(Context gr)
		{
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GODraw, this);
			#endif

			Rectangle rBack = new Rectangle (Slot.Size);

			background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle (gr, rBack, cornerRadius);
			gr.Fill ();

			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif
		}

		/// <summary>
		/// Internal drawing context creation on a cached surface limited to slot size
		/// this trigger the effective drawing routine </summary>
		protected virtual void RecreateCache ()
		{
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GORecreateCache, this);
			#endif

			/*if (bmp == null)
				bmp = IFace.surf.CreateSimilar (Content.ColorAlpha, Slot.Width, Slot.Height);
			else if (LastPaintedSlot.Width != Slot.Width || LastPaintedSlot.Height != Slot.Height)
				bmp.SetSize (Slot.Width, Slot.Height);*/
			bmp?.Dispose ();
			bmp = new ImageSurface(Format.Argb32, Slot.Width, Slot.Height);
			
			using (Context gr = new Context (bmp)) {
				gr.Antialias = Interface.Antialias;
				onDraw (gr);
			}

			IsDirty = false;

			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif
		}
		protected virtual void UpdateCache(Context ctx){
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent(DbgEvtType.GOUpdateCacheAndPaintOnCTX, this);
			#endif

			Rectangle rb = Slot + Parent.ClientRectangle.Position;
			if (clearBackground) {
					ctx.Save ();
					ctx.Operator = Operator.Clear;
					ctx.Rectangle (rb);
					ctx.Fill ();
					ctx.Restore ();
			}

			ctx.SetSourceSurface (bmp, rb.X, rb.Y);
			ctx.Paint ();
			Clipping.Dispose ();
			Clipping = new Region ();
			#if DEBUG_LOG
			dbgEvt.end = DebugLog.chrono.ElapsedTicks;
			#endif
		}
		/// <summary> Chained painting routine on the parent context of the actual cached version
		/// of the widget </summary>
		public virtual void Paint (ref Context ctx)
		{
			#if DEBUG_LOG
			DebugLog.AddEvent(DbgEvtType.GOPaint, this);
			#endif
			//TODO:this test should not be necessary
			if (Slot.Height < 0 || Slot.Width < 0 || parent == null)
				return;
			lock (this) {
				if (cacheEnabled) {
					if (Slot.Width > Interface.MaxCacheSize || Slot.Height > Interface.MaxCacheSize)
						cacheEnabled = false;
				}

				if (cacheEnabled) {
					if (IsDirty)
						RecreateCache ();

					UpdateCache (ctx);
					if (!isEnabled)						
						paintDisabled (ctx, Slot + Parent.ClientRectangle.Position);					
				} else {
					Rectangle rb = Slot + Parent.ClientRectangle.Position;
					ctx.Save ();

					ctx.Translate (rb.X, rb.Y);

					onDraw (ctx);
					if (!isEnabled)
						paintDisabled (ctx, Slot);

					ctx.Restore ();
				}
				LastPaintedSlot = Slot;
			}
		}
		void paintDisabled(Context gr, Rectangle rb){
			gr.Operator = Operator.Xor;
			gr.SetSourceRGBA (0.6, 0.6, 0.6, 0.3);
			gr.Rectangle (rb);
			gr.Fill ();
			gr.Operator = Operator.Over;
		}
		#endregion

        #region Keyboard handling
		public virtual void onKeyDown(object sender, KeyEventArgs e){
			KeyDown.Raise (this, e);
		}
		public virtual void onKeyUp(object sender, KeyEventArgs e){
			KeyUp.Raise (this, e);
		}
		public virtual void onKeyPress(object sender, KeyPressEventArgs e){
			KeyPress.Raise (this, e);
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
			if (!(isVisible & isEnabled)||IsDragged)
				return false;
			if (!parent.PointIsIn(ref m))
				return false;
			m -= (parent.getSlot().Position + parent.ClientRectangle.Position) ;
			return Slot.ContainsOrIsEqual (m);					
		}
		public virtual bool MouseIsIn(Point m)
		{			
			return (!(isVisible & isEnabled)||IsDragged) ? false : PointIsIn (ref m);
		}
		public virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			if (IFace.HoverWidget != this) {
				IFace.HoverWidget = this;
				onMouseEnter (this, e);
			}

			//this.onMouseMove (this, e);//without this, window border doesn't work, should be removed
		}
		public virtual void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			if (allowDrag & hasFocus & e.Mouse.LeftButton == ButtonState.Pressed) {
				if (IFace.DragAndDropOperation == null) {
					IFace.DragAndDropOperation = new DragDropEventArgs (this);
					onStartDrag (this, IFace.DragAndDropOperation);
				}
			}

			//dont bubble event if dragged, mouse move is routed directely from iface
			//to let other control behind have mouse entering
			if (isDragged)
				return;
			
			//bubble event to the top
			GraphicObject p = focusParent;
			if (p != null)
				p.onMouseMove(sender,e);

			MouseMove.Raise (this, e);
		}
		public virtual void onMouseDown(object sender, MouseButtonEventArgs e){
			#if DEBUG_FOCUS
			Debug.WriteLine("MOUSE DOWN => " + this.ToString());
			#endif

			if (focusable && !Interface.FocusOnHover) {
				BubblingMouseButtonEventArg be = e as BubblingMouseButtonEventArg;
				if (be.Focused == null) {
					be.Focused = this;
					IFace.FocusedWidget = this;
					if (e.Button == MouseButton.Right && contextCommands != null)
						IFace.ShowContextMenu (this);					
				}
			}
			//bubble event to the top
			GraphicObject p = focusParent;
			if (p != null)
				p.onMouseDown(sender,e);

			MouseDown.Raise (this, e);
		}
		public virtual void onMouseUp(object sender, MouseButtonEventArgs e){
			#if DEBUG_FOCUS
			Debug.WriteLine("MOUSE UP => " + this.ToString());
			#endif

			if (IFace.DragAndDropOperation != null){
				if (IFace.DragAndDropOperation.DragSource == this) {
					if (IFace.DragAndDropOperation.DropTarget != null)
						onDrop (this, IFace.DragAndDropOperation);
					else
						onEndDrag (this, IFace.DragAndDropOperation);
					IFace.DragAndDropOperation = null;
				}
			}

			//bubble event to the top
			GraphicObject p = focusParent;
			if (p != null)
				p.onMouseUp(sender,e);

			MouseUp.Raise (this, e);
		}
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){
#if DEBUG_FOCUS
			Debug.WriteLine("CLICK => " + this.ToString());
#endif
            if (MouseClick != null)
            {
                MouseClick.Raise(this, e);
                return;
            }
			GraphicObject p = focusParent;
			if (p != null)
				p.onMouseClick(sender,e);			
		}
		public virtual void onMouseDoubleClick(object sender, MouseButtonEventArgs e){
#if DEBUG_FOCUS
			Debug.WriteLine("DOUBLE CLICK => " + this.ToString());
#endif
            if (MouseDoubleClick != null)
            {
                MouseDoubleClick.Raise(this, e);
                return;
            }
            GraphicObject p = focusParent;
			if (p != null)
				p.onMouseDoubleClick(sender,e);			
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
            if (MouseWheelChanged != null)
            {
                MouseWheelChanged.Raise(this, e);
                return;
            }
            GraphicObject p = focusParent;
			if (p != null)
				p.onMouseWheel(sender,e);
		}
		public virtual void onMouseEnter(object sender, MouseMoveEventArgs e)
		{
			#if DEBUG_FOCUS
			Debug.WriteLine("MouseEnter => " + this.ToString());
			#endif

			if (IFace.DragAndDropOperation != null) {
				GraphicObject g = this;
				while (g != null) {
					if (g.AllowDrop) {
						if (IFace.DragAndDropOperation.DragSource != this && IFace.DragAndDropOperation.DropTarget != this) {
							if (IFace.DragAndDropOperation.DropTarget != null)
								IFace.DragAndDropOperation.DropTarget.onDragLeave (this, IFace.DragAndDropOperation);
							g.onDragEnter (this, IFace.DragAndDropOperation);
						}
						break;
					}
					g = g.focusParent;
				}
			}

			MouseEnter.Raise (this, e);
		}
		public virtual void onMouseLeave(object sender, MouseMoveEventArgs e)
		{
			#if DEBUG_FOCUS
			Debug.WriteLine("MouseLeave => " + this.ToString());
			#endif

			MouseLeave.Raise (this, e);
		}

		#endregion

		protected virtual void onFocused(object sender, EventArgs e){
			if (IFace.FocusedWidget != this)
				IFace.FocusedWidget = this;
			#if DEBUG_FOCUS
			Debug.WriteLine("Focused => " + this.ToString());
			#endif
			Focused.Raise (this, e);
		}
		protected virtual void onUnfocused(object sender, EventArgs e){
			#if DEBUG_FOCUS
			Debug.WriteLine("UnFocused => " + this.ToString());
			#endif
			Unfocused.Raise (this, e);
		}
		public virtual void onEnable(object sender, EventArgs e){
			Enabled.Raise (this, e);
		}
		public virtual void onDisable(object sender, EventArgs e){
			Disabled.Raise (this, e);
		}
		protected virtual void onParentChanged(object sender, DataSourceChangeEventArgs e) {
//			if (e.NewDataSource != null) {
//				if (width == Measure.Inherit)
//					RegisterForLayouting (LayoutingType.Width);
//				if (height == Measure.Inherit)
//					RegisterForLayouting (LayoutingType.Height);
//			}
			
			ParentChanged.Raise (this, e);
			if (logicalParent == null)
				LogicalParentChanged.Raise (this, e);
		}
		protected virtual void onLogicalParentChanged(object sender, DataSourceChangeEventArgs e) {
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
		public override string ToString ()
		{
			string tmp ="";

			if (Parent != null)
				tmp = Parent.ToString () + tmp;
			#if DEBUG_LAYOUTING
			return Name == "unamed" ? tmp + "." + this.GetType ().Name + GraphicObjects.IndexOf(this).ToString(): tmp + "." + Name;
			#else
			return string.IsNullOrEmpty(Name) ? tmp + "." + this.GetType ().Name : tmp + "." + Name;
			#endif
		}
		/// <summary>
		/// Checks to handle when widget is removed from the visible graphic tree
		/// </summary>
		void unshownPostActions () {
			if (IFace.HoverWidget != null) {
				if (IFace.HoverWidget.IsOrIsInside (this)) {
					IFace.HoverWidget = null;
					IFace.ProcessMouseMove (IFace.Mouse.X, IFace.Mouse.Y);
				}
			}
			if (IFace.ActiveWidget != null) {
				if (IFace.ActiveWidget.IsOrIsInside (this))
					IFace.ActiveWidget = null;
			}
			if (IFace.FocusedWidget != null) {
				if (IFace.FocusedWidget.IsOrIsInside (this))
					IFace.FocusedWidget = null;
			}					
		}
	}
}
