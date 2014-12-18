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

namespace go
{
	public class GraphicObject : IXmlSerializable, ILayoutable
	{

		#region private properties

		ILayoutable _parent;
		string _name;
		Color _background;
		Color _foreground;
		Color _borderColor;
		int _borderWidth;
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

		public Rectangle Bounds;
		public Rectangle Slot = new Rectangle ();
		public object Tag;
		public byte[] bmp;

		#region ILayoutable

		[System.Xml.Serialization.XmlIgnore]
		public ILayoutable Parent { 
			get { return _parent; }
			set { _parent = value; }
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool SizeIsValid {
			get { return hIsValid & wIsValid; }
			set {
				hIsValid = value;
				wIsValid = value;
			}
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool WIsValid {
			get { return wIsValid; }
			set { wIsValid = value; }
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool HIsValid {
			get { return hIsValid; }
			set { hIsValid = value; }
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool XIsValid {
			get { return xIsValid; }
			set { xIsValid = value; }
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool YIsValid {
			get { return yIsValid; }
			set { yIsValid = value; }
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool PositionIsValid {
			get { return xIsValid & yIsValid; }
			set {
				xIsValid = value;
				yIsValid = value;
			}
		}
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool LayoutIsValid {
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

		public virtual void InvalidateLayout ()
		{
			bmp = null;
			LayoutIsValid = false;
		}

		[System.Xml.Serialization.XmlIgnore]
		public virtual Rectangle ClientRectangle {
			get {
				Rectangle cb = Slot.Size;
				cb.Inflate ( - BorderWidth - Margin);
				return cb;
			}
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
		#endregion

		#region event handlers

		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged = delegate { };
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp = delegate { };
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown = delegate { };
		public event EventHandler<MouseButtonEventArgs> MouseClicked = delegate { };
		public event EventHandler<MouseMoveEventArgs> MouseMove = delegate { };
		public event EventHandler<MouseMoveEventArgs> MouseEnter = delegate { };
		public event EventHandler<MouseMoveEventArgs> MouseLeave = delegate { };
		public event EventHandler<KeyboardKeyEventArgs> KeyDown = delegate { };
		public event EventHandler<KeyboardKeyEventArgs> KeyUp = delegate { };
		public event EventHandler Focused = delegate { };
		public event EventHandler Unfocused = delegate { };

		#endregion

		#region public properties

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("unamed")]
		public virtual string Name {
			get { return _name; }
			set { _name = value; }
		}
		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(VerticalAlignment.Center)]
		public virtual VerticalAlignment VerticalAlignment {
			get { return _verticalAlignment; }
			set { 
				if (_verticalAlignment == value)
					return;

				_verticalAlignment = value; 

//				if (Top > 0)
			}
		}
		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(HorizontalAlignment.Center)]
		public virtual HorizontalAlignment HorizontalAlignment {
			get { return _horizontalAlignment; }
			set { _horizontalAlignment = value; }
		}
		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
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

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
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

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public virtual int Width {
			get { return Bounds.Width; }
			set {
				if (Bounds.Width == value)
					return;

				Bounds.Width = value;

				InvalidateLayout ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public virtual int Height {
			get { return Bounds.Height; }
			set {
				if (Bounds.Height == value)
					return;

				Bounds.Height = value;

				InvalidateLayout ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(false)]
		public virtual bool Focusable {
			get { return _focusable; }
			set { _focusable = value; }
		}
        
		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("Transparent")]
		public virtual Color Background {
			get { return _background; }
			set {
				_background = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("White")]
		public virtual Color Foreground {
			get { return _foreground; }
			set {
				_foreground = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue("White")]
		public virtual Color BorderColor {
			get { return _borderColor; }
			set {
				_borderColor = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public virtual int BorderWidth {
			get { return _borderWidth; }
			set {
				_borderWidth = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(5)]
		public virtual double CornerRadius {
			get { return _cornerRadius; }
			set {
				_cornerRadius = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(0)]
		public virtual int Margin {
			get { return _margin; }
			set {
				_margin = value;
				registerForGraphicUpdate ();
			}
		}

		[System.Xml.Serialization.XmlAttributeAttribute()]
		[System.ComponentModel.DefaultValue(true)]
		public virtual bool Visible {
			get { return _isVisible; }
			set {
				if (value == _isVisible)
					return;

				_isVisible = value;
				if (Parent != null)
					Parent.InvalidateLayout ();
				//else
				//    registerForRedraw();
			}
		}
		//should maybe test only focusedWidget in higher container (opentkwin for now)
		[System.Xml.Serialization.XmlIgnore]
		public virtual bool HasFocus {
			get { return _hasFocus; }
			set { _hasFocus = value; }
		}

		[System.Xml.Serialization.XmlIgnore]
		public virtual bool isCached {
			get { return false; }
		}

		[System.Xml.Serialization.XmlIgnore]
		public virtual bool cachingInProgress {
			get { return false; }
			set { return; }
		}

		public virtual Rectangle clientBounds {
			get {
				Rectangle cb = Slot.Size;
				cb.Inflate (-(BorderWidth + Margin));
				return cb;
			}
		}






		public virtual Rectangle getSlot()
		{
			return Slot;
		}
		[XmlIgnore]
		public virtual bool DrawingIsValid
		{ get { return bmp == null ? 
				false : 
				true; } }
		#endregion

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
				OpenTKGameWindow.gobjsToRedraw.Add (this);
		}
		public virtual void registerClipRect()
		{
			OpenTKGameWindow.redrawClip.AddRectangle (ScreenCoordinates(Slot));
		}
		public virtual Size measureRawSize ()
		{
			return Bounds.Size;
		}

		public virtual void ComputeSize()
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
		public virtual void ComputePosition()
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
				} else
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
				} else
					yIsValid = false;
			}
		}
		public virtual void UpdateLayout ()
		{
//			if (LayoutIsValid)
//				return;
//
			//should maybe invalidate drawing

			//Rectangle oldRenderBounds = Slot;

			if (!SizeIsValid)
				ComputeSize ();
			if (!PositionIsValid)
				ComputePosition ();				
			if (LayoutIsValid)
				registerForRedraw ();

		}
		public virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (Slot.Size);

			if (BorderWidth > 0) 
				rBack.Inflate (-BorderWidth / 2);

			gr.Color = Background;
			CairoHelpers.CairoRectangle(gr,rBack,_cornerRadius);
			gr.Fill ();

			if (BorderWidth > 0) {

				gr.LineWidth = BorderWidth;
				gr.Color = BorderColor;
				CairoHelpers.CairoRectangle(gr,rBack,_cornerRadius);
				gr.Stroke ();
			}
		}

		internal virtual void UpdateGraphic ()
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

		//could maybe be internal
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
		public virtual void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			OpenTKGameWindow w = sender as OpenTKGameWindow;

			if (w.hoverWidget != this) {
				w.hoverWidget = this;
				onMouseEnter (sender, e);
			}

			if (MouseMove != null)
				MouseMove (sender, e);
		}
		public virtual void onMouseButtonUp(object sender, MouseButtonEventArgs e){
			if (MouseIsIn (e.Position))
				onMouseClicked (sender, e);
			if (MouseButtonUp != null)
				MouseButtonUp (sender, e);
		}
		public virtual void onMouseButtonDown(object sender, MouseButtonEventArgs e){
			(sender as OpenTKGameWindow).FocusedWidget = this;
			if (MouseButtonDown!=null)
				MouseButtonDown (sender, e);
		}
		public virtual void onMouseClicked(object sender, MouseButtonEventArgs e){	
			if (MouseClicked!=null)
				MouseClicked(sender,e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){
			GraphicObject p = Parent as GraphicObject;
			if (p != null)
				p.onMouseWheel(this,e);

			if (MouseWheelChanged!=null)
				MouseWheelChanged (this, e);
		}
		public virtual void onFocused(object sender, EventArgs e){
			if (Focused!=null)
				Focused (sender, e);
			this.HasFocus = true;
		}
		public virtual void onUnfocused(object sender, EventArgs e){
			if (Unfocused!=null)
				Unfocused (sender, e);
			this.HasFocus = false;
		}
		public virtual void onMouseEnter(object sender, MouseMoveEventArgs e)
		{
			//Debug.WriteLine ("Mouse Enter: " + this.ToString ());
			if (MouseEnter != null)
				MouseEnter (this, e);
		}
		public virtual void onMouseLeave(object sender, MouseMoveEventArgs e)
		{
			if (MouseLeave != null)
				MouseLeave (this, e);
		}
		#endregion

		#region Load/Save

		internal static List<EventSource> EventsToResolve;

		public static void Save<T>(string file, T graphicObject)
		{            
			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(typeof(T));

			xs = new XmlSerializer(typeof(T));
			using (Stream s = new FileStream(file, FileMode.Create))
			{
				xs.Serialize(s, graphicObject, xn);
			}
		}			
		public static void Load<T>(string file, out T result, object ClassContainingHandlers = null)
		{
			EventsToResolve = new List<EventSource>();

			XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
			xn.Add("", "");
			XmlSerializer xs = new XmlSerializer(typeof(T));            

			using (Stream s = new FileStream(file, FileMode.Open))
			{
				result = (T)xs.Deserialize(s);
			}

			if (ClassContainingHandlers == null)
				return;

			foreach (EventSource es in EventsToResolve)
			{
				if (es.Handler == null)
					continue;
					
				MethodInfo mi = ClassContainingHandlers.GetType().GetMethod(es.Handler, BindingFlags.NonPublic | BindingFlags.Public
					| BindingFlags.Instance);

				if (mi == null) {
					Debug.WriteLine ("Handler Method not found: " + es.Handler);
					continue;
				}
				FieldInfo fi;
				Type ty = es.Source.GetType ();
				do {
					fi = ty.GetField (es.EventName,
						BindingFlags.NonPublic |
						BindingFlags.Instance |
						BindingFlags.GetField);
					ty = ty.BaseType;
					if (ty == null)
						break;
				} while(fi == null);
				Delegate del = Delegate.CreateDelegate(fi.FieldType, ClassContainingHandlers, mi);
				fi.SetValue(es.Source, del);

			}

		}

		#endregion

		public override string ToString ()
		{
			return Name == "unamed" ? this.GetType ().ToString() : Name;
//			string tmp = this.GetType ().ToString ().Split (new char[] { '.' }).Last () + ":-";
//			if (!LayoutIsValid)
//				tmp += "L-";
//			//if (Interface.graphicUpdateList.Contains(this))
//			//    tmp += "GU-";
//			if (OpenTKGameWindow.redrawClip.intersect (this.renderBoundsInBackendSurfaceCoordonate))
//				tmp += "D-";
//			return tmp + string.Format ("rb:{0}", Slot);
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
				GraphicObject.EventsToResolve.Add(new EventSource 
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
