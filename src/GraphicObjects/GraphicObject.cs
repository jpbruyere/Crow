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
	public class GraphicObject : IXmlSerializable, ILayoutable
	{
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
		double _cornerRadius;
		int _margin;
		bool _focusable = false;
		bool _hasFocus = false;
		protected bool _isVisible = true;
		VerticalAlignment _verticalAlignment;
		HorizontalAlignment _horizontalAlignment;

		bool hIsValid = false;
		bool wIsValid = false;
		bool xIsValid = false;
		bool yIsValid = false;
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
		[XmlIgnore]public virtual bool SizeIsValid {
			get { return hIsValid & wIsValid; }
			set {
				hIsValid = value;
				wIsValid = value;
			}
		}
		[XmlIgnore]public virtual bool WIsValid {
			get { return wIsValid; }
			set { wIsValid = value; }
		}
		[XmlIgnore]public virtual bool HIsValid {
			get { return hIsValid; }
			set { hIsValid = value; }
		}
		[XmlIgnore]public virtual bool XIsValid {
			get { return xIsValid; }
			set { xIsValid = value; }
		}
		[XmlIgnore]public virtual bool YIsValid {
			get { return yIsValid; }
			set { yIsValid = value; }
		}
		[XmlIgnore]public virtual bool PositionIsValid {
			get { return xIsValid & yIsValid; }
			set {
				xIsValid = value;
				yIsValid = value;
			}
		}
		[XmlIgnore]public virtual bool LayoutIsValid {
			get { return SizeIsValid & PositionIsValid; }
			set {
				if (value == SizeIsValid & PositionIsValid)
					return;

				SizeIsValid = value;
				PositionIsValid = value;

				//if (!layoutIsValid && Parent != null)
				//    Parent.layoutIsValid = false;
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
			get { return Parent.TopContainer; }
		}
		public virtual void InvalidateLayout ()
		{
			bmp = null;
			LayoutIsValid = false;
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
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue("unamed")]
		public virtual string Name {
			get { return _name; }
			set { _name = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(VerticalAlignment.Center)]
		public virtual VerticalAlignment VerticalAlignment {
			get { return _verticalAlignment; }
			set { 
				if (_verticalAlignment == value)
					return;

				_verticalAlignment = value; 

//				if (Top > 0)
			}
		}
		[XmlAttributeAttribute()][DefaultValue(HorizontalAlignment.Center)]
		public virtual HorizontalAlignment HorizontalAlignment {
			get { return _horizontalAlignment; }
			set { _horizontalAlignment = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Left {
			get { return Bounds.X; }
			set {
				if (Bounds.X == value)
					return;

				Bounds.X = value;

				LayoutIsValid = false;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Top {
			get { return Bounds.Y; }
			set {
				if (Bounds.Y == value)
					return;

				Bounds.Y = value;

				LayoutIsValid = false;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Width {
			get { return Bounds.Width; }
			set {
				if (Bounds.Width == value)
					return;

				Bounds.Width = value;

				InvalidateLayout ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Height {
			get { return Bounds.Height; }
			set {
				if (Bounds.Height == value)
					return;

				Bounds.Height = value;

				InvalidateLayout ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Fit {
			get { return Bounds.Width < 0 && Bounds.Height < 0 ? true : false; }
			set {
				if (value == Fit)
					return;

				Bounds.Width = Bounds.Height = -1;
				InvalidateLayout ();
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
		[XmlAttributeAttribute()][DefaultValue(5)]
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

				//add slot to clipping to redraw
				TopContainer.gobjsToRedraw.Add (this);

				//ensure main win doesn't keep hidden childrens ref
				if (this.Contains (TopContainer.hoverWidget))
					TopContainer.hoverWidget = null;
//				if (Parent != null)
//					Parent.InvalidateLayout ();
				//else
				//    registerForRedraw();
			}
		}
		[XmlIgnore]public virtual bool HasFocus {
			get { return _hasFocus; }
			set { _hasFocus = value; }
		}
		[XmlIgnore]public virtual bool DrawingIsValid
		{ get { return bmp == null ? 
				false : 
				true; } }
		#endregion

		/// <summary>
		/// Loads the default values from XML attributes default
		/// </summary>
		void loadDefaultValues()
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
						if (pi.PropertyType == typeof(Color))
							pi.SetValue (this, Color.Parse ((string)dv.Value), null);
						else if (pi.PropertyType == typeof(Font))
							pi.SetValue (this, Font.Parse ((string)dv.Value), null);
						else
							pi.SetValue (this, dv.Value, null);
						continue;
					}

					//Debug.WriteLine (o.ToString ());
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
			registerForRedraw ();
			//Interface.registerForGraphicUpdate(this);
		}
		/// <summary>
		/// Add clipping region in redraw list of interface, dont update cached object content
		/// </summary>
		public virtual void registerForRedraw ()
		{
			if (LayoutIsValid && Visible)
				TopContainer.gobjsToRedraw.Add (this);
		}
		public virtual void registerClipRect()
		{
			TopContainer.redrawClip.AddRectangle (ScreenCoordinates(Slot));
		}
		protected virtual Size measureRawSize ()
		{
			return Bounds.Size;
		}

		protected virtual void ComputeSize()
		{
			Size rawSize = measureRawSize ();

			if (!wIsValid) {
				wIsValid = true;
				if (Width > 0)
					Slot.Width = Width;
				else if (Width < 0) {
					if (rawSize.Width > 0)
						Slot.Width = rawSize.Width;
					else
						wIsValid = false;
				} else if (Parent.WIsValid)
					Slot.Width = Parent.ClientRectangle.Width;
				else
					wIsValid = false;				
			}

			if (!hIsValid) {
				hIsValid = true;
				if (Height > 0)
					Slot.Height = Height;
				else if (Height < 0) {
					if (rawSize.Height > 0)
						Slot.Height = rawSize.Height;
					else
						hIsValid = false;
				} else if (Parent.HIsValid)
					Slot.Height = Parent.ClientRectangle.Height;
				else
					hIsValid = false;				
			}
		}
		protected virtual void ComputePosition()
		{
			if (!xIsValid) {
				xIsValid = true;
				if (Width == 0)
					Slot.X = 0;
				else if (Left != 0)
					Slot.X = Left;
				else if (Parent.WIsValid && WIsValid) {
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
				} else// if (Parent.getBounds().Width>=0)
					xIsValid = false;
			}

			if (!yIsValid) {
				yIsValid = true;
				if (Height== 0)
					Slot.Y = 0;
				else if (Top != 0)
					Slot.Y = Top;
				else if (Parent.HIsValid && HIsValid) {
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
				} else// if (Parent.getBounds().Height>=0)
					yIsValid = false;
			}
		}
		public virtual void UpdateLayout ()
		{
			if (!SizeIsValid)
				ComputeSize ();
			if (!PositionIsValid)
				ComputePosition ();				
			if (LayoutIsValid)
				registerForRedraw ();

		}
		protected virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			gr.Color = Background;
			CairoHelpers.CairoRectangle(gr,rBack,_cornerRadius);
			gr.Fill ();
		}
		protected virtual void UpdateGraphic ()
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
			return Visible & Focusable ? ScreenCoordinates(Slot).ContainsOrIsEqual (m) : false; 
		}
		public virtual void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			//ILayoutable w = sender as ILayoutable;

			if (TopContainer.hoverWidget != this) {
				TopContainer.hoverWidget = this;
				onMouseEnter (sender, e);
			}
				
			MouseMove.Raise (this, e);
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
			Debug.WriteLine ("Mouse enter: " + this.Name);
			MouseEnter.Raise (this, e);
		}
		public virtual void onMouseLeave(object sender, MouseMoveEventArgs e)
		{
			Debug.WriteLine ("Mouse leave: " + this.Name);
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
		//TODO: allert or inform when unknow attribute name in XML, very confusing...
		public virtual void ReadXml (System.Xml.XmlReader reader)
		{
			foreach (EventInfo ei in this.GetType().GetEvents()) {
//				FieldInfo fi = this.GetType().GetField(ei.Name,
//					BindingFlags.NonPublic |
//					BindingFlags.Instance |
//					BindingFlags.GetField);
				string handler = reader.GetAttribute(ei.Name);
				if (string.IsNullOrEmpty (handler))
					continue;
					
				Interface.EventsToResolve.Add(new EventSource 
					{ 
						Source = this, 
						Handler = handler,
						EventName = ei.Name
					});
			}
			foreach (PropertyInfo pi in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (pi.GetSetMethod () == null)
					continue;

				bool isAttribute = false;

				string name = "";
				object value = null;
				object defaultValue = null;

				Type valueType = null;


				MemberInfo mi = pi.GetGetMethod ();

				//if (mi.MemberType == MemberTypes.Property)
				//{
				//    PropertyInfo pi = mi as PropertyInfo;
				//    value = pi.GetValue(this, null);
				//    valueType = pi.PropertyType;
				//}
				//else if (mi.MemberType == MemberTypes.Field)
				//{
				//    FieldInfo fi = mi as FieldInfo;
				//    value = fi.GetValue(this);
				//    valueType = fi.FieldType;
				//}

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
						defaultValue = dv.Value;
						continue;
					}
				}


				if (isAttribute) {
					string v = reader.GetAttribute (name);

					if (string.IsNullOrEmpty (v)) {
						//TODO: maybe find another way to convert correctely colors default values
						if (pi.PropertyType == typeof(Color))
							pi.SetValue (this, Color.Parse ((string)defaultValue), null);
						else if (pi.PropertyType == typeof(Font))
							pi.SetValue (this, Font.Parse ((string)defaultValue), null);
						else
							pi.SetValue (this, defaultValue, null);
					} else {
						if (pi.PropertyType == typeof(string)) {
							pi.SetValue (this, v, null);
							continue;
						}

						object o = null;

						if (pi.PropertyType.IsEnum) {
							o = Enum.Parse (pi.PropertyType, v);
						} else {
							MethodInfo me = pi.PropertyType.GetMethod ("Parse", new Type[] { typeof(string) });
							o = me.Invoke (null, new string[] { v });
						}

						pi.SetValue (this, o, null);
					}
				} else {
					////if member type is not serializable, cancel
					//if (valueType.GetInterface("IXmlSerializable") == null)
					//    continue;

					//(pi.GetValue(this, null) as IXmlSerializable).WriteXml(writer);
				}
			}

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
