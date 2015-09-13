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

namespace go
{
	public class GraphicObject : IXmlSerializable, ILayoutable, IValueChange
	{
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
			loadDefaultValues ();
			registerForGraphicUpdate ();
		}
		public GraphicObject (Rectangle _bounds)
		{
			loadDefaultValues ();
			Bounds = _bounds;
			registerForGraphicUpdate ();
		}
		#endregion

		#region private fields
		ILayoutable _parent;
		string _name;
		Color _background;
		Color _foreground;
		Font _font;
		double _cornerRadius;
		int _margin;
		bool _focusable = false;
		bool _hasFocus = false;
		protected bool _isVisible = true;
		VerticalAlignment _verticalAlignment;
		HorizontalAlignment _horizontalAlignment;
		Size _maximumSize;
		Size _minimumSize;

		Picture _backgroundImage;
		string _template;
		#endregion

		#region public fields
		public Rectangle Bounds;
		public Rectangle Slot = new Rectangle ();
		public object Tag;
		public byte[] bmp;
		#endregion

		#region ILayoutable
		[XmlIgnore]public ILayoutable Parent { 
			get { return _parent; }
			set { _parent = value; }
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
		[XmlAttributeAttribute()][DefaultValue(2.0)]
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
		public virtual Picture BackgroundImage {
			get { return _backgroundImage; }
			set { 
				_backgroundImage = value; 
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("0;0")]
		public Size MaximumSize {
			get { return _maximumSize; }
			set { _maximumSize = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("0;0")]
		public Size MinimumSize {
			get { return _minimumSize; }
			set { _minimumSize = value; }
		}
		[XmlIgnore]public object DataSource;
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
			
			Interface.LayoutingQueue.RemoveAll (lq => lq.GraphicObject == this && (layoutType & (int)lq.LayoutType) > 0);

			if ((layoutType & (int)LayoutingType.Width) > 0) {

				//force sizing to fit if parent is sizing on children and 
				//this object has stretched size
				if (Parent.getBounds ().Width < 0 && Width == 0)
					Width = -1;
				
				if (Bounds.Width == 0) //stretch in parent
					Interface.LayoutingQueue.EnqueueAfterParentSizing (LayoutingType.Width, this);
				else if (Bounds.Width < 0) //fit 
					Interface.LayoutingQueue.EnqueueBeforeParentSizing (LayoutingType.Width, this);				
				else
					Interface.LayoutingQueue.Insert (0, new LayoutingQueueItem (LayoutingType.Width, this));
			}

			if ((layoutType & (int)LayoutingType.Height) > 0) {

				//force sizing to fit if parent is sizing on children
				if (Parent.getBounds ().Height < 0 && Height == 0)
					Height = -1;

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

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			if (Parent==null)
				return;
			
			switch (layoutType) {
			case LayoutingType.Width:				
				if (Parent.getBounds ().Width < 0)
					this.Parent.RegisterForLayouting ((int)LayoutingType.Width);
				else if (Width != 0) //update position in parent
					this.RegisterForLayouting ((int)LayoutingType.X);
				if (!(Parent is GenericStack))
					break;
				if ((Parent as GenericStack).Orientation == Orientation.Horizontal)
					this.Parent.RegisterForLayouting ((int)LayoutingType.PositionChildren);
				break;
			case LayoutingType.Height:
				if (Parent.getBounds().Height < 0)
					this.Parent.RegisterForLayouting((int)LayoutingType.Height);
				else if (Height != 0) //update position in parent
					this.RegisterForLayouting ((int)LayoutingType.Y);
				if (!(Parent is GenericStack))
					break;
				if ((Parent as GenericStack).Orientation == Orientation.Vertical)
					this.Parent.RegisterForLayouting ((int)LayoutingType.PositionChildren);
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

			//if no layouting remains in queue for item, registre for redraw
			if (Interface.LayoutingQueue.Where (lq => lq.GraphicObject == this).Count () <= 0 && bmp == null)
				this.RegisterForRedraw ();
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
		/// Interfal drawing context creation on a chached surface limited to slot size
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

			using (ImageSurface source = new ImageSurface(bmp, Format.Argb32, rb.Width, rb.Height, 4 * Slot.Width)) {
				ctx.SetSourceSurface (source, rb.X, rb.Y);
				ctx.Paint ();
			}
		}

        #region Keyboard handling
		public virtual void onKeyDown(object sender, KeyboardKeyEventArgs e){
			if (KeyDown != null)
				KeyDown (sender, e);
		}
        #endregion

		#region Mouse handling
		public virtual bool MouseIsIn(Point m)
		{
			return Visible ? ScreenCoordinates(Slot).ContainsOrIsEqual (m) : false; 
		}
		internal virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			if (TopContainer.hoverWidget != this) {
				TopContainer.hoverWidget = this;
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
			return Name == "unamed" ? this.GetType ().ToString() : Name;
		}
			
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
				MemberInfo mi = thisType.GetMember (attName).FirstOrDefault();
				if (mi == null) {
					Debug.WriteLine (Interface.CurrentGOMLPath + "=>GOML: Unknown attribute in " + thisType.ToString() + " : " + attName);
					continue;
				}
				if (mi.MemberType == MemberTypes.Event) {
					Interface.GOMLResolver.Add (new DynAttribute { 
						Source = this, 
						Value = attValue,
						MemberName = attName
					});
				} else if (mi.MemberType == MemberTypes.Property) {
					PropertyInfo pi = mi as PropertyInfo;

					if (pi.GetSetMethod () == null)
						continue;

					bool isAttribute = false;
					object defaultValue = null;

					foreach (object o in pi.GetCustomAttributes ()) {
						XmlAttributeAttribute xaa = o as XmlAttributeAttribute;
						if (xaa != null) {
							isAttribute = true;
							if (!string.IsNullOrEmpty (xaa.AttributeName))
								attName = xaa.AttributeName;
							continue;
						}
						if (o is XmlIgnoreAttribute)
							break;
						DefaultValueAttribute dv = o as DefaultValueAttribute;
						if (dv != null)
							defaultValue = dv.Value;						
					}
					if (!isAttribute)
						continue;
					
					if (string.IsNullOrEmpty (attValue)) {
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
					} else {

						if (attValue.StartsWith("{")) {
							//binding
							if (!attValue.EndsWith("}"))
								throw new Exception (string.Format("GOML:Malformed binding: {0}", attValue));

							string strBinding = attValue.Substring (1, attValue.Length - 2);
							Interface.GOMLResolver.Add (new DynAttribute () {
								Source = this,
								MemberName = attName,
								Value = strBinding
							});
							continue;
						}

						if (pi.PropertyType == typeof(string)) {
							pi.SetValue (this, attValue, null);
							continue;
						}

						object o = null;

						if (pi.PropertyType.IsEnum) {
							o = Enum.Parse (pi.PropertyType, attValue);
						} else {
							MethodInfo me = pi.PropertyType.GetMethod ("Parse", new Type[] { typeof(string) });
							o = me.Invoke (null, new string[] { attValue });
						}

						pi.SetValue (this, o, null);
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
