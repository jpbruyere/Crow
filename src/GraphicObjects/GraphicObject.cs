using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cairo;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Crow
{
	public class GraphicObject : IXmlSerializable, ILayoutable, IValueChange, ICloneable, IBindable
	{
		#region IBindable implementation
		List<Binding> bindings = new List<Binding> ();
		public List<Binding> Bindings {
			get { return bindings; }
		}

		#endregion

		internal static ulong currentUid = 0;
		internal ulong uid = 0;

		Interface currentInterface = null;

		public Interface CurrentInterface {
			get {
				if (currentInterface == null) {
					currentInterface = Interface.CurrentInterface;
					initialize ();
				}
				return currentInterface;
			}
			set {
				currentInterface = value;
			}
		}

		Rectangles clipping = new Rectangles();
		public Rectangles Clipping { get { return clipping; }}

		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region CTOR
		public GraphicObject ()
		{
			#if DEBUG
			uid = currentUid;
			currentUid++;
			#endif
		}
		#endregion
		internal protected virtual void initialize(){
			loadDefaultValues ();
		}
		#region private fields
		LayoutingType registeredLayoutings = LayoutingType.All;
		ILayoutable logicalParent;
		ILayoutable parent;
		string name;
		Fill background = Color.Transparent;
		Fill foreground = Color.White;
		Font font = "droid, 10";
		Measure width, height;
		int left, top;
		double cornerRadius = 0;
		int margin = 0;
		bool focusable = false;
		bool hasFocus = false;
		bool isActive = false;
		bool mouseRepeat;
		protected bool isVisible = true;
		bool isEnabled = true;
		VerticalAlignment verticalAlignment = VerticalAlignment.Center;
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
		Size maximumSize = "0,0";
		Size minimumSize = "0,0";
		bool cacheEnabled = false;
		bool clipToClientRect = true;
		object dataSource;
		string style;
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
		/// TODO: we should ensure the whole parsed widget tree is the last painted
		/// version to clear effective oldslot if parents have been moved or resized.
		/// IDEA is to add a ScreenCoordinates function that use only lastPaintedSlots
		/// </summary>
		public Rectangle LastPaintedSlot;
		/// <summary>Prevent requeuing multiple times the same widget</summary>
		public bool IsQueueForRedraw = false;
		/// <summary>Random value placeholder</summary>
		public object Tag;
		/// <summary>drawing Cache, if null, a redraw is done, cached or not</summary>
		public byte[] bmp;
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
			set { parent = value; }
		}
		[XmlIgnore]public ILayoutable LogicalParent {
			get { return logicalParent == null ? Parent : logicalParent; }
			set { logicalParent = value; }
		}
		[XmlIgnore]public virtual Rectangle ClientRectangle {
			get {
				Rectangle cb = Slot.Size;
				cb.Inflate ( - Margin);
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
			return
				Parent.ScreenCoordinates(r) + Parent.getSlot().Position + Parent.ClientRectangle.Position;
		}
		public virtual Rectangle getSlot () { return Slot;}
		#endregion

		#region EVENT HANDLERS
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseUp;
		public event EventHandler<MouseButtonEventArgs> MouseDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseButtonEventArgs> MouseDoubleClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		public event EventHandler<MouseMoveEventArgs> MouseEnter;
		public event EventHandler<MouseMoveEventArgs> MouseLeave;
		public event EventHandler<KeyboardKeyEventArgs> KeyDown;
		public event EventHandler<KeyboardKeyEventArgs> KeyUp;
		public event EventHandler<KeyPressEventArgs> KeyPress;
		public event EventHandler Focused;
		public event EventHandler Unfocused;
		public event EventHandler Enabled;
		public event EventHandler Disabled;
		public event EventHandler<LayoutingEventArgs> LayoutChanged;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool CacheEnabled {
			get { return cacheEnabled; }
			set {
				if (cacheEnabled == value)
					return;
				cacheEnabled = value;
				NotifyValueChanged ("CacheEnabled", cacheEnabled);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
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
		[XmlAttributeAttribute()][DefaultValue(null)]
		public virtual string Name {
			get {
				#if DEBUG
				return string.IsNullOrEmpty(name) ? this.GetType().Name + uid.ToString () : name;
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
		[XmlAttributeAttribute	()][DefaultValue(VerticalAlignment.Center)]
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
		[XmlAttributeAttribute()][DefaultValue(HorizontalAlignment.Center)]
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
		[XmlAttributeAttribute()][DefaultValue(0)]
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
		[XmlAttributeAttribute()][DefaultValue(0)]
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
		/// When set to True, the <see cref="T:Crow.GraphicObject"/>'s width and height will be set to Fit.
		/// </summary>
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Fit {
			get { return Width == Measure.Fit && Height == Measure.Fit ? true : false; }
			set {
				if (value == Fit)
					return;

				Width = Height = Measure.Fit;
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Inherit")]
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
					if (value < MinimumSize.Width || (value > MaximumSize.Width && MaximumSize.Width > 0))
						return;
				}
				Measure lastWP = WidthPolicy;
				width = value;
				NotifyValueChanged ("Width", width);
				if (WidthPolicy != lastWP) {
					NotifyValueChanged ("WidthPolicy", WidthPolicy);
					//contentSize in Stacks are only update on childLayoutChange, and the single stretched
					//child of the stack is not counted in contentSize, so when changing size policy of a child
					//we should adapt contentSize
					//TODO:check case when child become stretched, and another stretched item already exists.
					if (parent is GenericStack) {//TODO:check if I should test Group instead
						if ((parent as GenericStack).Orientation == Orientation.Horizontal) {
							if (lastWP == Measure.Fit)
								(parent as GenericStack).contentSize.Width -= this.LastSlots.Width;
							else
								(parent as GenericStack).contentSize.Width += this.LastSlots.Width;
						}
					}
				}

				this.RegisterForLayouting (LayoutingType.Width);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Inherit")]
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
					if (value < MinimumSize.Height || (value > MaximumSize.Height && MaximumSize.Height > 0))
						return;
				}
				Measure lastHP = HeightPolicy;
				height = value;
				NotifyValueChanged ("Height", height);
				if (HeightPolicy != lastHP) {
					NotifyValueChanged ("HeightPolicy", HeightPolicy);
					if (parent is GenericStack) {
						if ((parent as GenericStack).Orientation == Orientation.Vertical) {
							if (lastHP == Measure.Fit)
								(parent as GenericStack).contentSize.Height -= this.LastSlots.Height;
							else
								(parent as GenericStack).contentSize.Height += this.LastSlots.Height;
						}
					}
				}

				this.RegisterForLayouting (LayoutingType.Height);
			}
		}
		/// <summary>
		/// Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched
		/// </summary>
		[XmlIgnore]public virtual Measure WidthPolicy { get {
				return Width.IsFit ? Measure.Fit : Measure.Stretched; } }
		/// <summary>
		/// Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched
		/// </summary>
		[XmlIgnore]public virtual Measure HeightPolicy { get {
				return Height.IsFit ? Measure.Fit : Measure.Stretched; } }
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Focusable {
			get { return focusable; }
			set {
				if (focusable == value)
					return;
				focusable = value;
				NotifyValueChanged ("Focusable", focusable);
			}
		}
		[XmlIgnore]public virtual bool HasFocus {
			get { return hasFocus; }
			set {
				if (value == hasFocus)
					return;

				hasFocus = value;
				NotifyValueChanged ("HasFocus", hasFocus);
			}
		}
		[XmlIgnore]public virtual bool IsActive {
			get { return isActive; }
			set {
				if (value == isActive)
					return;

				isActive = value;
				NotifyValueChanged ("IsActive", isActive);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool MouseRepeat {
			get { return mouseRepeat; }
			set {
				if (mouseRepeat == value)
					return;
				mouseRepeat = value;
				NotifyValueChanged ("MouseRepeat", mouseRepeat);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Transparent")]
		public virtual Fill Background {
			get { return background; }
			set {
				if (background == value)
					return;
				if (value == null)
					return;
				background = value;
				NotifyValueChanged ("Background", background);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
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
		[XmlAttributeAttribute()][DefaultValue("sans,10")]
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
		[XmlAttributeAttribute()][DefaultValue(0.0)]
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
		[XmlAttributeAttribute()][DefaultValue(0)]
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
		[XmlAttributeAttribute][DefaultValue(true)]
		public virtual bool Visible {
			get { return isVisible; }
			set {
				if (value == isVisible)
					return;

				isVisible = value;

				//ensure main win doesn't keep hidden childrens ref
				if (!isVisible && this.Contains (CurrentInterface.HoverWidget))
					CurrentInterface.HoverWidget = null;

				if (isVisible)
					RegisterForLayouting (LayoutingType.Sizing);
				else {
					Slot.Width = 0;
					LayoutChanged.Raise (this, new LayoutingEventArgs (LayoutingType.Width));
					Slot.Height = 0;
					LayoutChanged.Raise (this, new LayoutingEventArgs (LayoutingType.Height));
					CurrentInterface.EnqueueForRepaint (this);
					LastSlots.Width = LastSlots.Height = 0;
				}


				NotifyValueChanged ("Visible", isVisible);
			}
		}
		[XmlAttributeAttribute][DefaultValue(true)]
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
		[XmlAttributeAttribute()][DefaultValue("1,1")]
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
		[XmlAttributeAttribute()][DefaultValue("0,0")]
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
		[XmlAttributeAttribute][DefaultValue(null)]
		public virtual object DataSource {
			set {
				if (dataSource == value)
					return;
				#if DEBUG_BINDING
				Debug.WriteLine("******************************");
				Debug.WriteLine("New DataSource for => " + this.ToString());
				Debug.WriteLine("\t- " + DataSource);
				Debug.WriteLine("\t+ " + value);
				#endif

				this.ClearBinding ();

				dataSource = value;

				this.ResolveBindings();

				NotifyValueChanged ("DataSource", dataSource);
			}
			get {
				return dataSource == null ? LogicalParent == null ? null :
					LogicalParent is GraphicObject ?
					(LogicalParent as GraphicObject).DataSource : null : dataSource;
			}
		}
		[XmlAttributeAttribute]
		public virtual string Style {
			get { return style; }
			set {
				if (value == style)
					return;

				style = value;

				NotifyValueChanged ("Style", style);
			}
		}
		#endregion

		#region Default and Style Values loading
		/// <summary> Loads the default values from XML attributes default </summary>
		public void loadDefaultValues()
		{
			#if DEBUG_LOAD
			Debug.WriteLine ("LoadDefValues for " + this.ToString ());
			#endif

			Type thisType = this.GetType ();

			if (!string.IsNullOrEmpty (Style)) {
				if (Interface.DefaultValuesLoader.ContainsKey (Style)) {
					Interface.DefaultValuesLoader [Style] (this);
					return;
				}
			} else {
				if (Interface.DefaultValuesLoader.ContainsKey (thisType.FullName)) {
					Interface.DefaultValuesLoader [thisType.FullName] (this);
					return;
				} else if (!Interface.Styling.ContainsKey (thisType.FullName)) {
					if (Interface.DefaultValuesLoader.ContainsKey (thisType.Name)) {
						Interface.DefaultValuesLoader [thisType.Name] (this);
						return;
					}
				}
			}

			List<Style> styling = new List<Style>();

			//Search for a style mathing :
			//1: Full class name, with full namespace
			//2: class name
			//3: style may have been registered with their ressource ID minus .style extention
			//   those files being placed in a Styles folder
			string styleKey = Style;
			if (!string.IsNullOrEmpty (Style)) {
				if (Interface.Styling.ContainsKey (Style)) {
					styling.Add (Interface.Styling [Style]);
				}
			}
			if (Interface.Styling.ContainsKey (thisType.FullName)) {
				styling.Add (Interface.Styling [thisType.FullName]);
				if (string.IsNullOrEmpty (styleKey))
					styleKey = thisType.FullName;
			}
			if (Interface.Styling.ContainsKey (thisType.Name)) {
				styling.Add (Interface.Styling [thisType.Name]);
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

			dm = new DynamicMethod("dyn_loadDefValues",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {typeof(object)},thisType,true);

			il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof(GraphicObject));
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);

			foreach (EventInfo ei in thisType.GetEvents(BindingFlags.Public | BindingFlags.Instance)) {
				string expression;
				if (!getDefaultEvent(ei, styling, out expression))
					continue;
				CompilerServices.emitBindingCreation (il, ei.Name, expression);
			}

			foreach (PropertyInfo pi in thisType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;
				object defaultValue;
				if (!getDefaultValue (pi, styling, out defaultValue))
					continue;

				CompilerServices.EmitSetValue (il, pi, defaultValue);
			}
			il.Emit(OpCodes.Ret);
			#endregion

			try {
				Interface.DefaultValuesLoader[styleKey] = (Interface.LoaderInvoker)dm.CreateDelegate(typeof(Interface.LoaderInvoker));
				Interface.DefaultValuesLoader[styleKey] (this);
			} catch (Exception ex) {
				throw new Exception ("Error applying style <" + styleKey + ">:", ex);
			}
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
		bool getDefaultValue(PropertyInfo pi, List<Style> styling,
			out object defaultValue){
			defaultValue = null;
			string name = "";

			XmlIgnoreAttribute xia = (XmlIgnoreAttribute)pi.GetCustomAttribute (typeof(XmlIgnoreAttribute));
			if (xia != null)
				return false;
			XmlAttributeAttribute xaa = (XmlAttributeAttribute)pi.GetCustomAttribute (typeof(XmlAttributeAttribute));
			if (xaa != null) {
				if (string.IsNullOrEmpty (xaa.AttributeName))
					name = pi.Name;
				else
					name = xaa.AttributeName;
			}

			int styleIndex = -1;
			if (styling.Count > 0){
				for (int i = 0; i < styling.Count; i++) {
					if (styling[i].ContainsKey (name)){
						styleIndex = i;
						break;
					}
				}
			}
			if (styleIndex >= 0){
				if (pi.PropertyType.IsEnum)//maybe should be in parser..
					defaultValue = Enum.Parse(pi.PropertyType, (string)styling[styleIndex] [name], true);
				else
					defaultValue = styling[styleIndex] [name];
			}else {
				DefaultValueAttribute dv = (DefaultValueAttribute)pi.GetCustomAttribute (typeof (DefaultValueAttribute));
				if (dv == null)
					return false;
				defaultValue = dv.Value;
			}
			return true;
		}
		#endregion

		public virtual GraphicObject FindByName(string nameToFind){
			return string.Equals(nameToFind, name, StringComparison.Ordinal) ? this : null;
		}
		public virtual bool Contains(GraphicObject goToFind){
			return false;
		}

		#region Queuing
		public virtual void RegisterClip(Rectangle clip){
			if (CacheEnabled && bmp != null)
				Clipping.AddRectangle (clip + ClientRectangle.Position);
			if (Parent != null)
				Parent.RegisterClip (clip + Slot.Position + ClientRectangle.Position);
		}
		/// <summary> Full update, taking care of sizing policy </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForGraphicUpdate ()
		{
			bmp = null;
			if (Width.IsFit || Height.IsFit)
				RegisterForLayouting (LayoutingType.Sizing);
			else if (RegisteredLayoutings == LayoutingType.None)
				CurrentInterface.EnqueueForRepaint (this);
		}
		/// <summary> query an update of the content, a redraw </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForRedraw ()
		{
			bmp = null;
			if (RegisteredLayoutings == LayoutingType.None)
				CurrentInterface.EnqueueForRepaint (this);
		}
		#endregion

		#region Layouting

		/// <summary> return size of content + margins </summary>
		protected virtual int measureRawSize (LayoutingType lt) {
			return lt == LayoutingType.Width ?
				contentSize.Width + 2 * Margin: contentSize.Height + 2 * Margin;
		}
		/// <summary> By default in groups, LayoutingType.ArrangeChildren is reset </summary>
		public virtual void ChildrenLayoutingConstraints(ref LayoutingType layoutType){
		}
		public virtual bool ArrangeChildren { get { return false; } }
		public virtual void RegisterForLayouting(LayoutingType layoutType){
			if (Parent == null)
				return;
			lock (CurrentInterface.LayoutMutex) {
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
//				layoutType &= (~RegisteredLayoutings);

				if (layoutType == LayoutingType.None)
					return;

				//enqueue LQI LayoutingTypes separately
				if (layoutType.HasFlag (LayoutingType.Width))
					CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Width, this));
				if (layoutType.HasFlag (LayoutingType.Height))
					CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Height, this));
				if (layoutType.HasFlag (LayoutingType.X))
					CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.X, this));
				if (layoutType.HasFlag (LayoutingType.Y))
					CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Y, this));
				if (layoutType.HasFlag (LayoutingType.ArrangeChildren))
					CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.ArrangeChildren, this));
			}
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			#if DEBUG_LAYOUTING
			CurrentInterface.currentLQI.Slot = LastSlots;
			CurrentInterface.currentLQI.NewSlot = Slot;
			#endif

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
				if (Left == 0) {

					if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Width) ||
					    RegisteredLayoutings.HasFlag (LayoutingType.Width))
						return false;

					switch (HorizontalAlignment) {
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
					Slot.X = Left;

				if (LastSlots.X == Slot.X)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.X = Slot.X;
				break;
			case LayoutingType.Y:
				if (Top == 0) {

					if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Height) ||
					    RegisteredLayoutings.HasFlag (LayoutingType.Height))
						return false;

					switch (VerticalAlignment) {
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
					Slot.Y = Top;

				if (LastSlots.Y == Slot.Y)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Y = Slot.Y;
				break;
			case LayoutingType.Width:
				if (Visible) {
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
					if (Slot.Width < MinimumSize.Width) {
						Slot.Width = MinimumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					} else if (Slot.Width > MaximumSize.Width && MaximumSize.Width > 0) {
						Slot.Width = MaximumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					}
				} else
					Slot.Width = 0;

				if (LastSlots.Width == Slot.Width)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Width = Slot.Width;
				break;
			case LayoutingType.Height:
				if (Visible) {
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
					if (Slot.Height < MinimumSize.Height) {
						Slot.Height = MinimumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					} else if (Slot.Height > MaximumSize.Height && MaximumSize.Height > 0) {
						Slot.Height = MaximumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					}
				} else
					Slot.Height = 0;

				if (LastSlots.Height == Slot.Height)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Height = Slot.Height;
				break;
			}

			//if no layouting remains in queue for item, registre for redraw
			if (this.registeredLayoutings == LayoutingType.None && bmp == null)
				CurrentInterface.EnqueueForRepaint (this);

			return true;
		}
		#endregion

		#region Rendering
		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		protected virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle(gr,rBack,cornerRadius);
			gr.Fill ();
		}

		/// <summary>
		/// Internal drawing context creation on a cached surface limited to slot size
		/// this trigger the effective drawing routine </summary>
		protected virtual void RecreateCache ()
		{
			int stride = 4 * Slot.Width;

			int bmpSize = Math.Abs (stride) * Slot.Height;
			bmp = new byte[bmpSize];

			using (ImageSurface draw =
                new ImageSurface(bmp, Format.Argb32, Slot.Width, Slot.Height, stride)) {
				using (Context gr = new Context (draw)) {
					gr.Antialias = Antialias.Subpixel;
					onDraw (gr);
				}
				draw.Flush ();
			}
		}
		protected virtual void UpdateCache(Context ctx){
			Rectangle rb = Slot + Parent.ClientRectangle.Position;
			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				//TODO:improve equality test for basic color and Fill
				if (this.Background is SolidColor) {
					if ((this.Background as SolidColor).Equals (Color.Clear)) {
						ctx.Save ();
						ctx.Operator = Operator.Clear;
						ctx.Rectangle (rb);
						ctx.Fill ();
						ctx.Restore ();
					}
				}
				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
			//Clipping.clearAndClip (ctx);
			Clipping.Reset();
		}
		/// <summary> Chained painting routine on the parent context of the actual cached version
		/// of the widget </summary>
		public virtual void Paint (ref Context ctx)
		{
			if (!Visible)
				return;

			//TODO:this test should not be necessary
			if (Slot.Height < 0 || Slot.Width < 0)
				return;

			LastPaintedSlot = Slot;

			if (cacheEnabled) {
				if (Slot.Width > Interface.MaxCacheSize || Slot.Height > Interface.MaxCacheSize)
					cacheEnabled = false;
			}

			if (cacheEnabled) {
				if (bmp == null)
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
		public virtual void onKeyDown(object sender, KeyboardKeyEventArgs e){
			KeyDown.Raise (sender, e);
		}
		public virtual void onKeyUp(object sender, KeyboardKeyEventArgs e){
			KeyUp.Raise (sender, e);
		}
		public virtual void onKeyPress(object sender, KeyPressEventArgs e){
			KeyPress.Raise (sender, e);
		}
        #endregion

		#region Mouse handling
		public virtual bool MouseIsIn(Point m)
		{
			if (!(Visible & isEnabled))
				return false;
			if (ScreenCoordinates (Slot).ContainsOrIsEqual (m)) {
				Scroller scr = Parent as Scroller;
				if (scr == null) {
					if (Parent is GraphicObject)
						return (Parent as GraphicObject).MouseIsIn (m);
					else return true;
				}
				return scr.MouseIsIn (scr.savedMousePos);
			}
			return false;
		}
		public virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			if (CurrentInterface.HoverWidget != this) {
				CurrentInterface.HoverWidget = this;
				onMouseEnter (this, e);
			}

			this.onMouseMove (this, e);
		}
		public virtual void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			//bubble event to the top
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseMove(sender,e);

			MouseMove.Raise (sender, e);
		}
		public virtual void onMouseDown(object sender, MouseButtonEventArgs e){
			if (CurrentInterface.activeWidget == null)
				CurrentInterface.activeWidget = this;
			if (this.Focusable && !Interface.FocusOnHover) {
				BubblingMouseButtonEventArg be = e as BubblingMouseButtonEventArg;
				if (be.Focused == null) {
					be.Focused = this;
					CurrentInterface.FocusedWidget = this;
				}
			}
			//bubble event to the top
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseDown(sender,e);

			MouseDown.Raise (this, e);
		}
		public virtual void onMouseUp(object sender, MouseButtonEventArgs e){
			//bubble event to the top
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseUp(sender,e);

			MouseUp.Raise (this, e);

			if (MouseIsIn (e.Position) && IsActive)
				onMouseClick (this, e);
		}
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){

			if (Interface.clickTimer.ElapsedMilliseconds > 0 &&
			    Interface.clickTimer.ElapsedMilliseconds < Interface.DoubleClick) {
				Interface.clickTimer.Reset ();
				onMouseDoubleClick (this, e);
				return;
			} else
				Interface.clickTimer.Restart ();

			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseClick(sender,e);
			MouseClick.Raise (this, e);
		}
		public virtual void onMouseDoubleClick(object sender, MouseButtonEventArgs e){
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseDoubleClick(sender,e);
			MouseDoubleClick.Raise (this, e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseWheel(sender,e);

			MouseWheelChanged.Raise (this, e);
		}
		public virtual void onMouseEnter(object sender, MouseMoveEventArgs e)
		{
			#if DEBUG_FOCUS
			Debug.WriteLine("MouseEnter => " + this.ToString());
			#endif
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

		public virtual void onFocused(object sender, EventArgs e){
			#if DEBUG_FOCUS
			Debug.WriteLine("Focused => " + this.ToString());
			#endif
			Focused.Raise (this, e);
			this.HasFocus = true;
		}
		public virtual void onUnfocused(object sender, EventArgs e){
			#if DEBUG_FOCUS
			Debug.WriteLine("UnFocused => " + this.ToString());
			#endif

			Unfocused.Raise (this, e);
			this.HasFocus = false;
		}
		public virtual void onEnable(object sender, EventArgs e){
			Enabled.Raise (this, e);
		}
		public virtual void onDisable(object sender, EventArgs e){
			Disabled.Raise (this, e);
		}

		#region Binding
		public void BindMember(string _member, string _expression){
			Bindings.Add(new Binding (this, _member, _expression));
		}
		public virtual void ResolveBindings()
		{
			if (Bindings.Count == 0)
				return;
			#if DEBUG_BINDING
			Debug.WriteLine ("Resolve Bindings => " + this.ToString ());
			#endif

			CompilerServices.ResolveBindings (Bindings);
		}

		/// <summary>
		/// Remove dynamic delegates by ids from dataSource
		///  and delete ref of this in Shared interface refs
		/// </summary>
		public virtual void ClearBinding(){
			//dont clear binding if dataSource is not null,
			foreach (Binding b in Bindings) {
				try {
					if (!b.Resolved)
						continue;
					//cancel compiled events
					if (b.Target == null){
						continue;
						#if DEBUG_BINDING
						Debug.WriteLine("Clear binding canceled for => " + b.ToString());
						#endif
					}
					if (b.Target.Instance != DataSource){
						#if DEBUG_BINDING
						Debug.WriteLine("Clear binding canceled for => " + b.ToString());
						#endif
						continue;
					}
					#if DEBUG_BINDING
					Debug.WriteLine("ClearBinding => " + b.ToString());
					#endif
					if (string.IsNullOrEmpty (b.DynMethodId)) {
						b.Resolved = false;
						if (b.Source.Member.MemberType == MemberTypes.Event)
							removeEventHandler (b);
						//TODO:check if full reset is necessary
						continue;
					}
					MemberReference mr = null;
					if (b.Target == null)
						mr = b.Source;
					else
						mr = b.Target;
					Type dataSourceType = mr.Instance.GetType();
					EventInfo evtInfo = dataSourceType.GetEvent ("ValueChanged");
					FieldInfo evtFi = CompilerServices.GetEventHandlerField (dataSourceType, "ValueChanged");
					MulticastDelegate multicastDelegate = evtFi.GetValue (mr.Instance) as MulticastDelegate;
					if (multicastDelegate != null) {
						foreach (Delegate d in multicastDelegate.GetInvocationList()) {
							if (d.Method.Name == b.DynMethodId)
								evtInfo.RemoveEventHandler (mr.Instance, d);
						}
					}
					b.Reset ();
				} catch (Exception ex) {
					Debug.WriteLine("\t Error: " + ex.ToString());
				}
			}
		}
		void removeEventHandler(Binding b){
			FieldInfo fiEvt = CompilerServices.GetEventHandlerField (b.Source.Instance.GetType(), b.Source.Member.Name);
			MulticastDelegate multiDel = fiEvt.GetValue (b.Source.Instance) as MulticastDelegate;
			if (multiDel != null) {
				foreach (Delegate d in multiDel.GetInvocationList()) {
					if (d.Method.Name == b.Target.Member.Name)
						b.Source.Event.RemoveEventHandler (b.Source.Instance, d);
				}
			}
		}
		#endregion

		#region IXmlSerializable
		public virtual System.Xml.Schema.XmlSchema GetSchema ()
		{
			return null;
		}
		void affectMember(string name, string value){
			Type thisType = this.GetType ();

			if (string.IsNullOrEmpty (value))
				return;

			MemberInfo mi = thisType.GetMember (name).FirstOrDefault();
			if (mi == null) {
				Debug.WriteLine ("XML: Unknown attribute in " + thisType.ToString() + " : " + name);
				return;
			}
			if (mi.MemberType == MemberTypes.Event) {
				this.Bindings.Add (new Binding (new MemberReference(this, mi), value));
				return;
			}
			if (mi.MemberType == MemberTypes.Property) {
				PropertyInfo pi = mi as PropertyInfo;

				if (pi.GetSetMethod () == null) {
					Debug.WriteLine ("XML: Read only property in " + thisType.ToString() + " : " + name);
					return;
				}

				XmlAttributeAttribute xaa = (XmlAttributeAttribute)pi.GetCustomAttribute (typeof(XmlAttributeAttribute));
				if (xaa != null) {
					if (!string.IsNullOrEmpty (xaa.AttributeName))
						name = xaa.AttributeName;
				}
				if (value.StartsWith("{",StringComparison.Ordinal)) {
					//binding
					if (!value.EndsWith("}", StringComparison.Ordinal))
						throw new Exception (string.Format("XML:Malformed binding: {0}", value));

					this.Bindings.Add (new Binding (new MemberReference(this, pi), value.Substring (1, value.Length - 2)));
					return;
				}
				if (pi.GetCustomAttribute (typeof(XmlIgnoreAttribute)) != null)
					return;
				if (xaa == null)//not define as xmlAttribute
					return;

				if (pi.PropertyType == typeof(string)) {
					pi.SetValue (this, value, null);
					return;
				}

				if (pi.PropertyType.IsEnum) {
					pi.SetValue (this, Enum.Parse (pi.PropertyType, value), null);
				} else {
					MethodInfo me = pi.PropertyType.GetMethod ("Parse", new Type[] { typeof(string) });
					pi.SetValue (this, me.Invoke (null, new string[] { value }), null);
				}
			}
		}
		public virtual void ReadXml (System.Xml.XmlReader reader)
		{
			if (reader.HasAttributes) {

				style = reader.GetAttribute ("Style");

				loadDefaultValues ();

				while (reader.MoveToNextAttribute ()) {
					if (reader.Name == "Style")
						continue;

					affectMember (reader.Name, reader.Value);
				}
				reader.MoveToElement ();
			}else
				loadDefaultValues ();
		}
		public virtual void WriteXml (System.Xml.XmlWriter writer)
		{
			foreach (PropertyInfo pi in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;

				bool isAttribute = false;
				bool hasDefaultValue = false;
				bool ignore = false;
				string name = "";
				object value = null;
				Type valueType = null;


				MemberInfo mi = pi.GetGetMethod ();

				if (mi == null)
					continue;

				value = pi.GetValue (this, null);
				valueType = pi.PropertyType;
				name = pi.Name;



				object[] att = pi.GetCustomAttributes (false);

				foreach (object o in att) {
					XmlAttributeAttribute xaa = o as XmlAttributeAttribute;
					if (xaa != null) {
						isAttribute = true;
						if (string.IsNullOrEmpty (xaa.AttributeName))
							name = pi.Name;
						else
							name = xaa.AttributeName;
						continue;
					}

					XmlIgnoreAttribute xia = o as XmlIgnoreAttribute;
					if (xia != null) {
						ignore = true;
						continue;
					}

					DefaultValueAttribute dv = o as DefaultValueAttribute;
					if (dv != null) {
						if (dv.Value.Equals (value))
							hasDefaultValue = true;
						if (dv.Value.ToString () == value.ToString ())
							hasDefaultValue = true;

						continue;
					}


				}

				if (hasDefaultValue || ignore || value==null)
					continue;

				if (isAttribute)
					writer.WriteAttributeString (name, value.ToString ());
				else {
					if (valueType.GetInterface ("IXmlSerializable") == null)
						continue;

					(pi.GetValue (this, null) as IXmlSerializable).WriteXml (writer);
				}
			}
			foreach (EventInfo ei in this.GetType().GetEvents()) {
				FieldInfo fi = this.GetType().GetField(ei.Name,
					BindingFlags.NonPublic |
					BindingFlags.Instance |
					BindingFlags.GetField);

				Delegate dg = (System.Delegate)fi.GetValue (this);

				if (dg == null)
					continue;

				foreach (Delegate d in dg.GetInvocationList()) {
					if (!d.Method.Name.StartsWith ("<"))//Skipping empty handler, not clear it's trikky
						writer.WriteAttributeString (ei.Name, d.Method.Name);
				}
			}
		}
		#endregion

		#region ICloneable implementation
		public object Clone ()
		{
			Type type = this.GetType ();
			GraphicObject result = (GraphicObject)Activator.CreateInstance (type);
			result.CurrentInterface = CurrentInterface;

			foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;

				if (pi.GetCustomAttribute<XmlIgnoreAttribute> () != null)
					continue;
				if (pi.Name == "DataSource")
					continue;

				pi.SetValue(result, pi.GetValue(this));
			}
			return result;
		}
		#endregion
		/// <summary>
		/// full GraphicTree clone with binding definition
		/// </summary>
		public virtual GraphicObject DeepClone(){
			GraphicObject tmp = Clone () as GraphicObject;
			foreach (Binding b in this.bindings)
				tmp.Bindings.Add (new Binding (new MemberReference (tmp, b.Source.Member), b.Expression));
			return tmp;
		}

		public override string ToString ()
		{
			string tmp ="";

			if (Parent != null)
				tmp = Parent.ToString () + tmp;
			#if DEBUG_LAYOUTING
			return Name == "unamed" ? tmp + "." + this.GetType ().Name + uid.ToString(): tmp + "." + Name;
			#else
			return Name == "unamed" ? tmp + "." + this.GetType ().Name : tmp + "." + Name;
			#endif
		}
	}
}
