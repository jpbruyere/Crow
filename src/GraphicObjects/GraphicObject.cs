using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Diagnostics;
using OpenTK.Input;

using Cairo;

using System.Xml.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.IO;
//using System.Xml;
using System.Xml;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

namespace Crow
{		
	public class GraphicObject : IXmlSerializable, ILayoutable, IValueChange
	{
		#if DEBUG_LAYOUTING
		internal static ulong currentUid = 0;
		internal ulong uid = 0;
		#endif

		internal List<Binding> Bindings = new List<Binding> ();

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
			#if DEBUG_LAYOUTING
			uid = currentUid;
			currentUid++;
			#endif

			if (Interface.XmlSerializerInit)
				return;

			loadDefaultValues ();
			registerForGraphicUpdate ();
		}
		public GraphicObject (Rectangle _bounds)
		{
			if (Interface.XmlSerializerInit)
				return;
			
			loadDefaultValues ();
			Bounds = _bounds;
			registerForGraphicUpdate ();
		}
		#endregion

		#region private fields
		ILayoutable _parent;
		string _name = "unamed";
		Color _background = Color.Transparent;
		Color _foreground = Color.White;
		Font _font = "droid, 10";
		double _cornerRadius = 0;
		int _margin = 0;
		bool _focusable = false;
		bool _hasFocus = false;
		protected bool _isVisible = true;
		VerticalAlignment _verticalAlignment = VerticalAlignment.Center;
		HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;
		Size _maximumSize = "0;0";
		Size _minimumSize = "0;0";

		Picture _backgroundImage;
		string _backgroundImagePath;
		string _template;
		#endregion

		#region public fields
		public GraphicObject LogicalParent;
		public Rectangle Bounds;
		public Rectangle Slot = new Rectangle ();
		public object Tag;
		public byte[] bmp;
		#endregion

		#region ILayoutable
		[XmlIgnore]public ILayoutable Parent { 
			get { return _parent; }
			set {
//				if (_parent == value)
//					return;
//				if (_parent != null)
//					ClearBinding ();
//				
				_parent = value;
//
//				if (DataSource != null)
//					ResolveBindings ();
			}
		}


		[XmlIgnore]public virtual Rectangle ClientRectangle {
			get {
				Rectangle cb = Slot.Size;
				cb.Inflate ( - Margin);
				return cb;
			}
		}
		[XmlIgnore]public virtual IGOLibHost TopContainer {
			get { return Parent == null ? null : Parent.TopContainer; }
		}
		public virtual Rectangle ContextCoordinates(Rectangle r){
			return
				Parent.ContextCoordinates (r);// + ClientRectangle.Position;
		}			
		public virtual Rectangle ScreenCoordinates (Rectangle r){
			//r += Slot.Position;

			return 
				Parent.ScreenCoordinates(r) + Parent.getSlot().Position + Parent.ClientRectangle.Position;
		}
		public virtual Rectangle getSlot()
		{
			return Slot;
		}
		public virtual Rectangle getBounds()
		{
			return Bounds;
		}
		#endregion

		#region EVENT HANDLERS
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		public event EventHandler<MouseMoveEventArgs> MouseEnter;
		public event EventHandler<MouseMoveEventArgs> MouseLeave;
		public event EventHandler<KeyboardKeyEventArgs> KeyDown;
		public event EventHandler<KeyboardKeyEventArgs> KeyUp;
		public event EventHandler Focused;
		public event EventHandler Unfocused;
		public event EventHandler<LayoutChangeEventArgs> LayoutChanged;
		#endregion

		#region public properties
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
			get { return Bounds.X; }
			set {
				if (Bounds.X == value)
					return;

				Bounds.X = value;
				NotifyValueChanged ("Left", Bounds.X);
				this.RegisterForLayouting ((int)LayoutingType.X);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Top {
			get { return Bounds.Y; }
			set {
				if (Bounds.Y == value)
					return;

				Bounds.Y = value;
				NotifyValueChanged ("Top", Bounds.Y);
				this.RegisterForLayouting ((int)LayoutingType.Y);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Width {
			get { return Bounds.Width; }
			set {
				if (Bounds.Width == value)
					return;

				Bounds.Width = value;
				NotifyValueChanged ("Width", Bounds.Width);
				this.RegisterForLayouting ((int)LayoutingType.Width);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Height {
			get { return Bounds.Height; }
			set {
				if (Bounds.Height == value)
					return;

				Bounds.Height = value;
				NotifyValueChanged ("Height", Bounds.Height);
				this.RegisterForLayouting ((int)LayoutingType.Height);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Fit {
			get { return Bounds.Width < 0 && Bounds.Height < 0 ? true : false; }
			set {
				if (value == Fit)
					return;

				Bounds.Width = Bounds.Height = -1;
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Focusable {
			get { return _focusable | Interface.DesignerMode; }
			set { _focusable = value; }
		}        
		[XmlAttributeAttribute()][DefaultValue("Transparent")]
		public virtual Color Background {
			get { return _background; }
			set {
				_background = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Color Foreground {
			get { return _foreground; }
			set {
				_foreground = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("droid,10")]
		public virtual Font Font {
			get { return _font; }
			set { _font = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(0.0)]
		public virtual double CornerRadius {
			get { return _cornerRadius; }
			set {
				_cornerRadius = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Margin {
			get { return _margin; }
			set {
				_margin = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool Visible {
			get { return _isVisible; }
			set {
				if (value == _isVisible)
					return;

				_isVisible = value;

				if (TopContainer == null)
					return;
				//add slot to clipping to redraw
				TopContainer.gobjsToRedraw.Add (this);

				//ensure main win doesn't keep hidden childrens ref
				if (this.Contains (TopContainer.hoverWidget))
					TopContainer.hoverWidget = null;
				if (Parent is GenericStack)
					Parent.RegisterForLayouting ((int)LayoutingType.Sizing | (int)LayoutingType.PositionChildren);
//					Parent.InvalidateLayout ();
				//else
				//    registerForRedraw();
			}
		}
		[XmlIgnore]public virtual bool HasFocus {
			get { return _hasFocus; }
			set { _hasFocus = value; }
		}
		//TODO: only used in group, should be removed from base go object
		[XmlIgnore]public virtual bool DrawingIsValid
		{ get { return bmp == null ? 
				false : 
				true; } }
		[XmlAttributeAttribute()][DefaultValue(null)]
		public virtual string BackgroundImagePath {
			get { return _backgroundImagePath; }
			set { 
				_backgroundImagePath = value;
				if (string.IsNullOrEmpty(_backgroundImagePath))					
					return;

				if (_backgroundImagePath.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
					_backgroundImage = new SvgPicture ();
				else 
					_backgroundImage = new BmpPicture ();

				_backgroundImage.LoadImage (_backgroundImagePath);
				//_backgroundImage.Scale = false;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()]
		public virtual Picture BackgroundImage {
			get { return _backgroundImage; }
			set { 
				_backgroundImage = value; 
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("0;0")]
		public virtual Size MaximumSize {
			get { return _maximumSize; }
			set { _maximumSize = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("0;0")]
		public virtual Size MinimumSize {
			get { return _minimumSize; }
			set { _minimumSize = value; }
		}
		object dataSource;

		[XmlIgnore]public virtual object DataSource {
			set {
				if (dataSource == value)
					return;

				if (dataSource != null)
					this.ClearBinding ();
				
				dataSource = value;

				if (dataSource != null)
					this.ResolveBindings();
			}
			get {				
				return dataSource == null ? 
					LogicalParent == null ?
					Parent is GraphicObject ? (Parent as GraphicObject).DataSource : null :  LogicalParent.DataSource : dataSource;
			}
		}
		#endregion

		/// <summary>
		/// allow selection of svg subobject to draw in goml, should be improved
		/// ex: allow access to backgroundImage.subimg from goml
		/// </summary>
		public string BackImgSub = null;

		/// <summary>
		/// Loads the default values from XML attributes default
		/// </summary>
		protected virtual void loadDefaultValues()
		{
			foreach (PropertyInfo pi in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;

				bool isAttribute = false;
				string name = "";
				Type valueType = null;

				MemberInfo mi = pi.GetGetMethod ();

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
					if (xia != null)
						continue;

					DefaultValueAttribute dv = o as DefaultValueAttribute;
					if (dv != null) {
						object defaultValue = dv.Value;
						//avoid system types automaticaly converted by parser
						if (defaultValue != null && !pi.PropertyType.Namespace.StartsWith("System")) {
							if (pi.PropertyType != defaultValue.GetType()) {
								MethodInfo miParse = pi.PropertyType.GetMethod ("Parse", BindingFlags.Static | BindingFlags.Public);
								if (miParse != null) {									
									pi.SetValue (this, miParse.Invoke (null, new object[]{ defaultValue }), null);
									continue;
								}
							}
						}
						pi.SetValue (this, defaultValue, null);	
					}						
				}
			}
		}

		public virtual GraphicObject FindByName(string nameToFind){
			return nameToFind == _name ? this : null;
		}
		public virtual bool Contains(GraphicObject goToFind){
			return false;
		}


		/// <summary>
		/// Clear chached object and add clipping region in redraw list of interface
		/// </summary>
		public virtual void registerForGraphicUpdate ()
		{
			bmp = null;
			if (TopContainer != null)
				TopContainer.gobjsToRedraw.Add (this);
		}
		/// <summary>
		/// Add clipping region in redraw list of interface, dont update cached object content
		/// </summary>
		public virtual void RegisterForRedraw ()
		{
			if (TopContainer != null)
				TopContainer.gobjsToRedraw.Add (this);
		}

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

		public virtual void registerClipRect()
		{
			TopContainer.redrawClip.AddRectangle (ScreenCoordinates(Slot));
			//this clipping should take only last painted slots in ancestor tree which
			//is not the case for now.
			TopContainer.redrawClip.AddRectangle (ScreenCoordinates(LastPaintedSlot));
		}
		/// <summary> return size of content + margins </summary>
		protected virtual Size measureRawSize ()
		{
			return Bounds.Size;
		}
		/// <summary> clear current layoutingQueue items for object and
		/// trigger a new layouting pass for a layoutType </summary>
		public virtual void RegisterForLayouting(int layoutType)
		{
			if (Parent == null)
				return;
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("RegisterForLayouting => {1}->{0}", layoutType, this.ToString());
			#endif
			lock (Interface.LayoutingQueue) {
				Interface.LayoutingQueue.RemoveAll (lq => lq.GraphicObject == this && (layoutType & (int)lq.LayoutType) > 0);

				if ((layoutType & (int)LayoutingType.Width) > 0) {
					if (Bounds.Width == 0) //stretch in parent
						Interface.LayoutingQueue.EnqueueAfterParentSizing (LayoutingType.Width, this);
					else if (Bounds.Width < 0) //fit 
						Interface.LayoutingQueue.EnqueueBeforeParentSizing (LayoutingType.Width, this);
					else
						Interface.LayoutingQueue.Insert (0, new LayoutingQueueItem (LayoutingType.Width, this));
				}

				if ((layoutType & (int)LayoutingType.Height) > 0) {
					if (Bounds.Height == 0) //stretch in parent
						Interface.LayoutingQueue.EnqueueAfterParentSizing (LayoutingType.Height, this);
					else if (Bounds.Height < 0) //fit 
						Interface.LayoutingQueue.EnqueueBeforeParentSizing (LayoutingType.Height, this);
					else
						Interface.LayoutingQueue.Insert (0, new LayoutingQueueItem (LayoutingType.Height, this));
				}

				if ((layoutType & (int)LayoutingType.X) > 0)
					//for x positionning, sizing of parent and this have to be done
					Interface.LayoutingQueue.EnqueueAfterThisAndParentSizing (LayoutingType.X, this);

				if ((layoutType & (int)LayoutingType.Y) > 0)
					//for x positionning, sizing of parent and this have to be done
					Interface.LayoutingQueue.EnqueueAfterThisAndParentSizing (LayoutingType.Y, this);
			}
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			if (Parent==null)
				return;
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("Layout change: " + this.ToString () + ":" + LastSlots.ToString() + "=>" + Slot.ToString ());
			#endif
			
			switch (layoutType) {
			case LayoutingType.Width:				
				if (Parent.getBounds ().Width < 0) {
					Group gw = Parent as Group;
					if (gw != null) {
						if (Slot.Width > gw.maxChildrenWidth)
							gw.maxChildrenWidth = Slot.Width;
					}
					this.Parent.RegisterForLayouting ((int)LayoutingType.Width);
				}else if (Width != 0) //update position in parent
					this.RegisterForLayouting ((int)LayoutingType.X);
				GenericStack gsw = Parent as GenericStack;
				if (gsw == null)
					break;	
				if ((Parent as GenericStack).Orientation == Orientation.Horizontal) {
//					ulong idx = (ulong)gsw.Children.IndexOf (this);
//					if (idx < gsw.stackingUpdateStartIndex)
//						gsw.stackingUpdateStartIndex = idx;
					this.Parent.RegisterForLayouting ((int)LayoutingType.PositionChildren);
				}
				break;
			case LayoutingType.Height:
				if (Parent.getBounds ().Height < 0) {
					Group gh = Parent as Group;
					if (gh != null) {
						if (Slot.Width > gh.maxChildrenHeight)
							gh.maxChildrenHeight = Slot.Height;
					}
					this.Parent.RegisterForLayouting ((int)LayoutingType.Height);
				}else if (Height != 0) //update position in parent
					this.RegisterForLayouting ((int)LayoutingType.Y);
				GenericStack gsh = Parent as GenericStack;
				if (gsh==null)
					break;				
				if (gsh.Orientation == Orientation.Vertical) {
//					ulong idx = (ulong)gsh.Children.IndexOf (this);
//					if (idx < gsh.stackingUpdateStartIndex)
//						gsh.stackingUpdateStartIndex = idx;
					this.Parent.RegisterForLayouting ((int)LayoutingType.PositionChildren);
				}
				break;
			}
			LayoutChanged.Raise (this, new LayoutChangeEventArgs (layoutType));
		}
		/// <summary> Update layout component, this is where the computation of alignement
		/// and size take place </summary>
		public virtual void UpdateLayout (LayoutingType layoutType)
		{		
			switch (layoutType) {
			case LayoutingType.X:
				if (Bounds.X == 0) {
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
					Slot.X = Bounds.X;

				if (LastSlots.X == Slot.X)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.X = Slot.X;
				break;
			case LayoutingType.Y:
				if (Bounds.Y == 0) {
					switch (VerticalAlignment) {
					case VerticalAlignment.Top:
						Slot.Y = 0;
						break;
					case VerticalAlignment.Bottom:
						Slot.Y = Parent.ClientRectangle.Height - Slot.Height;
						break;
					case VerticalAlignment.Center:
						Slot.Y = Parent.ClientRectangle.Height / 2 - Slot.Height / 2;
						break;
					}
				}else
					Slot.Y = Bounds.Y;

				if (LastSlots.Y == Slot.Y)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Y = Slot.Y;
				break;
			case LayoutingType.Width:
				//force sizing to fit if parent is sizing on children and
				//this object has stretched size
				if (Parent.getBounds ().Width < 0 && Width == 0)
					Width = -1;

				if (Width > 0)
					Slot.Width = Width;
				else if (Width < 0)
					Slot.Width = measureRawSize ().Width;
				else
					Slot.Width = Parent.ClientRectangle.Width;

				//size constrain
				if (Slot.Width < MinimumSize.Width)
					Slot.Width = MinimumSize.Width;
				else if (Slot.Width > MaximumSize.Width && MaximumSize.Width > 0)
					Slot.Width = MaximumSize.Width;

				if (LastSlots.Width == Slot.Width)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Width = Slot.Width;
				break;
			case LayoutingType.Height:
				//force sizing to fit if parent is sizing on children
				if (Parent.getBounds ().Height < 0 && Height == 0)
					Height = -1;

				if (Height > 0)
					Slot.Height = Height;
				else if (Height < 0)
					Slot.Height = measureRawSize ().Height;
				else
					Slot.Height = Parent.ClientRectangle.Height;

				//size constrain
				if (Slot.Height < MinimumSize.Height)
					Slot.Height = MinimumSize.Height;
				else if (Slot.Height > MaximumSize.Height && MaximumSize.Height > 0)
					Slot.Height = MaximumSize.Height;


				if (LastSlots.Height == Slot.Height)
					break;

				bmp = null;

				OnLayoutChanges (layoutType);

				LastSlots.Height = Slot.Height;
				break;
			}
			lock (Interface.LayoutingQueue) {
				//if no layouting remains in queue for item, registre for redraw
				if (Interface.LayoutingQueue.Where (lq => lq.GraphicObject == this).Count () <= 0 && bmp == null)
					this.RegisterForRedraw ();
			}
		}

		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		protected virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			gr.Color = Background;
			CairoHelpers.CairoRectangle(gr,rBack,_cornerRadius);
			gr.Fill ();

			if (BackgroundImage == null)
				return;

			BackgroundImage.Paint (gr, rBack, BackImgSub);
		}

		/// <summary>
		/// Interfal drawing context creation on a cached surface limited to slot size
		/// this trigger the effective drawing routine </summary>
		protected virtual void UpdateGraphic ()
		{
			LastPaintedSlot = Slot;

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
				//draw.WriteToPng ("/mnt/data/test.png");
			}
		}
		/// <summary> Chained painting routine on the parent context of the actual cached version
		/// of the widget </summary>
		public virtual void Paint (ref Context ctx, Rectangles clip = null)
		{
			if (!Visible)
				return;

			if (bmp == null)
				UpdateGraphic ();

			Rectangle rb = Parent.ContextCoordinates(Slot);

			using (ImageSurface source = new ImageSurface(bmp, Format.Argb32, Slot.Width, Slot.Height, 4 * Slot.Width)) {
				if (this.Background == Color.Clear) {
					ctx.Save ();
					ctx.Operator = Operator.Clear;
					ctx.Rectangle(rb);
					ctx.Fill ();
					ctx.Restore ();
				}
				ctx.SetSourceSurface (source, rb.X, rb.Y);
				ctx.Paint ();
			}
		}

        #region Keyboard handling
		public virtual void onKeyDown(object sender, KeyboardKeyEventArgs e){
			KeyDown.Raise (sender, e);
		}
        #endregion

		#region Mouse handling
		public virtual bool MouseIsIn(Point m)
		{
			if (!Visible) 
				return false;
			if (ScreenCoordinates (Slot).ContainsOrIsEqual (m)) {
				Scroller scr = Parent as Scroller;
				if (scr == null)
					return true;
				return scr.MouseIsIn (scr.savedMousePos);
			}
			return false; 
		}
		public virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			IGOLibHost glh = TopContainer;
			if (glh.hoverWidget != this) {
				glh.hoverWidget = this;
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
		public virtual void onMouseButtonUp(object sender, MouseButtonEventArgs e){
			if (MouseIsIn (e.Position))
				onMouseClick (sender, e);

			MouseButtonUp.Raise (this, e);
		}
		public virtual void onMouseButtonDown(object sender, MouseButtonEventArgs e){
			TopContainer.FocusedWidget = this;

			MouseButtonDown.Raise (this, e);
		}
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){	
			MouseClick.Raise (this,e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseWheel(this,e);
				
			MouseWheelChanged.Raise (this, e);
		}
		public virtual void onFocused(object sender, EventArgs e){
			Focused.Raise (this, e);
			this.HasFocus = true;
		}
		public virtual void onUnfocused(object sender, EventArgs e){
			Unfocused.Raise (this, e);
			this.HasFocus = false;
		}
		public virtual void onMouseEnter(object sender, MouseMoveEventArgs e)
		{
			MouseEnter.Raise (this, e);
		}
		public virtual void onMouseLeave(object sender, MouseMoveEventArgs e)
		{
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
		public virtual void ResolveBindings()
		{
			#if DEBUG_BINDING
			Debug.WriteLine ("ResolveBinding => " + this.ToString ());
			#endif
			List<Binding> resolved = new List<Binding> ();
			foreach (Binding b in Bindings) {
				if (!string.IsNullOrEmpty (b.DynMethodId))
					continue;
				if (b.Source.Member.MemberType == MemberTypes.Event) {
					if (b.Expression.StartsWith("{")){
						CompileEventSource(b);
						continue;
					}
				}
				if (!b.FindTarget ())
					continue;
				if (b.Source.Member.MemberType == MemberTypes.Event) {
					//register handler for event
					if (b.Target.Method == null) {
						Debug.WriteLine ("Handler Method not found: " + b.Expression);
						continue;
					}

					MethodInfo addHandler = b.Source.Event.GetAddMethod ();
					Delegate del = Delegate.CreateDelegate (b.Source.Event.EventHandlerType, b.Target.Instance, b.Target.Method);
					addHandler.Invoke (this, new object[] { del });
					continue;
				}
				resolved.Add (b);				
			}
			//group;only one dynMethods by target (valuechanged event source)
			//changed value name tested in switch
			IEnumerable<Binding[]> groupedByTarget = resolved.GroupBy (g => g.Target.Instance, g => g, (k, g) => g.ToArray ());
			foreach (Binding[] grouped in groupedByTarget) {
				int i = 0;
				Type targetType = grouped[0].Target.Instance.GetType();
				Type sourceType = this.GetType();

				DynamicMethod dm = null;
				ILGenerator il = null;

				MethodInfo stringEquals = typeof(string).GetMethod
					("op_Equality", new Type[2] {typeof(string), typeof(string)});


				System.Reflection.Emit.Label[] jumpTable = null;
				System.Reflection.Emit.Label endMethod = new System.Reflection.Emit.Label();

				#region Retrieve EventHandler parameter type
				EventInfo ei = targetType.GetEvent ("ValueChanged");
				//no dynamic update if ValueChanged interface is not implemented
				if (ei != null){
					MethodInfo evtInvoke = ei.EventHandlerType.GetMethod ("Invoke");
					ParameterInfo[] evtParams = evtInvoke.GetParameters ();
					Type handlerArgsType = evtParams [1].ParameterType;

					Type[] args = {typeof(object), typeof(object),handlerArgsType};
					dm = new DynamicMethod(grouped[0].NewDynMethodId,
						MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
						CallingConventions.Standard,
						typeof(void),
						args,
						sourceType,true);

					il = dm.GetILGenerator(256);

					endMethod = il.DefineLabel();
					jumpTable = new System.Reflection.Emit.Label[grouped.Length];
					for (i = 0; i < grouped.Length; i++)
						jumpTable [i] = il.DefineLabel ();
					il.DeclareLocal(typeof(string));
					il.DeclareLocal(typeof(object));

					il.Emit(OpCodes.Nop);
					il.Emit(OpCodes.Ldarg_0);
					//il.Emit(OpCodes.Isinst, sourceType);
					//push new value onto stack
					il.Emit(OpCodes.Ldarg_2);
					FieldInfo fiNewValue = typeof(ValueChangeEventArgs).GetField("NewValue");
					il.Emit(OpCodes.Ldfld, fiNewValue);
					il.Emit(OpCodes.Stloc_1);
					//push name 
					il.Emit(OpCodes.Ldarg_2);
					FieldInfo fiMbName = typeof(ValueChangeEventArgs).GetField("MemberName");
					il.Emit(OpCodes.Ldfld, fiMbName);
					il.Emit(OpCodes.Stloc_0);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Brfalse, endMethod);
				}
				#endregion

				i = 0;
				foreach (Binding b in grouped) {
					#region initialize target with actual value
					object targetValue = null;
					if (b.Target.Member != null){
						if (b.Target.Member.MemberType == MemberTypes.Property)
							targetValue = b.Target.Property.GetGetMethod ().Invoke (b.Target.Instance, null);
						else if (b.Target.Member.MemberType == MemberTypes.Field)
							targetValue = b.Target.Field.GetValue (b.Target.Instance);
						else if (b.Target.Member.MemberType == MemberTypes.Method){
							MethodInfo mthSrc = b.Target.Method;
							if (mthSrc.IsDefined(typeof(ExtensionAttribute), false))
								targetValue = mthSrc.Invoke(null, new object[] {b.Target.Instance});
							else
								targetValue = mthSrc.Invoke(b.Target.Instance, null);
						}else
							throw new Exception ("unandled source member type for binding");						
					}else if (string.IsNullOrEmpty(b.Expression))
						targetValue= grouped [0].Target.Instance;//empty binding exp=> bound to target object by default
					//TODO: handle other dest type conversions
					if (b.Source.Property.PropertyType == typeof(string)){
						if (targetValue == null){
							//set default value

						}else
							targetValue = targetValue.ToString ();
					}
					b.Source.Property.GetSetMethod ().Invoke (this, new object[] { targetValue });
					#endregion

					//if no dyn update, skip jump table
					if (il == null)
						continue;

					il.Emit (OpCodes.Ldloc_0);
					if (b.Target.Member != null)
						il.Emit (OpCodes.Ldstr, b.Target.Member.Name);
					else
						il.Emit (OpCodes.Ldstr, b.Expression.Split('/').LastOrDefault());
					il.Emit (OpCodes.Callvirt, stringEquals);
					il.Emit (OpCodes.Brtrue, jumpTable[i]);
					i++;
				}

				if (il == null)
					continue;
				
				il.Emit (OpCodes.Br, endMethod);

				i = 0;
				foreach (Binding b in grouped) {

					il.MarkLabel (jumpTable [i]);
					il.Emit(OpCodes.Ldloc_1);

					//by default, target value type is deducted from source member type to allow
					//memberless binding, if targetMember exists, it will be used to determine target
					//value type for conversion
					Type targetValueType = b.Source.Property.PropertyType;
					if (b.Target.Member != null) {
						if (b.Target.Member.MemberType == MemberTypes.Property)
							targetValueType = b.Target.Property.PropertyType;
						else if (b.Target.Member.MemberType == MemberTypes.Field)
							targetValueType = b.Target.Field.FieldType;
						else
							throw new Exception ("unhandle target member type in binding");
					}
					
					if (!targetValueType.IsValueType)
						il.Emit(OpCodes.Castclass, targetValueType);
					else if (b.Source.Property.PropertyType != targetValueType)
						il.Emit(OpCodes.Callvirt, CompilerServices.GetConvertMethod( b.Source.Property.PropertyType ));
					else
						il.Emit(OpCodes.Unbox_Any, b.Source.Property.PropertyType);

					il.Emit(OpCodes.Callvirt, b.Source.Property.GetSetMethod());
					il.Emit (OpCodes.Br, endMethod);
					i++;

				}
				il.MarkLabel(endMethod);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);

				Delegate del = dm.CreateDelegate(ei.EventHandlerType, this);
				MethodInfo addHandler = ei.GetAddMethod ();
				addHandler.Invoke(grouped [0].Target.Instance, new object[] {del});
			}
		}
		/// <summary>
		/// Compile events expression in GOML attributes
		/// </summary>
		/// <param name="es">Event binding details</param>
		public void CompileEventSource(Binding binding)
		{			
			Type sourceType = this.GetType();

			#region Retrieve EventHandler parameter type
			MethodInfo evtInvoke = binding.Source.Event.EventHandlerType.GetMethod ("Invoke");
			ParameterInfo[] evtParams = evtInvoke.GetParameters ();
			Type handlerArgsType = evtParams [1].ParameterType;
			#endregion

			Type[] args = {typeof(object), typeof(object),handlerArgsType};
			DynamicMethod dm = new DynamicMethod(binding.NewDynMethodId,
				typeof(void), 
				args,
				sourceType);


			#region IL generation
			ILGenerator il = dm.GetILGenerator(256);

			string src = binding.Expression.Trim();

			if (! (src.StartsWith("{") || src.EndsWith ("}"))) 
				throw new Exception (string.Format("GOML:Malformed {0} Event handler: {1}", binding.Source.Member.Name, binding.Expression));

			src = src.Substring (1, src.Length - 2);
			string[] srcLines = src.Split (new char[] { ';' });

			foreach (string srcLine in srcLines) {
				string statement = srcLine.Trim ();

				string[] operandes = statement.Split (new char[] { '=' });
				if (operandes.Length < 2) //not an affectation
				{
					continue;
				}
				string lop = operandes [0].Trim ();
				string rop = operandes [operandes.Length-1].Trim ();

				#region LEFT OPERANDES
				GraphicObject lopObj = this;	//default left operand base object is 
				//the first arg (object sender) of the event handler

				string[] lopParts = lop.Split (new char[] { '.' });
				if (lopParts.Length == 2) {//should search also for member of es.Source
					lopObj = this.FindByName (lopParts [0]);
					if (lopObj==null)
						throw new Exception (string.Format("GOML:Unknown name: {0}", lopParts[0]));
					//TODO: should create private member holding ref of lopObj, and emit
					//a call to FindByName(lopObjName) during #ctor or in a onLoad func or evt handler
					throw new Exception (string.Format("GOML:obj tree ref not yet implemented", lopParts[0]));
				}else
					il.Emit(OpCodes.Ldarg_0);	//load sender ref onto the stack

				int i = lopParts.Length -1;

				MemberInfo lopMbi = lopObj.GetType().GetMember (lopParts[i])[0];
				OpCode lopSetOC;
				dynamic lopSetMbi;
				Type lopT = null;
				switch (lopMbi.MemberType) {
				case MemberTypes.Property:
					PropertyInfo lopPi = sourceType.GetProperty (lopParts[i]);
					MethodInfo dstMi = lopPi.GetSetMethod ();
					lopT = lopPi.PropertyType;
					lopSetMbi = dstMi;
					lopSetOC = OpCodes.Callvirt;
					break;
				case MemberTypes.Field:
					FieldInfo dstFi = sourceType.GetField(lopParts[i]);
					lopT = dstFi.FieldType;
					lopSetMbi = dstFi;
					lopSetOC = OpCodes.Stfld;
					break;
				default:
					throw new Exception (string.Format("GOML:member type not handle: {0}", lopParts[i]));
				}  
				#endregion

				#region RIGHT OPERANDES
				if (rop.StartsWith("\'")){
					if (!rop.EndsWith("\'"))
						throw new Exception (string.Format
							("GOML:malformed string constant in handler: {0}", rop));	
					string strcst = rop.Substring (1, rop.Length - 2);

					il.Emit(OpCodes.Ldstr,strcst);

				}else{
					//search for a static field in left operand type named 'rop name'
					FieldInfo ropFi = lopT.GetField (rop, BindingFlags.Static|BindingFlags.Public);
					if (ropFi != null)
					{
						il.Emit (OpCodes.Ldsfld, ropFi);
					}else{
						//search if parsing methods are present
						MethodInfo lopTryParseMi = lopT.GetMethod("TryParse");
						//TODO
					}
				}

				#endregion

				//emit left operand assignment
				il.Emit(lopSetOC, lopSetMbi);
			}

			il.Emit(OpCodes.Ret);

			#endregion

			Delegate del = dm.CreateDelegate(binding.Source.Event.EventHandlerType,this);
			MethodInfo addHandler = binding.Source.Event.GetAddMethod ();
			addHandler.Invoke(this, new object[] {del});
		}
		/// <summary>
		/// Remove dynamic delegates by ids from dataSource
		///  and delete ref of this in Shared interface refs
		/// </summary>
		public virtual void ClearBinding(){
			foreach (Binding b in Bindings) {
				if (string.IsNullOrEmpty (b.DynMethodId))
					continue;
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
			}
		}
		#endregion

		#region IXmlSerializable
		public virtual System.Xml.Schema.XmlSchema GetSchema ()
		{
			return null;
		}
		public virtual void ReadXml (System.Xml.XmlReader reader)
		{
			if (!reader.HasAttributes)
				return;
			Type thisType = this.GetType ();
			while (reader.MoveToNextAttribute ()) {
				string attName = reader.Name;
				string attValue = reader.Value;

				if (string.IsNullOrEmpty (attValue))
					continue;
				
				MemberInfo mi = thisType.GetMember (attName).FirstOrDefault();
				if (mi == null) {
					Debug.WriteLine ("GOML: Unknown attribute in " + thisType.ToString() + " : " + attName);
					continue;
				}
				if (mi.MemberType == MemberTypes.Event) {
					this.Bindings.Add (new Binding (new MemberReference(this, mi), attValue));
					continue;
				}
				if (mi.MemberType == MemberTypes.Property) {
					PropertyInfo pi = mi as PropertyInfo;

					if (pi.GetSetMethod () == null)
						continue;

					bool isAttribute = false;
					object defaultValue = null;

					foreach (object attrib in pi.GetCustomAttributes ()) {
						XmlAttributeAttribute xaa = attrib as XmlAttributeAttribute;
						if (xaa != null) {
							isAttribute = true;
							if (!string.IsNullOrEmpty (xaa.AttributeName))
								attName = xaa.AttributeName;
							continue;
						}
						if (attrib is XmlIgnoreAttribute)
							break;
						DefaultValueAttribute dv = attrib as DefaultValueAttribute;
						if (dv != null)
							defaultValue = dv.Value;						
					}
					if (!isAttribute)
						continue;
//					{
//						//avoid system types automaticaly converted by parser
//						if (defaultValue != null && !pi.PropertyType.Namespace.StartsWith("System")) {
//							if (pi.PropertyType != defaultValue.GetType()) {
//								MethodInfo miParse = pi.PropertyType.GetMethod ("Parse", BindingFlags.Static | BindingFlags.Public);
//								if (miParse != null) {									
//									pi.SetValue (this, miParse.Invoke (null, new object[]{ defaultValue }), null);
//									continue;
//								}
//							}
//						}
//						pi.SetValue (this, defaultValue, null);
//					} else {

					if (attValue.StartsWith("{")) {
						//binding
						if (!attValue.EndsWith("}"))
							throw new Exception (string.Format("GOML:Malformed binding: {0}", attValue));

						this.Bindings.Add (new Binding (new MemberReference(this, pi), attValue.Substring (1, attValue.Length - 2)));
						continue;
					}

					if (pi.PropertyType == typeof(string)) {
						pi.SetValue (this, attValue, null);
						continue;
					}

					object o = null;

					if (pi.PropertyType.IsEnum) {
						pi.SetValue (this, Enum.Parse (pi.PropertyType, attValue), null);
					} else {
						MethodInfo me = pi.PropertyType.GetMethod ("Parse", new Type[] { typeof(string) });
						pi.SetValue (this, me.Invoke (null, new string[] { attValue }), null);
					}

					 

				}
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

	}
}
