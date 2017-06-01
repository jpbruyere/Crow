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
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Crow.Native;

namespace Crow
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void LayoutChangedCallBack (LayoutingType lt);

	public class GraphicObject : IValueChange, IDisposable
	{
		internal static ulong currentUid = 0;
		internal ulong uid = 0;


		unsafe internal crow_object_t* nativeHnd;

		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region CTOR
		public GraphicObject ()
		{
			initLibcrow ();
			#if DEBUG
			uid = currentUid;
			currentUid++;
			#endif
		}
		protected virtual void initLibcrow (){
			unsafe {
				nativeHnd =  LibCrow.crow_object_create ();
				LibCrow.crow_object_set_type (nativeHnd, CrowType.Simple);
				nativeHnd->Context = Interface.CurrentInterface.layoutingCtx;
				nativeHnd->IsDirty = 1;
				//nativeHnd->OnLayoutChanged = Marshal.GetFunctionPointerForDelegate((LayoutChangedCallBack)OnLayoutChanges);
			}
		}
		#endregion

		/// <summary>
		/// Initialize this Graphic object instance by setting style and default values and loading template if required
		/// </summary>
		public virtual void Initialize(){
			if (currentInterface == null)
				currentInterface = Interface.CurrentInterface;

			loadDefaultValues ();
		}
		#region private fields

		Interface currentInterface = null;
		GraphicObject logicalParent;
		GraphicObject parent;
		string name;
		Fill background = Color.Transparent;
		Fill foreground = Color.White;
		Font font = "droid, 10";
		double cornerRadius = 0;
		bool focusable = false;
		bool hasFocus = false;
		bool isActive = false;
		bool mouseRepeat;
		bool isEnabled = true;
		bool clipToClientRect = true;
		protected object dataSource;
		string style;
		object tag;
		#endregion

		#region public fields
		/// <summary>
		/// The clipping rectangles list
		/// </summary>
		public Region Clipping = new Region();
		/// <summary>Prevent requeuing multiple times the same widget</summary>
		public bool IsQueueForRedraw = false;
		/// <summary>drawing Cache bitmap</summary>
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
		[XmlIgnore]unsafe public LayoutingType RegisteredLayoutings {
			get { return nativeHnd->RegisteredLayoutings; } set { nativeHnd->RegisteredLayoutings = value; } }
		//TODO: it would save the recurent cost of a cast in event bubbling if parent type was GraphicObject
		//		or we could add to the interface the mouse events
		/// <summary>
		/// Parent in the graphic tree, used for rendering and layouting
		/// </summary>
		[XmlIgnore]unsafe public virtual GraphicObject Parent {
			get { return parent; }
			set {
				if (parent == value)
					return;
				DataSourceChangeEventArgs e = new DataSourceChangeEventArgs (parent, value);
				lock (this) {
					parent = value;
//					if (parent == null)
//						nativeHnd->Parent = null;
//					else
//						nativeHnd->Parent = value.nativeHnd;
				}

				onParentChanged (this, e);
			}
		}
		[XmlIgnore]public GraphicObject LogicalParent {
			get { return logicalParent == null ? Parent : logicalParent; }
			set {
				if (logicalParent == value)
					return;
				if (logicalParent != null)
					logicalParent.DataSourceChanged -= onLogicalParentDataSourceChanged;
				DataSourceChangeEventArgs dsce = new DataSourceChangeEventArgs (LogicalParent, null);
				logicalParent = value;
				dsce.NewDataSource = LogicalParent;
				if (logicalParent != null)
					logicalParent.DataSourceChanged += onLogicalParentDataSourceChanged;
				onLogicalParentChanged (this, dsce);
			}
		}
		[XmlIgnore]unsafe public virtual Rectangle ClientRectangle {
			get {
				Rectangle cb = nativeHnd->Slot.Size;
				cb.Inflate ( - Margin);
				return cb;
			}
		}
		public virtual Rectangle ContextCoordinates(Rectangle r){
			if (Parent is Interface)
				return r + Parent.ClientRectangle.Position;
			return parent.CacheEnabled ?
				r + Parent.ClientRectangle.Position :
				Parent.ContextCoordinates (r);
		}
		public virtual Rectangle ScreenCoordinates (Rectangle r){
			return
				Parent.ScreenCoordinates(r) + Parent.getSlot().Position + Parent.ClientRectangle.Position;
		}
		unsafe public virtual Rectangle getSlot () { return nativeHnd->Slot;}
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
		public event EventHandler<DataSourceChangeEventArgs> DataSourceChanged;
		public event EventHandler<DataSourceChangeEventArgs> ParentChanged;
		public event EventHandler<DataSourceChangeEventArgs> LogicalParentChanged;
		#endregion

		#region public properties
		[XmlIgnore]public Interface CurrentInterface {
			get {
				if (currentInterface == null) {
					currentInterface = Interface.CurrentInterface;
					Initialize ();
				}
				return currentInterface;
			}
			set {
				currentInterface = value;
			}
		}
		/// <summary>Random value placeholder</summary>
		[XmlAttributeAttribute]
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
		/// If enabled, resulting bitmap of graphic object is cached in an byte array
		/// speeding up rendering of complex object. Default is enabled.
		/// </summary>
		[XmlAttributeAttribute][DefaultValue(true)]
		unsafe public virtual bool CacheEnabled {
			get { return nativeHnd->CacheEnabled>0; }
			set {
				if (CacheEnabled == value)
					return;
				if (value)
					nativeHnd->CacheEnabled = 1;
				else
					nativeHnd->CacheEnabled = 0;
				NotifyValueChanged ("CacheEnabled", CacheEnabled);
			}
		}
		/// <summary>
		/// If true, rendering of GraphicObject is clipped inside client rectangle
		/// </summary>
		[XmlAttributeAttribute][DefaultValue(true)]
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
		/// <summary>
		/// Name is used in binding to reference other GraphicObjects inside the graphic tree
		/// </summary>
		[XmlAttributeAttribute][DefaultValue(null)]
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
		unsafe public virtual VerticalAlignment VerticalAlignment {
			get { return nativeHnd->VerticalAlignment; }
			set {
				if (nativeHnd->VerticalAlignment == value)
					return;

				nativeHnd->VerticalAlignment = value;
				NotifyValueChanged("VerticalAlignment", nativeHnd->VerticalAlignment);
				RegisterForLayouting (LayoutingType.Y);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(HorizontalAlignment.Center)]
		unsafe public virtual HorizontalAlignment HorizontalAlignment {
			get { return nativeHnd->HorizontalAlignment; }
			set {
				if (nativeHnd->HorizontalAlignment == value)
					return;
				nativeHnd->HorizontalAlignment = value;
				NotifyValueChanged("HorizontalAlignment", nativeHnd->HorizontalAlignment);
				RegisterForLayouting (LayoutingType.X);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Left {
			get { unsafe { return nativeHnd->Left; } }
			set {
				if (Left == value)
					return;
				unsafe {
					nativeHnd->Left = value;
				}
				NotifyValueChanged ("Left", Left);
				this.RegisterForLayouting (LayoutingType.X);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public virtual int Top {
			get { unsafe { return nativeHnd->Top; } }
			set {
				if (Top == value)
					return;
				unsafe {
					nativeHnd->Top = value;
				}
				NotifyValueChanged ("Top", Top);
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
		unsafe public virtual Measure Width {
			get {
				return nativeHnd->Width.Units == Unit.Inherit ? Parent == null ?
					Measure.Stretched :	Parent.WidthPolicy : nativeHnd->Width;
			}
			set {
				if (nativeHnd->Width == value)
					return;
				if (value.IsFixed) {
					if (value < MinimumSize.Width || (value > MaximumSize.Width && MaximumSize.Width > 0))
						return;
				}
				Measure lastWP = WidthPolicy;
				nativeHnd->Width = value;
				NotifyValueChanged ("Width", nativeHnd->Width);
				if (WidthPolicy != lastWP) {
					NotifyValueChanged ("WidthPolicy", WidthPolicy);
					//contentSize in Stacks are only update on childLayoutChange, and the single stretched
					//child of the stack is not counted in contentSize, so when changing size policy of a child
					//we should adapt contentSize
					//TODO:check case when child become stretched, and another stretched item already exists.
					if (parent is GenericStack) {//TODO:check if I should test Group instead
						if ((parent as GenericStack).Orientation == Orientation.Horizontal) {
							if (lastWP == Measure.Fit)
								(parent as GenericStack).contentSize.Width -= nativeHnd->LastSlot.Width;
							else
								(parent as GenericStack).contentSize.Width += nativeHnd->LastSlot.Width;
						}
					}
				}

				this.RegisterForLayouting (LayoutingType.Width);
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Inherit")]
		unsafe public virtual Measure Height {
			get {
				return nativeHnd->Height.Units == Unit.Inherit ? Parent == null ?
					Measure.Stretched :	Parent.HeightPolicy : nativeHnd->Height;
			}
			set {
				if (nativeHnd->Height == value)
					return;
				if (value.IsFixed) {
					if (value < MinimumSize.Height || (value > MaximumSize.Height && MaximumSize.Height > 0))
						return;
				}
				Measure lastHP = HeightPolicy;
				nativeHnd->Height = value;
				NotifyValueChanged ("Height", nativeHnd->Height);
				if (HeightPolicy != lastHP) {
					NotifyValueChanged ("HeightPolicy", HeightPolicy);
					if (parent is GenericStack) {
						if ((parent as GenericStack).Orientation == Orientation.Vertical) {
							if (lastHP == Measure.Fit)
								(parent as GenericStack).contentSize.Height -= nativeHnd->LastSlot.Height;
							else
								(parent as GenericStack).contentSize.Height += nativeHnd->LastSlot.Height;
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
				if (hasFocus)
					onFocused (this, null);
				else
					onUnfocused (this, null);
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
		bool clearBackground = false;
		[XmlAttributeAttribute()][DefaultValue("Transparent")]
		unsafe public virtual Fill Background {
			get { return background; }
			set {
				if (background == value)
					return;
				clearBackground = false;
				if (nativeHnd->Background != IntPtr.Zero) {
					Cairo.NativeMethods.cairo_pattern_destroy (nativeHnd->Background);
					nativeHnd->Background = IntPtr.Zero;
				}
				if (value == null)
					return;
				background = value;
				SolidColor sc = value as SolidColor;
				if (sc != null) {
					nativeHnd->Background = Cairo.NativeMethods.cairo_pattern_create_rgba (
						sc.color.R, sc.color.G, sc.color.B, sc.color.A);
				}
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
			get { unsafe { return nativeHnd->Margin; } }
			set {
				if (value == Margin)
					return;
				unsafe {
					nativeHnd->Margin = value;
				}
				NotifyValueChanged ("Margin", Margin);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute][DefaultValue(true)]
		public virtual bool Visible {
			get { unsafe { return nativeHnd->Visible>0;} }
			set {
				if (value == Visible)
					return;
				unsafe {
					if (value)
						nativeHnd->Visible = 1;
					else
						nativeHnd->Visible = 0;
				}

				RegisterForLayouting (LayoutingType.Sizing);

				//trigger a mouse to handle possible hover changes
				//CurrentInterface.ProcessMouseMove (CurrentInterface.Mouse.X, CurrentInterface.Mouse.Y);

				NotifyValueChanged ("Visible", Visible);
			}
		}
		[XmlIgnore]unsafe public bool IsDirty {
			get { return nativeHnd->IsDirty>0; }
			set {
				if (value)
					nativeHnd->IsDirty = 1;
				else
					nativeHnd->IsDirty = 0;
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
		[XmlAttributeAttribute][DefaultValue("1,1")]
		public virtual Size MinimumSize {
			get { unsafe { return nativeHnd->MinimumSize;} }
			set {
				if (value == MinimumSize)
					return;

				unsafe {
					nativeHnd->MinimumSize = value;
				}

				NotifyValueChanged ("MinimumSize", MinimumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		[XmlAttributeAttribute][DefaultValue("0,0")]
		public virtual Size MaximumSize {
			get { unsafe { return nativeHnd->MaximumSize;} }
			set {
				if (value == MaximumSize)
					return;
				unsafe {
					nativeHnd->MaximumSize = value;
				}

				NotifyValueChanged ("MaximumSize", MaximumSize);
				RegisterForLayouting (LayoutingType.Sizing);
			}
		}
		/// <summary>
		/// Seek first logical tree upward if logicalParent is set, or seek graphic tree for
		/// a not null dataSource that will be active for all descendants having dataSource=null
		/// </summary>
		[XmlAttributeAttribute][DefaultValue(null)]
		public virtual object DataSource {
			set {
				if (DataSource == value)
					return;

				DataSourceChangeEventArgs dse = new DataSourceChangeEventArgs (DataSource, null);
				dataSource = value;
				dse.NewDataSource = DataSource;

				OnDataSourceChanged (this, dse);

				NotifyValueChanged ("DataSource", DataSource);
			}
			get {
				return dataSource == null ?
					LogicalParent == null ? null :
					LogicalParent.DataSource : dataSource;
			}
		}
		protected virtual void onLogicalParentDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			if (localDataSourceIsNull)
				OnDataSourceChanged (this, e);
		}
		internal bool localDataSourceIsNull { get { return dataSource == null; } }
		internal bool localLogicalParentIsNull { get { return logicalParent == null; } }

		public virtual void OnDataSourceChanged(object sender, DataSourceChangeEventArgs e){
			DataSourceChanged.Raise (this, e);
			#if DEBUG_BINDING
			Debug.WriteLine("New DataSource for => {0} \n\t{1}=>{2}", this.ToString(),e.OldDataSource,e.NewDataSource);
			#endif
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
		internal void loadDefaultValues()
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

			//Search for a style matching :
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
				typeof(void),new Type[] {CompilerServices.TObject},thisType,true);

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
						il.Emit (OpCodes.Callvirt, CompilerServices.miCompileDynEventHandler);
						il.Emit (OpCodes.Castclass, ei.EventHandlerType);
						il.Emit (OpCodes.Callvirt, ei.AddMethod);
					}else
						Debug.WriteLine("error in styling, event not handled : " + trimed);
				}
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

			//try {
				Interface.DefaultValuesLoader[styleKey] = (Interface.LoaderInvoker)dm.CreateDelegate(typeof(Interface.LoaderInvoker));
				Interface.DefaultValuesLoader[styleKey] (this);
//			} catch (Exception ex) {
//				throw new Exception ("Error applying style <" + styleKey + ">:", ex);
//			}
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
		/// <summary>
		/// Register old and new slot for clipping
		/// </summary>
		unsafe public virtual void ClippingRegistration(){
			IsQueueForRedraw = false;
			if (Parent == null)
				return;
			Parent.RegisterClip (nativeHnd->LastPaintedSlot);
			Parent.RegisterClip (nativeHnd->Slot);
		}
		/// <summary>
		/// Add clip rectangle to this.clipping and propagate up to root
		/// </summary>
		/// <param name="clip">Clip rectangle</param>
		unsafe public virtual void RegisterClip(Rectangle clip){
			Rectangle  r = clip + ClientRectangle.Position;
			if (CacheEnabled && !IsDirty)
				Clipping.UnionRectangle (r);
			if (Parent == null)
				return;
			GraphicObject p = Parent as GraphicObject;
			if (p?.IsDirty == true && p?.CacheEnabled == true)
				return;
			Parent.RegisterClip (r + nativeHnd->Slot.Position);
		}
		/// <summary> Full update, taking care of sizing policy </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForGraphicUpdate ()
		{
			IsDirty = true;
			if (Width.IsFit || Height.IsFit)
				RegisterForLayouting (LayoutingType.Sizing);
			else if (RegisteredLayoutings == LayoutingType.None)
				CurrentInterface.EnqueueForRepaint (this);
		}
		/// <summary> query an update of the content, a redraw </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RegisterForRedraw ()
		{
			IsDirty = true;
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

		unsafe public virtual void RegisterForLayouting(LayoutingType layoutType){
			LibCrow.crow_object_register_layouting (this.nativeHnd, layoutType);
		}

		/// <summary> trigger dependant sizing component update </summary>
		public virtual void OnLayoutChanges(LayoutingType  layoutType)
		{
			#if DEBUG_LAYOUTING
//			CurrentInterface.currentLQI.Slot = LastSlots;
//			CurrentInterface.currentLQI.NewSlot = Slot;
			unsafe{
			Debug.WriteLine ("\t\t{0} => {1}", nativeHnd->LastSlot, nativeHnd->Slot);
			}
			#endif

			switch (layoutType) {
			case LayoutingType.Width:
				RegisterForLayouting (LayoutingType.X);
				break;
			case LayoutingType.Height:
				RegisterForLayouting (LayoutingType.Y);
				break;
			case LayoutingType.X:
				Console.WriteLine (Name);
				break;
			}


			//LayoutChanged.Raise (this, new LayoutingEventArgs (layoutType));
		}
		internal protected void raiseLayoutChanged(LayoutingEventArgs e){
			LayoutChanged.Raise (this, e);
		}
		/// <summary> Update layout component only one at a time, this is where the computation of alignement
		/// and size take place.
		/// The redrawing will only be triggered if final slot size has changed </summary>
		/// <returns><c>true</c>, if layouting was possible, <c>false</c> if conditions were not
		/// met and LQI has to be re-queued</returns>
		unsafe public virtual bool UpdateLayout (LayoutingType layoutType)
		{
			if (LibCrow.crow_object_do_layout (nativeHnd, layoutType)==0)
				return false;
			
			//unset bit, it would be reset if LQI is re-queued
			//RegisteredLayoutings &= (~layoutType);

			switch (layoutType) {
//			case LayoutingType.X:				
//				if (nativeHnd->LastSlot.X == nativeHnd->Slot.X)
//					break;
//
//				IsDirty = true;
//
//				OnLayoutChanges (layoutType);
//
//				nativeHnd->LastSlot.X = nativeHnd->Slot.X;
//				break;
			case LayoutingType.Y:
				if (Top == 0) {

					if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Height) ||
					    RegisteredLayoutings.HasFlag (LayoutingType.Height))
						return false;

					switch (VerticalAlignment) {
					case VerticalAlignment.Top://this could be processed even if parent Height is not known
						nativeHnd->Slot.Y = 0;
						break;
					case VerticalAlignment.Bottom:
						nativeHnd->Slot.Y = Parent.ClientRectangle.Height - nativeHnd->Slot.Height;
						break;
					case VerticalAlignment.Center:
						nativeHnd->Slot.Y = Parent.ClientRectangle.Height / 2 - nativeHnd->Slot.Height / 2;
						break;
					}
				} else
					nativeHnd->Slot.Y = Top;

				if (nativeHnd->LastSlot.Y == nativeHnd->Slot.Y)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				nativeHnd->LastSlot.Y = nativeHnd->Slot.Y;
				break;
			case LayoutingType.Width:
				if (Visible) {
					if (Width.IsFixed)
						nativeHnd->Slot.Width = Width;
					else if (Width == Measure.Fit) {
						nativeHnd->Slot.Width = measureRawSize (LayoutingType.Width);
					} else if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Width))
						return false;
					else if (Width == Measure.Stretched)
						nativeHnd->Slot.Width = Parent.ClientRectangle.Width;
					else
						nativeHnd->Slot.Width = (int)Math.Round ((double)(Parent.ClientRectangle.Width * Width) / 100.0);

					if (nativeHnd->Slot.Width < 0)
						return false;

					//size constrain
					if (nativeHnd->Slot.Width < MinimumSize.Width) {
						nativeHnd->Slot.Width = MinimumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					} else if (nativeHnd->Slot.Width > MaximumSize.Width && MaximumSize.Width > 0) {
						nativeHnd->Slot.Width = MaximumSize.Width;
						//NotifyValueChanged ("WidthPolicy", Measure.Stretched);
					}
				} else
					nativeHnd->Slot.Width = 0;

				if (nativeHnd->LastSlot.Width == nativeHnd->Slot.Width)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				nativeHnd->LastSlot.Width = nativeHnd->Slot.Width;
				break;
			case LayoutingType.Height:
				if (Visible) {
					if (Height.IsFixed)
						nativeHnd->Slot.Height = Height;
					else if (Height == Measure.Fit) {
						nativeHnd->Slot.Height = measureRawSize (LayoutingType.Height);
					} else if (Parent.RegisteredLayoutings.HasFlag (LayoutingType.Height))
						return false;
					else if (Height == Measure.Stretched)
						nativeHnd->Slot.Height = Parent.ClientRectangle.Height;
					else
						nativeHnd->Slot.Height = (int)Math.Round ((double)(Parent.ClientRectangle.Height * Height) / 100.0);

					if (nativeHnd->Slot.Height < 0)
						return false;

					//size constrain
					if (nativeHnd->Slot.Height < MinimumSize.Height) {
						nativeHnd->Slot.Height = MinimumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					} else if (nativeHnd->Slot.Height > MaximumSize.Height && MaximumSize.Height > 0) {
						nativeHnd->Slot.Height = MaximumSize.Height;
						//NotifyValueChanged ("HeightPolicy", Measure.Stretched);
					}
				} else
					nativeHnd->Slot.Height = 0;

				if (nativeHnd->LastSlot.Height == nativeHnd->Slot.Height)
					break;

				IsDirty = true;

				OnLayoutChanges (layoutType);

				nativeHnd->LastSlot.Height = nativeHnd->Slot.Height;
				break;
			}

			//if no layouting remains in queue for item, registre for redraw
			if (RegisteredLayoutings == LayoutingType.None && IsDirty)
				CurrentInterface.EnqueueForRepaint (this);

			return true;
		}
		#endregion

		#region Rendering
		/// <summary> This is the common overridable drawing routine to create new widget </summary>
		unsafe protected virtual void onDraw(Context gr)
		{
			Rectangle rBack = new Rectangle (nativeHnd->Slot.Size);

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle (gr, rBack, cornerRadius);
			gr.Fill ();
		}

		/// <summary>
		/// Internal drawing context creation on a cached surface limited to slot size
		/// this trigger the effective drawing routine </summary>
		unsafe protected virtual void RecreateCache ()
		{
			int stride = 4 * nativeHnd->Slot.Width;

			int bmpSize = Math.Abs (stride) * nativeHnd->Slot.Height;
			bmp = new byte[bmpSize];
			IsDirty = false;
			using (ImageSurface draw =
				new ImageSurface(bmp, Format.Argb32, nativeHnd->Slot.Width, nativeHnd->Slot.Height, stride)) {
				using (Context gr = new Context (draw)) {
					gr.Antialias = Interface.Antialias;
					onDraw (gr);
				}
				draw.Flush ();
			}
		}
		unsafe protected virtual void UpdateCache(Context ctx){
			Rectangle rb = nativeHnd->Slot + Parent.ClientRectangle.Position;
			using (ImageSurface cache = new ImageSurface (bmp, Format.Argb32, nativeHnd->Slot.Width, nativeHnd->Slot.Height, 4 * nativeHnd->Slot.Width)) {
				if (clearBackground) {
						ctx.Save ();
						ctx.Operator = Operator.Clear;
						ctx.Rectangle (rb);
						ctx.Fill ();
						ctx.Restore ();
				}
				ctx.SetSourceSurface (cache, rb.X, rb.Y);
				ctx.Paint ();
			}
			Clipping.Dispose ();
			Clipping = new Region ();
		}
		/// <summary> Chained painting routine on the parent context of the actual cached version
		/// of the widget </summary>
		unsafe public virtual void Paint (ref Context ctx)
		{
			//TODO:this test should not be necessary
			if (nativeHnd->Slot.Height < 0 || nativeHnd->Slot.Width < 0 || parent == null)
				return;
			lock (this) {
				if (CacheEnabled) {
					if (nativeHnd->Slot.Width > Interface.MaxCacheSize || nativeHnd->Slot.Height > Interface.MaxCacheSize)
						CacheEnabled = false;
				}

				if (CacheEnabled) {
					if (IsDirty)
						RecreateCache ();

					UpdateCache (ctx);
					if (!isEnabled)
						paintDisabled (ctx, nativeHnd->Slot + Parent.ClientRectangle.Position);
				} else {
					Rectangle rb = nativeHnd->Slot + Parent.ClientRectangle.Position;
					ctx.Save ();

					ctx.Translate (rb.X, rb.Y);

					onDraw (ctx);
					if (!isEnabled)
						paintDisabled (ctx, nativeHnd->Slot);

					ctx.Restore ();
				}
				nativeHnd->LastPaintedSlot = nativeHnd->Slot;
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
		unsafe public virtual bool MouseIsIn(Point m)
		{
			try {
				if (!(Visible & isEnabled))
					return false;
				if (ScreenCoordinates (nativeHnd->Slot).ContainsOrIsEqual (m)) {
					Scroller scr = Parent as Scroller;
					if (scr == null)
						return Parent.MouseIsIn (m);
					return scr.MouseIsIn (scr.savedMousePos);
				}
			} catch (Exception ex) {
				return false;
			}
			return false;
		}
		public virtual void checkHoverWidget(MouseMoveEventArgs e)
		{
			if (CurrentInterface.HoverWidget != this) {
				CurrentInterface.HoverWidget = this;
				onMouseEnter (this, e);
			}

			this.onMouseMove (this, e);//without this, window border doesn't work, should be removed
		}
		public virtual void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			//bubble event to the top
			if (Parent != null)
				Parent.onMouseMove(sender,e);

			MouseMove.Raise (this, e);
		}
		public virtual void onMouseDown(object sender, MouseButtonEventArgs e){
			if (CurrentInterface.eligibleForDoubleClick == this && CurrentInterface.clickTimer.ElapsedMilliseconds < Interface.DoubleClick)
				onMouseDoubleClick (this, e);
			else
				currentInterface.clickTimer.Restart();
			CurrentInterface.eligibleForDoubleClick = null;

			if (CurrentInterface.ActiveWidget == null)
				CurrentInterface.ActiveWidget = this;
			if (this.Focusable && !Interface.FocusOnHover) {
				BubblingMouseButtonEventArg be = e as BubblingMouseButtonEventArg;
				if (be.Focused == null) {
					be.Focused = this;
					CurrentInterface.FocusedWidget = this;
				}
			}
			//bubble event to the top
			if (Parent != null)
				Parent.onMouseDown(sender,e);

			MouseDown.Raise (this, e);
		}
		public virtual void onMouseUp(object sender, MouseButtonEventArgs e){
			//bubble event to the top
			if (Parent != null)
				Parent.onMouseUp(sender,e);

			MouseUp.Raise (this, e);

			if (MouseIsIn (e.Position) && IsActive) {
				if (CurrentInterface.clickTimer.ElapsedMilliseconds < Interface.DoubleClick)
					CurrentInterface.eligibleForDoubleClick = this;
				onMouseClick (this, e);
			}
		}
		public virtual void onMouseClick(object sender, MouseButtonEventArgs e){			
			if (Parent != null)
				Parent.onMouseClick(sender,e);
			MouseClick.Raise (this, e);
		}
		public virtual void onMouseDoubleClick(object sender, MouseButtonEventArgs e){			
			if (Parent != null)
				Parent.onMouseDoubleClick(sender,e);
			MouseDoubleClick.Raise (this, e);
		}
		public virtual void onMouseWheel(object sender, MouseWheelEventArgs e){			
			if (Parent != null)
				Parent.onMouseWheel(sender,e);

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

		protected virtual void onFocused(object sender, EventArgs e){
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
			ParentChanged.Raise (this, e);
			if (logicalParent == null)
				LogicalParentChanged.Raise (this, e);
		}
		protected virtual void onLogicalParentChanged(object sender, DataSourceChangeEventArgs e) {
			LogicalParentChanged.Raise (this, e);
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
		#region IDisposable implementation
		~GraphicObject(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		bool disposed = false;
		protected virtual void Dispose (bool disposing){
			if (disposed)
				return;
			Clipping.Dispose ();
			unsafe{
				LibCrow.crow_object_destroy (nativeHnd);
			}
			disposed = true;
		}
		#endregion
	}
}
