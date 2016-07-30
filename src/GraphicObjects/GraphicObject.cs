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


		internal int layoutingTries = 0;

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
			uid = currentUid;
			currentUid++;

			if (Interface.XmlSerializerInit)
				return;

			loadDefaultValues ();
		}
		public GraphicObject (Rectangle _bounds)
		{
			if (Interface.XmlSerializerInit)
				return;

			loadDefaultValues ();
			Left = _bounds.Left;
			Top = _bounds.Top;
			Width = _bounds.Width;
			Height = _bounds.Height;
		}
		#endregion

		#region private fields
		LayoutingType registeredLayoutings = LayoutingType.None;
		ILayoutable logicalParent;
		ILayoutable _parent;
		string _name = "unamed";
		Fill _background = Color.Transparent;
		Fill _foreground = Color.White;
		Font _font = "droid, 10";
		Measure _width, _height;
		int _left, _top;
		double _cornerRadius = 0;
		int _margin = 0;
		bool _focusable = false;
		bool _hasFocus = false;
		bool _isActive = false;
		bool _mouseRepeat;
		protected bool _isVisible = true;
		VerticalAlignment _verticalAlignment = VerticalAlignment.Center;
		HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;
		Size _maximumSize = "0;0";
		Size _minimumSize = "0;0";
		bool cacheEnabled = false;
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
		public object Tag;
		public byte[] bmp;
		#endregion

		#region ILayoutable
		[XmlIgnore]public LayoutingType RegisteredLayoutings { get { return registeredLayoutings; } set { registeredLayoutings = value; } }
		//TODO: it would save the recurent cost of a cast in event bubbling if parent type was GraphicObject
		//		or we could add to the interface the mouse events
		/// <summary>
		/// Parent in the graphic tree, used for rendering and layouting
		/// </summary>
		[XmlIgnore]public virtual ILayoutable Parent {
			get { return _parent; }
			set { _parent = value; }
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
		public virtual Rectangle getSlot() => Slot;
		#endregion

		#region EVENT HANDLERS
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseUp;
		public event EventHandler<MouseButtonEventArgs> MouseDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		public event EventHandler<MouseMoveEventArgs> MouseEnter;
		public event EventHandler<MouseMoveEventArgs> MouseLeave;
		public event EventHandler<KeyboardKeyEventArgs> KeyDown;
		public event EventHandler<KeyboardKeyEventArgs> KeyUp;
		public event EventHandler<KeyPressEventArgs> KeyPress;
		public event EventHandler Focused;
		public event EventHandler Unfocused;
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
		[XmlAttributeAttribute()][DefaultValue("unamed")]
		public virtual string Name {
			get { return _name; }
			set {
				if (_name == value)
					return;
				_name = value;
				NotifyValueChanged("Name", _verticalAlignment);
			}
		}
		[XmlAttributeAttribute	()][DefaultValue(VerticalAlignment.Center)]
		public virtual VerticalAlignment VerticalAlignment {
			get { return _verticalAlignment; }
			set {
				if (_verticalAlignment == value)
					return;

				_verticalAlignment = value;
				NotifyValueChanged("VerticalAlignment", _verticalAlignment);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(HorizontalAlignment.Center)]
		public virtual HorizontalAlignment HorizontalAlignment {
			get { return _horizontalAlignment; }
			set {
				if (_horizontalAlignment == value)
					return;

				_horizontalAlignment = value;
				NotifyValueChanged("HorizontalAlignment", _horizontalAlignment);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Left {
			get { return _left; }
			set {
				if (_left == value)
					return;

				_left = value;
				NotifyValueChanged ("Left", _left);
				this.RegisterForLayouting (LayoutingType.X);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Top {
			get { return _top; }
			set {
				if (_top == value)
					return;

				_top = value;
				NotifyValueChanged ("Top", _top);
				this.RegisterForLayouting (LayoutingType.Y);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Stretched")]
		public virtual Measure Width {
			get { return _width; }
			set {
				if (_width == value)
					return;
				if (value.IsFixed) {
					if (value < MinimumSize.Width || (value > MaximumSize.Width && MaximumSize.Width > 0))
						return;
				}

				_width = value;
				NotifyValueChanged ("Width", _width);
				NotifyValueChanged ("WidthPolicy", WidthPolicy);

				this.RegisterForLayouting (LayoutingType.Width);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Stretched")]
		public virtual Measure Height {
			get { return _height; }
			set {
				if (_height == value)
					return;
				if (value.IsFixed) {
					if (value < MinimumSize.Height || (value > MaximumSize.Height && MaximumSize.Height > 0))
						return;
				}

				_height = value;
				NotifyValueChanged ("Height", _height);
				NotifyValueChanged ("HeightPolicy", HeightPolicy);

				this.RegisterForLayouting (LayoutingType.Height);
			}
		}
		/// <summary>
		/// Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched
		/// </summary>
		[XmlIgnore]public virtual Measure WidthPolicy { get {
				return Width.Units == Unit.Percent || Width.IsFixed ?
					Measure.Stretched : Measure.Fit; } }
		/// <summary>
		/// Used for binding on dimensions, this property will never hold fixed size, but instead only
		/// Fit or Stretched
		/// </summary>
		[XmlIgnore]public virtual Measure HeightPolicy { get {
				return Height.Units == Unit.Percent || Height.IsFixed ?
					Measure.Stretched : Measure.Fit; } }
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
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Focusable {
			get { return _focusable; }
			set {
				if (_focusable == value)
					return;
				_focusable = value;
				NotifyValueChanged ("Focusable", _focusable);
			}
		}
		[XmlIgnore]public virtual bool HasFocus {
			get { return _hasFocus; }
			set {
				if (value == _hasFocus)
					return;

				_hasFocus = value;
				NotifyValueChanged ("HasFocus", _hasFocus);
			}
		}
		[XmlIgnore]public virtual bool IsActive {
			get { return _isActive; }
			set {
				if (value == _isActive)
					return;

				_isActive = value;
				NotifyValueChanged ("IsActive", _isActive);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool MouseRepeat {
			get { return _mouseRepeat; }
			set {
				if (_mouseRepeat == value)
					return;
				_mouseRepeat = value;
				NotifyValueChanged ("MouseRepeat", _mouseRepeat);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Transparent")]
		public virtual Fill Background {
			get { return _background; }
			set {
				if (_background == value)
					return;
				_background = value;
				NotifyValueChanged ("Background", _background);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Fill Foreground {
			get { return _foreground; }
			set {
				if (_foreground == value)
					return;
				_foreground = value;
				NotifyValueChanged ("Foreground", _foreground);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("sans,10")]
		public virtual Font Font {
			get { return _font; }
			set {
				if (value == _font)
					return;
				_font = value;
				NotifyValueChanged ("Font", _font);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public virtual double CornerRadius {
			get { return _cornerRadius; }
			set {
				if (value == _cornerRadius)
					return;
				_cornerRadius = value;
				NotifyValueChanged ("CornerRadius", _cornerRadius);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Margin {
			get { return _margin; }
			set {
				if (value == _margin)
					return;
				_margin = value;
				NotifyValueChanged ("Margin", _margin);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool Visible {
			get { return _isVisible; }
			set {
				if (value == _isVisible)
					return;

				_isVisible = value;

				if (Interface.CurrentInterface == null)
					return;

				//ensure main win doesn't keep hidden childrens ref
				if (!_isVisible && this.Contains (Interface.CurrentInterface.hoverWidget))
					Interface.CurrentInterface.hoverWidget = null;

				if (Parent is GraphicObject)
					(Parent as GraphicObject).RegisterForLayouting (LayoutingType.Sizing);
				if (Parent is GenericStack)
					(Parent as GraphicObject).RegisterForLayouting (LayoutingType.ArrangeChildren);

				if (_isVisible)
					RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();

				NotifyValueChanged ("Visible", _isVisible);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("1;1")]
		public virtual Size MinimumSize {
			get { return _minimumSize; }
			set {
				if (value == _minimumSize)
					return;

				_minimumSize = value;

				NotifyValueChanged ("MinimumSize", _minimumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("0;0")]
		public virtual Size MaximumSize {
			get { return _maximumSize; }
			set {
				if (value == _maximumSize)
					return;

				_maximumSize = value;

				NotifyValueChanged ("MaximumSize", _maximumSize);
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

		/// <summary> Loads the default values from XML attributes default </summary>
		protected virtual void loadDefaultValues()
		{
			#if DEBUG_LOAD
			Debug.WriteLine ("LoadDefValues for " + this.ToString ());
			#endif

			Type thisType = this.GetType ();
			if (Interface.DefaultValuesLoader.ContainsKey (thisType.FullName)) {
				Interface.DefaultValuesLoader [thisType.FullName] (this);
				return;
			}

			Dictionary<string, string> styling = null;

			if (Interface.CurrentInterface.Styling.ContainsKey (thisType.Name))
				styling = Interface.CurrentInterface.Styling[thisType.Name];
				                                             
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

			il.Emit(OpCodes.Nop);

			foreach (PropertyInfo pi in thisType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				string name = "";
				object defaultValue = null;

				#region retrieve custom attributes
				if (pi.GetSetMethod () == null)
					continue;

				XmlIgnoreAttribute xia = (XmlIgnoreAttribute)pi.GetCustomAttribute (typeof(XmlIgnoreAttribute));
				if (xia != null)
					continue;
				XmlAttributeAttribute xaa = (XmlAttributeAttribute)pi.GetCustomAttribute (typeof(XmlAttributeAttribute));
				if (xaa != null) {
					if (string.IsNullOrEmpty (xaa.AttributeName))
						name = pi.Name;
					else
						name = xaa.AttributeName;
				}
				if (styling.ContainsKey (name)) {

				} else {
					if (name == "Style") {
						//retrieve default value from class attribute
						//DefaultStyle defStyle = thisType.GetCustomAttribute<DefaultStyle>();
						//if (defStyle != null)
						//	defaultValue = defStyle.Path;
					} else {
						DefaultValueAttribute dv = (DefaultValueAttribute)pi.GetCustomAttribute (typeof (DefaultValueAttribute));
						if (dv == null)
							continue;
						defaultValue = dv.Value;
					}
				}
				#endregion

				il.Emit (OpCodes.Ldarg_0);

				if (defaultValue == null) {
					il.Emit (OpCodes.Ldnull);
					il.Emit (OpCodes.Callvirt, pi.GetSetMethod ());
					continue;
				}
				Type dvType = defaultValue.GetType ();

				if (dvType.IsValueType) {
					if (pi.PropertyType.IsValueType) {
						if (pi.PropertyType.IsEnum) {
							if (pi.PropertyType != dvType)
								throw new Exception ("Enum mismatch in default values: " + pi.PropertyType.FullName);
							il.Emit (OpCodes.Ldc_I4, Convert.ToInt32 (defaultValue));
						} else {
							switch (Type.GetTypeCode (dvType)) {
							case TypeCode.Boolean:
								if ((bool)defaultValue == true)
									il.Emit (OpCodes.Ldc_I4_1);
								else
									il.Emit (OpCodes.Ldc_I4_0);
								break;
//						case TypeCode.Empty:
//							break;
//						case TypeCode.Object:
//							break;
//						case TypeCode.DBNull:
//							break;
//						case TypeCode.SByte:
//							break;
//						case TypeCode.Decimal:
//							break;
//						case TypeCode.DateTime:
//							break;
							case TypeCode.Char:
								il.Emit (OpCodes.Ldc_I4, Convert.ToChar (defaultValue));
								break;
							case TypeCode.Byte:
							case TypeCode.Int16:
							case TypeCode.Int32:
								il.Emit (OpCodes.Ldc_I4, Convert.ToInt32 (defaultValue));
								break;
							case TypeCode.UInt16:
							case TypeCode.UInt32:
								il.Emit (OpCodes.Ldc_I4, Convert.ToUInt32 (defaultValue));
								break;
							case TypeCode.Int64:
								il.Emit (OpCodes.Ldc_I8, Convert.ToInt64 (defaultValue));
								break;
							case TypeCode.UInt64:
								il.Emit (OpCodes.Ldc_I8, Convert.ToUInt64 (defaultValue));
								break;
							case TypeCode.Single:
								il.Emit (OpCodes.Ldc_R4, Convert.ToSingle (defaultValue));
								break;
							case TypeCode.Double:
								il.Emit (OpCodes.Ldc_R8, Convert.ToDouble (defaultValue));
								break;
							case TypeCode.String:
								il.Emit (OpCodes.Ldstr, Convert.ToString (defaultValue));
								break;
							default:
								il.Emit (OpCodes.Pop);
								continue;
							}
						}
					} else
						throw new Exception ("Expecting valuetype in default values for: " + pi.Name);
				}else{
					//surely a class or struct
					if (dvType != typeof(string))
						throw new Exception ("Expecting String in default values for: " + pi.Name);
					if (pi.PropertyType == typeof(string))
						il.Emit (OpCodes.Ldstr, Convert.ToString (defaultValue));
					else {
						MethodInfo miParse = pi.PropertyType.GetMethod ("Parse", BindingFlags.Static | BindingFlags.Public);
						if (miParse == null)
							throw new Exception ("no Parse method found for: " + pi.PropertyType.FullName);

						il.Emit (OpCodes.Ldstr, Convert.ToString (defaultValue));
						il.Emit (OpCodes.Callvirt, miParse);

						if (miParse.ReturnType != pi.PropertyType)
							il.Emit (OpCodes.Unbox_Any, pi.PropertyType);
					}
				}
				il.Emit (OpCodes.Callvirt, pi.GetSetMethod ());
			}
			il.Emit(OpCodes.Ret);
			#endregion

			Interface.DefaultValuesLoader[thisType.FullName] = (Interface.loadDefaultInvoker)dm.CreateDelegate(typeof(Interface.loadDefaultInvoker));
			Interface.DefaultValuesLoader[thisType.FullName] (this);
			applyStyle ();
		}

		public virtual GraphicObject FindByName(string nameToFind){
			return string.Equals(nameToFind, _name, StringComparison.Ordinal) ? this : null;
		}
		public virtual bool Contains(GraphicObject goToFind){
			return false;
		}
		public virtual void RegisterClip(Rectangle clip){
			if (CacheEnabled && bmp != null)
				Clipping.AddRectangle (clip + ClientRectangle.Position);
			if (Parent != null)
				Parent.RegisterClip (clip + Slot.Position + ClientRectangle.Position);
		}
		public bool IsQueueForGraphicUpdate = false;
		/// <summary>
		/// Clear chached object and add clipping region in redraw list of interface
		/// </summary>
		public void RegisterForGraphicUpdate ()
		{
			Interface.RegisterForGraphicUpdate (this);
		}
		internal bool IsInRedrawList = false;
		/// <summary>
		/// Add clipping region in redraw list of interface, dont update cached object content
		/// </summary>
		internal void AddToRedrawList ()
		{
			Interface.AddToRedrawList (this);
		}

		#region Layouting
		[XmlIgnore]public int LayoutingTries {
			get { return layoutingTries; }
			set { layoutingTries = value; }
		}
		protected Size contentSize;
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
			lock (Interface.CurrentInterface.LayoutMutex) {
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

				//prevent queueing same LayoutingType for this
				layoutType &= (~RegisteredLayoutings);

				if (layoutType == LayoutingType.None)
					return;

				#if DEBUG_LAYOUTING
				Debug.WriteLine ("REGLayout => {1}->{0}", layoutType, this.ToString());
				#endif
				//enqueue LQI LayoutingTypes separately
				if (layoutType.HasFlag (LayoutingType.Width))
					Interface.CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Width, this));
				if (layoutType.HasFlag (LayoutingType.Height))
					Interface.CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Height, this));
				if (layoutType.HasFlag (LayoutingType.X))
					Interface.CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.X, this));
				if (layoutType.HasFlag (LayoutingType.Y))
					Interface.CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.Y, this));
				if (layoutType.HasFlag (LayoutingType.ArrangeChildren))
					Interface.CurrentInterface.LayoutingQueue.Enqueue (new LayoutingQueueItem (LayoutingType.ArrangeChildren, this));
			}
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("\t    " + LastSlots.ToString() + "\n\t => " + Slot.ToString ());
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
				this.AddToRedrawList ();

			return true;
		}
		#endregion

		#region Rendering
		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		protected virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle(gr,rBack,_cornerRadius);
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

			LastPaintedSlot = Slot;

			if (cacheEnabled) {
				if (Slot.Width > Interface.MaxCacheSize || Slot.Height > Interface.MaxCacheSize)
					cacheEnabled = false;
			}

			if (cacheEnabled) {
				if (bmp == null)
					RecreateCache ();

				UpdateCache (ctx);
			} else {
				Rectangle rb = Slot + Parent.ClientRectangle.Position;
				ctx.Save ();

				ctx.Translate (rb.X, rb.Y);

				onDraw (ctx);

				ctx.Restore ();
			}
		}
		#endregion

        #region Keyboard handling
		public virtual void onKeyDown(object sender, KeyboardKeyEventArgs e){
			KeyDown.Raise (sender, e);
		}
		public virtual void onKeyPress(object sender, KeyPressEventArgs e){
			KeyPress.Raise (sender, e);
		}
        #endregion

		#region Mouse handling
		public virtual bool MouseIsIn(Point m)
		{
			if (!Visible)
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
			if (Interface.CurrentInterface.hoverWidget != this) {
				Interface.CurrentInterface.hoverWidget = this;
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
			if (Interface.CurrentInterface.activeWidget == null)
				Interface.CurrentInterface.activeWidget = this;
			if (this.Focusable && !Interface.FocusOnHover) {
				BubblingMouseButtonEventArg be = e as BubblingMouseButtonEventArg;
				if (be.Focused == null) {
					be.Focused = this;
					Interface.CurrentInterface.FocusedWidget = this;
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

			if (MouseIsIn (e.Position)&&HasFocus)
				onMouseClick(sender,e);
		}
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){
			MouseClick.Raise (this, e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseWheel(this,e);

			MouseWheelChanged.Raise (this, e);
		}
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
				Debug.WriteLine ("GOML: Unknown attribute in " + thisType.ToString() + " : " + name);
				return;
			}
			if (mi.MemberType == MemberTypes.Event) {
				this.Bindings.Add (new Binding (new MemberReference(this, mi), value));
				return;
			}
			if (mi.MemberType == MemberTypes.Property) {
				PropertyInfo pi = mi as PropertyInfo;

				if (pi.GetSetMethod () == null) {
					Debug.WriteLine ("GOML: Read only property in " + thisType.ToString() + " : " + name);
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
						throw new Exception (string.Format("GOML:Malformed binding: {0}", value));

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
		void applyStyle(){
			//The first place searched for a style is in the style propery of this instance
			//this field may contains the path to a Style file for the object.
			if (string.IsNullOrEmpty (style))
				return;
			Stream s = Interface.GetStreamFromPath (style);
			if (s == null)
				throw new Exception ("Style Path not found: " + style);

			#if DEBUG_LOAD
			Debug.WriteLine ("ApplyStyle for " + this.ToString ());
			#endif
			using (StreamReader sr = new StreamReader (s)) {
				while (!sr.EndOfStream) {
					string tmp = sr.ReadLine ();
					if (string.IsNullOrWhiteSpace (tmp))
						continue;
					int i = tmp.IndexOf ('=');
					if (i < 0)
						continue;
					string name = tmp.Substring (0, i).Trim();
					string value = tmp.Substring (i + 1).Trim ();

					affectMember (name, value);
				}
			}
		}
		public virtual void ReadXml (System.Xml.XmlReader reader)
		{
			if (!reader.HasAttributes)
				return;

			string stylePath = reader.GetAttribute ("Style");

			if (!string.IsNullOrEmpty (style)) {
				Style = stylePath;
				applyStyle ();
			}
			while (reader.MoveToNextAttribute ()) {
				if (reader.Name == "Style")
					continue;

				affectMember (reader.Name, reader.Value);
			}
			reader.MoveToElement();
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

			foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;

				if (pi.GetCustomAttribute<XmlIgnoreAttribute> () != null)
					continue;
				if (pi.Name == "DataSource")
					continue;
//				object[] att = pi.GetCustomAttributes (false);
//				foreach (object o in att) {
//					XmlIgnoreAttribute xia = o as XmlIgnoreAttribute;
//					if (xia != null)
//						continue;
//				}

				pi.SetValue(result, pi.GetValue(this));
			}
			return result;
		}
		#endregion
	}
}
