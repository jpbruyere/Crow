// Copyright (c) 2013-2020  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Crow.Cairo;
using Crow.IML;
using Glfw;

namespace Crow
{
	/// <summary>
	/// The Interface Class is the root of crow graphic trees. It is thread safe allowing
	/// application to run multiple interfaces in different threads.
	/// It provides the Dirty bitmap and zone of the interface to be drawn on screen.
	/// </summary>
	/// <remarks>
	/// The Interface contains :
	/// 	- rendering and layouting queues and logic.
	/// 	- helpers to load XML interfaces files directely bound to this interface
	/// 	- global static constants and variables of CROW
	/// 	- Keyboard and Mouse logic
	/// 	- the resulting bitmap of the interface
	/// 
	/// the master branch and the nuget package includes an OpenTK renderer which allows
	/// the creation of multiple threaded interfaces.
	/// 
	/// If you intend to create another renderer (GDK, vulkan, etc) the minimal step is to
	/// put an interface instance as member of your root object and call (optionally in another thread) the update function
	/// at regular interval. Then you have to call
	/// mouse, keyboard and resize functions of the interface when those events occurs in the host app.
	/// 
	/// The resulting surface (a byte array in the OpenTK renderer) is made available and protected with the
	/// RenderMutex of the interface.
	/// </remarks>
	public class Interface : ILayoutable, IDisposable, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		#region CTOR
		static Interface ()
		{
			/*if (Type.GetType ("Mono.Runtime") == null) {
				throw new Exception (@"C.R.O.W. run only on Mono, download latest version at: http://www.mono-project.com/download/stable/");
			}*/

			CROW_CONFIG_ROOT =
				System.IO.Path.Combine (
					Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					".config");
			CROW_CONFIG_ROOT = System.IO.Path.Combine (CROW_CONFIG_ROOT, "crow");
			if (!Directory.Exists (CROW_CONFIG_ROOT))
				Directory.CreateDirectory (CROW_CONFIG_ROOT);

			//ensure all assemblies are loaded, because IML could contains classes not instanciated in source
			foreach (string af in Directory.GetFiles (AppDomain.CurrentDomain.BaseDirectory, "*.dll")) {
				try {
					Assembly.LoadFrom (af);
				} catch {
					System.Diagnostics.Debug.WriteLine ("{0} not loaded as assembly.", af);
				}
			}

			FontRenderingOptions = new FontOptions ();
			FontRenderingOptions.Antialias = Antialias.Subpixel;
			FontRenderingOptions.HintMetrics = HintMetrics.On;
			FontRenderingOptions.HintStyle = HintStyle.Full;
			FontRenderingOptions.SubpixelOrder = SubpixelOrder.Default;

			loadCursors ();
		}
		public Interface (int width, int height, IntPtr glfwWindowHandle) : this (width, height, false, false)
		{
			hWin = glfwWindowHandle;
		}
		public Interface (int width = 800, int height = 600, bool startUIThread = true, bool createSurface = true)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CurrentInterface = this;
			clientRectangle = new Rectangle (0, 0, width, height);

			if (createSurface)
				initSurface ();

			if (startUIThread) {
				Thread t = new Thread (InterfaceThread) {
					IsBackground = true
				};
				t.Start ();
			}

#if MEASURE_TIME
			PerfMeasures.Add (updateMeasure);
			PerfMeasures.Add (drawingMeasure);
			PerfMeasures.Add (layoutingMeasure);
			PerfMeasures.Add (clippingMeasure);
#endif
		}
		#endregion

		/** GLFW callback may return a custom pointer, this list makes the link between the GLFW window pointer and the
			manage VkWindow instance. */
		static Dictionary<IntPtr, Interface> windows = new Dictionary<IntPtr, Interface> ();
		/** GLFW window native pointer and current native handle for mouse cursor */
		IntPtr hWin;
		Cursor currentCursor;
		bool ownWindow;

		void initSurface ()
		{
			Glfw3.Init ();

			Glfw3.WindowHint (WindowAttribute.ClientApi, 0);
			Glfw3.WindowHint (WindowAttribute.Resizable, 1);
			Glfw3.WindowHint (WindowAttribute.Decorated, 1);

			hWin = Glfw3.CreateWindow (clientRectangle.Width, clientRectangle.Height, "win name", MonitorHandle.Zero, IntPtr.Zero);
			if (hWin == IntPtr.Zero)
				throw new Exception ("[GLFW3] Unable to create vulkan Window");
			ownWindow = true;

			Glfw3.SetKeyCallback (hWin, HandleKeyDelegate);
			Glfw3.SetMouseButtonPosCallback (hWin, HandleMouseButtonDelegate);
			Glfw3.SetCursorPosCallback (hWin, HandleCursorPosDelegate);
			Glfw3.SetScrollCallback (hWin, HandleScrollDelegate);
			Glfw3.SetCharCallback (hWin, HandleCharDelegate);
			Glfw3.SetWindowSizeCallback (hWin, HandleWindowSizeDelegate);

			//Glfw3.SetWindowTitle (hWin, "FPS: " + fps.ToString ());
			switch (Environment.OSVersion.Platform) {
			case PlatformID.MacOSX:
				break;
			case PlatformID.Unix:
				IntPtr disp = Glfw3.GetX11Display ();
				IntPtr nativeWin = Glfw3.GetX11Window (hWin);
				Int32 scr = Glfw3.GetX11DefaultScreen (disp);
				IntPtr visual = Glfw3.GetX11DefaultVisual (disp, scr);
				surf = new XlibSurface (disp, nativeWin, visual, clientRectangle.Width, clientRectangle.Height);
				break;
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
				IntPtr hWin32 = Glfw3.GetWin32Window (hWin);
				IntPtr hdc = Glfw3.GetWin32DC (hWin32);
				surf = new Win32Surface (hdc);
				break;
			case PlatformID.Xbox:
			case PlatformID.WinCE:
				throw new PlatformNotSupportedException ("Unable to create cairo surface.");
			}

			windows.Add (hWin, this);
		}

		#region events delegates

		static CursorPosDelegate HandleCursorPosDelegate = (window, xPosition, yPosition) => {
			windows [window].OnMouseMove ((int)xPosition, (int)yPosition);
		};
		static MouseButtonDelegate HandleMouseButtonDelegate = (IntPtr window, MouseButton button, InputAction action, Modifier mods) => {
			if (action == InputAction.Release)
				windows [window].OnMouseButtonUp (button);
			else//press and repeat
				windows [window].OnMouseButtonDown (button);

		};
		static ScrollDelegate HandleScrollDelegate = (IntPtr window, double xOffset, double yOffset) => {
			windows [window].OnMouseWheelChanged ((int)yOffset);
		};
		static KeyDelegate HandleKeyDelegate = (IntPtr window, Key key, int scanCode, InputAction action, Modifier modifiers) => {
			if (action == InputAction.Release)
				windows [window].OnKeyUp (key);
			 else
				windows [window].OnKeyDown (key);
		};
		static CharDelegate HandleCharDelegate = (IntPtr window, CodePoint codepoint) => {
			windows [window].OnKeyPress (codepoint.ToChar());
		};
		static WindowSizeDelegate HandleWindowSizeDelegate = (IntPtr window, int Width, int Height) => {
			windows [window].ProcessResize (new Rectangle (0, 0, Width, Height));
		};

		#endregion


		public bool Running {
			get => !Glfw3.WindowShouldClose (hWin);
			set => Glfw3.SetWindowShouldClose (hWin, value == true ? 0 : 1);
		}
		public virtual void InterfaceThread ()
		{

			while (!Glfw3.WindowShouldClose (hWin)) {
				Update ();
				Thread.Sleep (UPDATE_INTERVAL);
#if MEASURE_TIME
				foreach (PerformanceMeasure m in PerfMeasures) 
					m.NotifyChanges ();
#endif
			}
		}
		protected virtual void OnInitialized ()
		{
			try {
				Load ("#main.crow").DataSource = this;
			} catch { }
			Initialized.Raise (this, null);
		}
		/// <summary>
		/// load styling, init default tooltips and context menus, load main.crow resource if exists.
		/// </summary>
		public void Init () {
			loadStyling ();
			initTooltip ();
			initContextMenus ();
			OnInitialized ();
		}
		/// <summary>
		/// call Init() then enter the running loop performing ProcessEvents until running==false.
		/// </summary>
		public virtual void Run () {
			Init ();

			while (!Glfw3.WindowShouldClose (hWin)) {
				Glfw3.PollEvents ();
				Thread.Sleep(1);
			}
		}

		public virtual void Quit () => Glfw3.SetWindowShouldClose (hWin, 1);

		public bool Shift => Glfw3.GetKey(hWin, Glfw.Key.LeftShift) == InputAction.Press;
		public bool Ctrl => Glfw3.GetKey (hWin, Glfw.Key.LeftControl) == InputAction.Press;
		public bool Alt => Glfw3.GetKey (hWin, Glfw.Key.LeftAlt) == InputAction.Press;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				currentCursor?.Dispose ();
				if (ownWindow) {
					Glfw3.DestroyWindow (hWin);
					Glfw3.Terminate ();
				}

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~Interface() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		/*public void ProcessEvents ()
		{
			//if (armedClick != null) {
			//	if (lastClickTime.ElapsedMilliseconds > DOUBLECLICK_TRESHOLD) {
			//		//cancel double click and 
			//		armedClick.onMouseClick (armedClick, armedClickEventArgs);
			//		armedClick = null;
			//	}
			//}
		}*/

		#region Static and constants
		/// <summary>Time interval in milisecond between Updates of the interface</summary>
		public static int UPDATE_INTERVAL = 5;
		/// <summary>Crow configuration root path</summary>
		public static string CROW_CONFIG_ROOT;
		/// <summary>If true, mouse focus is given when mouse is over control</summary>
		public static bool FOCUS_ON_HOVER = true;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 10;
		/// <summary> delay before tooltip appears </summary>
		public static int TOOLTIP_DELAY = 500;
		/// <summary>Double click threshold in milisecond</summary>
		public static int DOUBLECLICK_TRESHOLD = 320;//max duration between two mouse_down evt for a dbl clk in milisec.
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DEVICE_REPEAT_DELAY = 700;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DEVICE_REPEAT_INTERVAL = 40;
		public static float WheelIncrement = 1;
		/// <summary>Tabulation size in Text controls</summary>
		public static int TAB_SIZE = 4;
		public static string LineBreak = "\n";
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		/// <summary> Disable caching for a widget if this threshold is reached </summary>
		public const int MaxCacheSize = 2048;
		/// <summary> Above this count, the layouting is discard from the current
		/// update cycle and requeued for the next</summary>
		public const int MaxLayoutingTries = 3;
		/// <summary> Above this count, the layouting is discard for the widget and it
		/// will not be rendered on screen </summary>
		public const int MaxDiscardCount = 5;
		/// <summary> Global font rendering settings for Cairo </summary>
		public static FontOptions FontRenderingOptions;
		/// <summary> Global font rendering settings for Cairo </summary>
		public static Antialias Antialias = Antialias.Subpixel;

		/// <summary>
		/// Each control need a ref to the root interface containing it, if not set in GraphicObject.currentInterface,
		/// the ref of this one will be stored in GraphicObject.currentInterface
		/// </summary>
		protected static Interface CurrentInterface;
		#endregion

		#region Events
		//public event EventHandler<MouseCursorChangedEventArgs> MouseCursorChanged;
		////public event EventHandler Quit;
		public event EventHandler Initialized;
		//public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		//public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		//public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		//public event EventHandler<MouseButtonEventArgs> MouseClick;
		//public event EventHandler<MouseMoveEventArgs> MouseMove;
		//public event EventHandler<KeyEventArgs> KeyDown;
		//public event EventHandler<KeyEventArgs> KeyUp;
		//public event EventHandler<KeyPressEventArgs> KeyPress;
		/*public event EventHandler<KeyEventArgs> KeyboardKeyDown;
		public event EventHandler<KeyEventArgs> KeyboardKeyUp;*/
		#endregion

		/// <summary>Main Cairo surface</summary>
		public Surface surf;

		#region Public Fields
		/// <summary>Graphic Tree of this interface</summary>
		public List<Widget> GraphicTree = new List<Widget>();
		/// <summary>Interface's resulting bitmap</summary>
		public byte[] bmp;
		/// <summary>resulting bitmap limited to last redrawn part</summary>
		public byte[] dirtyBmp;
		/// <summary>True when host has to repaint Interface</summary>
		public bool IsDirty;
		/// <summary>Coordinate of the dirty bmp on the original bmp</summary>
		public Rectangle DirtyRect;
		/// <summary>Locked for each layouting operation</summary>
		public object LayoutMutex = new object();
		/// <summary>Sync mutex between host and Crow for rendering operations (bmp, dirtyBmp,...)</summary>
		public object RenderMutex = new object();
		/// <summary>Global lock of the update cycle</summary>
		public object UpdateMutex = new object();
		/// <summary>Global lock of the clipping queue</summary>
		public object ClippingMutex = new object();
		//TODO:share resource instances
		/// <summary>
		/// Store loaded resources instances shared among controls to reduce memory footprint
		/// </summary>
		public Dictionary<string,object> Ressources = new Dictionary<string, object>();
		/// <summary>The Main layouting queue.</summary>
		public Queue<LayoutingQueueItem> LayoutingQueue = new Queue<LayoutingQueueItem> ();
		/// <summary>Store discarded lqi between two updates</summary>
		public Queue<LayoutingQueueItem> DiscardQueue;
		/// <summary>Main drawing queue, holding layouted controls</summary>
		public Queue<Widget> ClippingQueue = new Queue<Widget>();
		public string Clipboard;//TODO:use object instead for complex copy paste
		/// <summary>each IML and fragments (such as inline Templates) are compiled as a Dynamic Method stored here
		/// on the first instance creation of a IML item.
		/// </summary>
		public Dictionary<String, Instantiator> Instantiators = new Dictionary<string, Instantiator>();
		public Dictionary<String, Instantiator> Templates = new Dictionary<string, Instantiator> ();
		/// <summary>
		/// default templates dic by metadata token
		/// </summary>
		public Dictionary<int, Instantiator> DefaultTemplates = new Dictionary<int, Instantiator> ();
		/// <summary>
		/// Item templates stored with their index
		/// </summary>
		public Dictionary<String, ItemTemplate> ItemTemplates = new Dictionary<string, ItemTemplate> ();

		public List<CrowThread> CrowThreads = new List<CrowThread>();//used to monitor thread finished

		public DragDropEventArgs DragAndDropOperation = null;
		public Surface DragImage = null;
		public int DragImageWidth, DragImageHeight, DragImageX, DragImageY;
		public void ClearDragImage () {
			lock (UpdateMutex) {				
				clipping.UnionRectangle(new Rectangle (DragImageX, DragImageY, DragImageWidth, DragImageHeight));				
				DragImage.Dispose();
				DragImage = null;
			}			
		}
		#endregion

		#region Private Fields
		/// <summary>Client rectangle in the host context</summary>
		protected Rectangle clientRectangle;
		/// <summary>Clipping rectangles on the root context</summary>
		Region clipping = new Region();
		/// <summary>Main Cairo context</summary>
		//Context ctx;
		#endregion

		#region Default values and Style loading
		/// Default values of properties from Widgets are retrieve from XML Attributes.
		/// The reflexion process used to retrieve those values being very slow, it is compiled in MSIL
		/// and injected as a dynamic method referenced in the DefaultValuesLoader Dictionnary.
		/// The compilation is done on the first object instancing, and is also done for custom widgets
		public delegate void LoaderInvoker(object instance);
		/// <summary>Store one loader per StyleKey</summary>
		public Dictionary<String, LoaderInvoker> DefaultValuesLoader = new Dictionary<string, LoaderInvoker>();
		/// <summary>Store dictionnary of member/value per StyleKey</summary>
		public Dictionary<string, Style> Styling;
		/// <summary>
		/// Replacement value for style like cmake or bash variable.
		/// </summary>
		/// <remarks>
		/// each 'key=value' pair in style files not enclosed in brackets are threated as constant.
		/// If the same constant is defined more than once, only the first is kept.
		/// Than in any IML expresion, in style or xml, constant may be used as a replacement string with ${CONSTANTID}.
		/// If a constant is not resolved in iml while creating the instantiator, an error is thrown.
		/// </remarks>
		public readonly Dictionary<string, string> StylingConstants = new Dictionary<string, string> ();
		/// <summary> parse all styling data's during application startup and build global Styling Dictionary </summary>
		protected virtual void loadStyling() {
			Styling = new Dictionary<string, Style> ();

			//fetch styling info in this order, if member styling is alreadey referenced in previous
			//assembly, it's ignored.
			loadStylingFromAssembly (Assembly.GetEntryAssembly ());
			loadStylingFromAssembly (Assembly.GetExecutingAssembly ());
		}
		/// <summary> Search for .style resources in assembly </summary>
		protected void loadStylingFromAssembly (Assembly assembly) {
			if (assembly == null)
				return;
			foreach (string s in assembly
				.GetManifestResourceNames ()
				.Where (r => r.EndsWith (".style", StringComparison.OrdinalIgnoreCase))) {
				using (StyleReader sr = new StyleReader (assembly.GetManifestResourceStream (s))) 
					sr.Parse (this, s);				
			}
		}
		#endregion


		#region Load/Save
		/// <summary>get template stream from path providing the declaring type for which
		/// this template is loaded. If not found in entry assembly, the assembly where the type is defined
		/// will be searched
		/// </summary>
		/// <returns>The template stream</returns>
		public virtual Stream GetTemplateStreamFromPath (string path, Type declaringType)
		{
			Stream s = null;
			if (path.StartsWith ("#", StringComparison.Ordinal)) {
				string resId = path.Substring (1);
				s = Assembly.GetEntryAssembly ()?.GetManifestResourceStream (resId);
				if (s != null)
					return s;
				string assemblyName = resId.Split ('.')[0];
				Assembly a = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (aa => aa.GetName ().Name == assemblyName);
				s = a?.GetManifestResourceStream (resId);
				if (s != null)
					return s;
				s = Assembly.GetAssembly (declaringType).GetManifestResourceStream (resId);
				if (s == null)
					throw new Exception ($"Template ressource not found '{path}'");
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ($"Template not found: {path}", path);
				s = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return s;
		}
		/// <summary>Open file or find a resource from path string</summary>
		/// <returns>A file or resource stream</returns>
		/// <param name="path">This could be a normal file path, or an embedded ressource ID
		/// Resource ID's must be prefixed with '#' character</param>
		public virtual Stream GetStreamFromPath (string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#", StringComparison.Ordinal)) {
				string resId = path.Substring (1);
				stream = Assembly.GetEntryAssembly ()?.GetManifestResourceStream (resId);
				if (stream != null)
					return stream;
				string assemblyName = resId.Split ('.') [0];
				Assembly a = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (aa => aa.GetName ().Name == assemblyName);
				if (a == null)
					throw new Exception ($"Assembly '{assemblyName}' not found for ressource '{path}'.");
				stream = a.GetManifestResourceStream (resId);
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ($"File not found: {path}", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}
		public static Stream StaticGetStreamFromPath (string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#", StringComparison.Ordinal)) {
				string resId = path.Substring (1);
				stream = Assembly.GetEntryAssembly ()?.GetManifestResourceStream (resId);
				if (stream != null)
					return stream;
				string assemblyName = resId.Split ('.') [0];
				Assembly a = AppDomain.CurrentDomain.GetAssemblies ().FirstOrDefault (aa => aa.GetName ().Name == assemblyName);
				if (a == null)
					throw new Exception ($"Assembly '{assemblyName}' not found for ressource '{path}'.");
				stream = a.GetManifestResourceStream (resId);
				/*foreach (var s in a.GetManifestResourceNames()) {
					System.Diagnostics.Debug.WriteLine (s);
				}*/
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ($"File not found: {path}", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}
		/// <summary>
		/// Add the content of the IML fragment to the graphic tree of this interface
		/// </summary>
		/// <returns>return the new instance for convenience, may be ignored</returns>
		/// <param name="imlFragment">a valid IML string</param>
		public Widget LoadIMLFragment (string imlFragment) {
			lock (UpdateMutex) {
				Widget tmp = CreateITorFromIMLFragment (imlFragment).CreateInstance();
				AddWidget (tmp);
				return tmp;
			}
		}
		/// <summary>
		/// Create an instantiator bound to this interface from the IML fragment
		/// </summary>
		/// <returns>return the new instantiator</returns>
		/// <param name="imlFragment">a valid IML string</param>
		public Instantiator CreateITorFromIMLFragment (string imlFragment) {			
			return Instantiator.CreateFromImlFragment (this, imlFragment);
		}
		/// <summary>
		/// Create an instance of a GraphicObject and add it to the GraphicTree of this Interface
		/// </summary>
		/// <returns>new instance of graphic object created</returns>
		/// <param name="path">path of the iml file to load</param>
		public Widget Load (string path)
		{
			lock (UpdateMutex) {
				Widget tmp = CreateInstance (path);
				AddWidget (tmp);
				return tmp;
			}
		}
		/// <summary>
		/// Create an instance of a GraphicObject linked to this interface but not added to the GraphicTree
		/// </summary>
		/// <returns>new instance of graphic object created</returns>
		/// <param name="path">path of the iml file to load</param>
		public virtual Widget CreateInstance (string path)
		{
			//try {
				return GetInstantiator (path).CreateInstance ();
			//} catch (Exception ex) {
			//	throw new Exception ("Error loading <" + path + ">:", ex);
			//}
		}
		/// <summary>
		/// Create an instance of a GraphicObject linked to this interface but not added to the GraphicTree
		/// </summary>
		/// <returns>new instance of graphic object created</returns>
		/// <param name="path">path of the iml file to load</param>
		public virtual Widget CreateTemplateInstance (string path, Type declaringType)
		{
//			try {
				if (!Templates.ContainsKey (path))
					Templates [path] = new Instantiator (this, GetTemplateStreamFromPath(path, declaringType), path);
				return Templates [path].CreateInstance ();
			//} catch (Exception ex) {
			//	throw new Exception ("Error loading Template <" + path + ">:", ex);
			//}
		}
		/// <summary>
		/// Fetch instantiator from cache or create it.
		/// </summary>
		/// <returns>new Instantiator</returns>
		/// <param name="path">path of the iml file to load</param>
		public Instantiator GetInstantiator(string path){
			if (!Instantiators.ContainsKey(path))
				Instantiators [path] = new Instantiator(this, path);
			return Instantiators [path];
		}
		/// <summary>Item templates are derived from instantiator, this function
		/// try to fetch the requested one in the cache or create it.
		/// They have additional properties for recursivity and
		/// custom display per item type</summary>
		public virtual ItemTemplate GetItemTemplate(string path, Type declaringType){
			if (!ItemTemplates.ContainsKey(path))
				ItemTemplates [path] = new ItemTemplate(this, path, declaringType);
			return ItemTemplates [path] as ItemTemplate;
		}
		#endregion

		#region focus
		Widget _activeWidget;	//button is pressed on widget
		Widget _hoverWidget;		//mouse is over
		Widget _focusedWidget;	//has keyboard (or other perif) focus

		/// <summary>Widget is focused and button is down or another perif action is occuring
		/// , it can not lose focus while Active</summary>
		public Widget ActiveWidget
		{
			get { return _activeWidget; }
			set
			{
				if (_activeWidget == value)
					return;

				if (_activeWidget != null)
					_activeWidget.IsActive = false;

				_activeWidget = value;

				#if DEBUG_FOCUS
				NotifyValueChanged("ActiveWidget", _activeWidget);
				#endif

				if (_activeWidget != null)
				{
					_activeWidget.IsActive = true;
					#if DEBUG_FOCUS
					NotifyValueChanged("ActiveWidget", _activeWidget);
					Debug.WriteLine("Active => " + _activeWidget.ToString());
				}else
					Debug.WriteLine("Active => null");
					#else
				}
					#endif
			}
		}
		/// <summary>Pointer is over the widget</summary>
		public virtual Widget HoverWidget
		{
			get { return _hoverWidget; }
			set {
				if (_hoverWidget == value)
					return;

				if (_hoverWidget != null)
					_hoverWidget.IsHover = false;

				_hoverWidget = value;

				#if DEBUG_FOCUS
				NotifyValueChanged("HoverWidget", _hoverWidget);
#endif

				if (DragAndDropOperation == null && FOCUS_ON_HOVER) {
					Widget w = _hoverWidget;
					while (w != null) {
						if (w.Focusable) {
							FocusedWidget = w;
							break;
						}
						w = w.FocusParent;
					}
				}

				if (_hoverWidget != null)
				{
					_hoverWidget.IsHover = true;
#if DEBUG_FOCUS
					Debug.WriteLine("Hover => " + _hoverWidget.ToString());
				}else
					Debug.WriteLine("Hover => null");
#else
				}
#endif					
			}
		}
		/// <summary>Widget has the keyboard or mouse focus</summary>
		public Widget FocusedWidget {
			get { return _focusedWidget; }
			set {
				if (_focusedWidget == value)
					return;
				if (_focusedWidget != null)
					_focusedWidget.HasFocus = false;
				_focusedWidget = value;
				#if DEBUG_FOCUS
				NotifyValueChanged("FocusedWidget", _focusedWidget);
				#endif
				if (_focusedWidget != null)
				{
					_focusedWidget.HasFocus = true;
#if DEBUG_FOCUS
					Debug.WriteLine ("Focus => " + _hoverWidget.ToString ());
				} else
					Debug.WriteLine ("Focus => null");
#else
				}
#endif
			}
		}
		#endregion

		#region UPDATE Loops
		/// <summary>Enqueue Graphic object for Repaint, DrawingQueue is locked because
		/// GraphObj's property Set methods could trigger an update from another thread
		/// Once in that queue, that means that the layouting of obj and childs have succeed,
		/// the next step when dequeued will be clipping registration</summary>
		public void EnqueueForRepaint(Widget g)
		{			
			lock (ClippingMutex) {
				if (g.IsQueueForClipping)
					return;
				#if DEBUG_LOG
				DebugLog.AddEvent(DbgEvtType.GOEnqueueForRepaint, g);
				#endif	
				ClippingQueue.Enqueue (g);
				g.IsQueueForClipping = true;
			}
		}
		/// <summary>Main Update loop, executed in this interface thread, protected by the UpdateMutex
		/// Steps:
		/// 	- execute device Repeat events
		/// 	- Layouting
		/// 	- Clipping
		/// 	- Drawing
		/// Result: the Interface bitmap is drawn in memory (byte[] bmp) and a dirtyRect and bitmap are available
		/// </summary>
		public void Update(Context ctx = null){
			CrowThread[] tmpThreads;
			lock (CrowThreads) {
				tmpThreads = new CrowThread[CrowThreads.Count];
				Array.Copy (CrowThreads.ToArray (), tmpThreads, CrowThreads.Count);
			}
			for (int i = 0; i < tmpThreads.Length; i++)
				tmpThreads [i].CheckState ();

			//if (mouseRepeatTimer.ElapsedMilliseconds > 0) {
			//	if ((bool)_hoverWidget?.MouseRepeat) {
			//		int repeatCount = (int)mouseRepeatTimer.ElapsedMilliseconds / DEVICE_REPEAT_INTERVAL - mouseRepeatCount;
			//		for (int i = 0; i < repeatCount; i++)
			//			_hoverWidget.onMouseDown (_hoverWidget, lastMouseDownEvent);
			//		mouseRepeatCount += repeatCount;
			//	}
			//} else if (lastMouseDown.ElapsedMilliseconds > DEVICE_REPEAT_DELAY)
				//mouseRepeatTimer.Start ();

			if (!Monitor.TryEnter (UpdateMutex))
				return;

			#if MEASURE_TIME
			updateMeasure.StartCycle();
			#endif

			/*#if DEBUG_LOG
			DebugLog.AddEvent (DbgEvtType.IFaceUpdate);
			#endif*/

			processLayouting ();

			clippingRegistration ();

			if (ctx == null) {
				using (ctx = new Context (surf)) 
					processDrawing (ctx);
			}else
				processDrawing (ctx);

#if MEASURE_TIME
			updateMeasure.StopCycle();
#endif

			Monitor.Exit (UpdateMutex);
		}
		/// <summary>Layouting loop, this is the first step of the udpate and process registered
		/// Layouting queue items. Failing LQI's are requeued in this cycle until MaxTry is reached which
		/// trigger an enqueue for the next Update Cycle</summary>
		protected virtual void processLayouting(){
			#if MEASURE_TIME
			layoutingMeasure.StartCycle();
			#endif
			if (Monitor.TryEnter (LayoutMutex)) {
				#if DEBUG_LOG
				if (LayoutingQueue.Count > 0)
					DebugLog.AddEvent (DbgEvtType.IFaceStartLayouting);
				#endif
				DiscardQueue = new Queue<LayoutingQueueItem> ();
				//Debug.WriteLine ("======= Layouting queue start =======");
				LayoutingQueueItem lqi;
				while (LayoutingQueue.Count > 0) {
					lqi = LayoutingQueue.Dequeue ();
					lqi.ProcessLayouting ();
				}
				LayoutingQueue = DiscardQueue;
				Monitor.Exit (LayoutMutex);
				DiscardQueue = null;
			}
			/*#if DEBUG_LOG
			DebugLog.AddEvent (DbgEvtType.IFaceStartLayouting);
			#endif*/
			#if MEASURE_TIME
			layoutingMeasure.StopCycle();
			#endif
		}
		/// <summary>Degueue Widget to clip from DrawingQueue and register the last painted slot and the new one
		/// Clipping rectangles are added at each level of the tree from leef to root, that's the way for the painting
		/// operation to known if it should go down in the tree for further graphic updates and repaints</summary>
		void clippingRegistration(){
			#if MEASURE_TIME
			clippingMeasure.StartCycle();
			#endif
			#if DEBUG_LOG
			if (ClippingQueue.Count > 0)
				DebugLog.AddEvent (DbgEvtType.IFaceStartClipping);
			#endif
			Widget g = null;
			while (ClippingQueue.Count > 0) {
				lock (ClippingMutex) {
					g = ClippingQueue.Dequeue ();
					g.IsQueueForClipping = false;
				}
				g.ClippingRegistration ();
			}
			/*#if DEBUG_LOG
			DebugLog.AddEvent (DbgEvtType.IFaceEndClipping);
			#endif*/
			#if MEASURE_TIME
			clippingMeasure.StopCycle();
			#endif
		}
		/// <summary>Clipping Rectangles drive the drawing process. For compositing, each object under a clip rectangle should be
		/// repainted. If it contains also clip rectangles, its cache will be update, or if not cached a full redraw will take place</summary>
		void processDrawing(Context ctx){
			#if MEASURE_TIME
			drawingMeasure.StartCycle();
			#endif
			#if DEBUG_LOG
			if (!clipping.IsEmpty)
				DebugLog.AddEvent (DbgEvtType.IFaceStartDrawing);
			#endif
			if (DragImage != null)
				clipping.UnionRectangle(new Rectangle (DragImageX, DragImageY, DragImageWidth, DragImageHeight));
				if (!clipping.IsEmpty) {
					IsDirty = true;

					ctx.PushGroup ();

					for (int i = GraphicTree.Count -1; i >= 0 ; i--){
						Widget p = GraphicTree[i];
						if (!p.Visible)
							continue;
						if (clipping.Contains (p.Slot) == RegionOverlap.Out)
							continue;

						ctx.Save ();
						p.Paint (ref ctx);
						ctx.Restore ();
					}

					if (DragAndDropOperation != null) {
						if (DragImage != null) {
							DirtyRect += new Rectangle (DragImageX, DragImageY, DragImageWidth, DragImageHeight);
							DragImageX = MousePosition.X - DragImageWidth / 2;
							DragImageY = MousePosition.Y - DragImageHeight / 2;
							ctx.Save ();
							ctx.ResetClip ();
							ctx.SetSourceSurface (DragImage, DragImageX, DragImageY);
							ctx.PaintWithAlpha (0.8);
							ctx.Restore ();
							DirtyRect += new Rectangle (DragImageX, DragImageY, DragImageWidth, DragImageHeight);
							IsDirty = true;
							//System.Diagnostics.Debug.WriteLine ("dragimage drawn: {0},{1}", DragImageX, DragImageY);
						}
					}

#if DEBUG_CLIP_RECTANGLE
					ctx.LineWidth = 1;
					ctx.SetSourceColor(Color.Magenta.AdjustAlpha (0.5));
					for (int i = 0; i < clipping.NumRectangles; i++)
						ctx.Rectangle(clipping.GetRectangle(i));
					ctx.Stroke ();
#endif

					ctx.PopGroupToSource ();

					for (int i = 0; i < clipping.NumRectangles; i++)
						ctx.Rectangle (clipping.GetRectangle (i));

					ctx.ClipPreserve ();
					ctx.Operator = Operator.Clear;
					ctx.Fill ();
					ctx.Operator = Operator.Over;

					ctx.Paint ();
					
					surf.Flush ();

					clipping.Dispose ();
					clipping = new Region ();
				}
			
			/*#if DEBUG_LOG
			DebugLog.AddEvent (DbgEvtType.IFaceEndDrawing);
			#endif*/
			#if MEASURE_TIME
			drawingMeasure.StopCycle();
			#endif
		}
		#endregion

		#region GraphicTree handling
		/// <summary>Add widget to the Graphic tree of this interface and register it for layouting</summary>
		public Widget AddWidget(Widget g)
		{
			g.Parent = this;
			int ptr = 0;
			Window newW = g as Window;
			if (newW != null) {
				while (ptr < GraphicTree.Count) {
					Window w = GraphicTree [ptr] as Window;
					if (w == null)
						break;
					if (newW.AlwaysOnTop || !w.AlwaysOnTop)
						break;
					
					ptr++;
				}
			}

			lock (UpdateMutex)
				GraphicTree.Insert (ptr, g);

			g.RegisteredLayoutings = LayoutingType.None;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);

			return g;
		}
		/// <summary>Set visible state of widget to false and delete if from the graphic tree</summary>
		public void DeleteWidget(Widget g)
		{
			lock (UpdateMutex) {
				RegisterClip (g.ScreenCoordinates (g.LastPaintedSlot));
				GraphicTree.Remove (g);
				g.Parent = null;
				g.Dispose ();
			}
		}
		/// <summary>Set visible state of widget to false and remove if from the graphic tree</summary>
		public void RemoveWidget(Widget g)
		{
//			if (g.Contains(HoverWidget)) {
//				while (HoverWidget != g.focusParent) {
//					HoverWidget.onMouseLeave (HoverWidget, null);
//					HoverWidget = HoverWidget.focusParent;
//				}
//			}
			lock (UpdateMutex) {
				RegisterClip (g.ScreenCoordinates (g.LastPaintedSlot));
				GraphicTree.Remove (g);
				g.Parent = null;
			}
		}
		/// <summary> Remove all Graphic objects from top container </summary>
		public void ClearInterface()
		{
			lock (UpdateMutex) {
				while (GraphicTree.Count > 0) {
					//TODO:parent is not reset to null because object will be added
					//to ObjectToRedraw list, and without parent, it fails
					Widget g = GraphicTree [0];
					RegisterClip (g.ScreenCoordinates (g.LastPaintedSlot));
					GraphicTree.RemoveAt (0);
					g.Dispose ();
				}
			}
			#if DEBUG_LAYOUTING
			LQIsTries = new List<LQIList>();
			curLQIsTries = new LQIList();
			LQIs = new List<LQIList>();
			curLQIs = new LQIList();
			#endif
		}
		/// <summary> Put widget on top of other root widgets</summary>
		public void PutOnTop(Widget g, bool isOverlay = false)
		{
			int ptr = 0;
			Window newW = g as Window;
			if (newW != null) {
				while (ptr < GraphicTree.Count) {
					Window w = GraphicTree [ptr] as Window;
					if (w != null) {
						if (newW.AlwaysOnTop || !w.AlwaysOnTop)
							break;
					}
					ptr++;
				}
			}
			if (GraphicTree.IndexOf(g) > ptr)
			{
				lock (UpdateMutex) {
					GraphicTree.Remove (g);
					GraphicTree.Insert (ptr, g);
				}
				EnqueueForRepaint (g);
			}
		}

		/// <summary>Search a Graphic object in the tree named 'nameToFind'</summary>
		public Widget FindByName (string nameToFind)
		{
			foreach (Widget w in GraphicTree) {
				Widget r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		#endregion

		/// <summary>
		/// Resize the interface. This function should be called by the host
		/// when window resize event occurs. 
		/// </summary>
		/// <param name="bounds">bounding box of the interface</param>
		public virtual void ProcessResize(Rectangle bounds){
			lock (UpdateMutex) {
				clientRectangle = bounds;

				surf.SetSize (clientRectangle.Width, clientRectangle.Height);

				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}
		}

		#region Mouse and Keyboard Handling
		MouseCursor cursor = MouseCursor.top_left_arrow;
		static void loadCursors ()
		{
			const int minimumSize = 24;
			//Load cursors
			XCursor.Cursors [MouseCursor.arrow] = XCursorFile.Load ("#Crow.Cursors.arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.base_arrow_down] = XCursorFile.Load ("#Crow.Cursors.base_arrow_down").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.base_arrow_up] = XCursorFile.Load ("#Crow.Cursors.base_arrow_up").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.boat] = XCursorFile.Load ("#Crow.Cursors.boat").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_left_corner] = XCursorFile.Load ("#Crow.Cursors.bottom_left_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_right_corner] = XCursorFile.Load ("#Crow.Cursors.bottom_right_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_side] = XCursorFile.Load ("#Crow.Cursors.bottom_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_tee] = XCursorFile.Load ("#Crow.Cursors.bottom_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.center_ptr] = XCursorFile.Load ("#Crow.Cursors.center_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.circle] = XCursorFile.Load ("#Crow.Cursors.circle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.cross] = XCursorFile.Load ("#Crow.Cursors.cross").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.cross_reverse] = XCursorFile.Load ("#Crow.Cursors.cross_reverse").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.crosshair] = XCursorFile.Load ("#Crow.Cursors.crosshair").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.dot] = XCursorFile.Load ("#Crow.Cursors.dot").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.dot_box_mask] = XCursorFile.Load ("#Crow.Cursors.dot_box_mask").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.double_arrow] = XCursorFile.Load ("#Crow.Cursors.double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draft_large] = XCursorFile.Load ("#Crow.Cursors.draft_large").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draft_small] = XCursorFile.Load ("#Crow.Cursors.draft_small").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draped_box] = XCursorFile.Load ("#Crow.Cursors.draped_box").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.exchange] = XCursorFile.Load ("#Crow.Cursors.exchange").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.fleur] = XCursorFile.Load ("#Crow.Cursors.fleur").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.gumby] = XCursorFile.Load ("#Crow.Cursors.gumby").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand] = XCursorFile.Load ("#Crow.Cursors.hand").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand1] = XCursorFile.Load ("#Crow.Cursors.hand1").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand2] = XCursorFile.Load ("#Crow.Cursors.hand2").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.help] = XCursorFile.Load ("#Crow.Cursors.help").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ibeam] = XCursorFile.Load ("#Crow.Cursors.ibeam").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_ptr] = XCursorFile.Load ("#Crow.Cursors.left_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_ptr_watch] = XCursorFile.Load ("#Crow.Cursors.left_ptr_watch").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_side] = XCursorFile.Load ("#Crow.Cursors.left_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_tee] = XCursorFile.Load ("#Crow.Cursors.left_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ll_angle] = XCursorFile.Load ("#Crow.Cursors.ll_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.lr_angle] = XCursorFile.Load ("#Crow.Cursors.lr_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.move] = XCursorFile.Load ("#Crow.Cursors.move").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.pencil] = XCursorFile.Load ("#Crow.Cursors.pencil").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.pirate] = XCursorFile.Load ("#Crow.Cursors.pirate").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.plus] = XCursorFile.Load ("#Crow.Cursors.plus").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.question_arrow] = XCursorFile.Load ("#Crow.Cursors.question_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_ptr] = XCursorFile.Load ("#Crow.Cursors.right_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_side] = XCursorFile.Load ("#Crow.Cursors.right_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_tee] = XCursorFile.Load ("#Crow.Cursors.right_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sailboat] = XCursorFile.Load ("#Crow.Cursors.sailboat").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_down_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_down_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_h_double_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_h_double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_left_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_left_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_right_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_right_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_up_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_up_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_v_double_arrow] = XCursorFile.Load ("#Crow.Cursors.sb_v_double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.shuttle] = XCursorFile.Load ("#Crow.Cursors.shuttle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sizing] = XCursorFile.Load ("#Crow.Cursors.sizing").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.target] = XCursorFile.Load ("#Crow.Cursors.target").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.tcross] = XCursorFile.Load ("#Crow.Cursors.tcross").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_left_arrow] = XCursorFile.Load ("#Crow.Cursors.top_left_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_left_corner] = XCursorFile.Load ("#Crow.Cursors.top_left_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_right_corner] = XCursorFile.Load ("#Crow.Cursors.top_right_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_side] = XCursorFile.Load ("#Crow.Cursors.top_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_tee] = XCursorFile.Load ("#Crow.Cursors.top_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.trek] = XCursorFile.Load ("#Crow.Cursors.trek").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ul_angle] = XCursorFile.Load ("#Crow.Cursors.ul_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ur_angle] = XCursorFile.Load ("#Crow.Cursors.ur_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.watch] = XCursorFile.Load ("#Crow.Cursors.watch").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.X_cursor] = XCursorFile.Load ("#Crow.Cursors.X_cursor").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.xterm] = XCursorFile.Load ("#Crow.Cursors.xterm").Cursors.First (c => c.Width >= minimumSize);
		}


		Stopwatch lastMouseDown = Stopwatch.StartNew (), mouseRepeatTimer = new Stopwatch ();
		bool doubleClickTriggered;	//next mouse up will trigger a double click
		//int mouseRepeatCount;
		MouseButtonEventArgs lastMouseDownEvent;

		public Point MousePosition { get; set; } = default;
		public bool IsDown (MouseButton button) => Glfw3.GetMouseButton (hWin, button) != InputAction.Release;

		Cursor createCursor (MouseCursor mc)
		{
			XCursor c = XCursor.Cursors [mc];
			return new CustomCursor (c.Width, c.Height, c.data, c.Xhot, c.Yhot);
		}


		public MouseCursor MouseCursor {
			get => cursor;
			set {
			
				if (value == cursor)
					return;
				cursor = value;

				currentCursor?.Dispose ();
				currentCursor = createCursor (cursor);				
				currentCursor.Set (hWin);
				//MouseCursorChanged.Raise (this,new MouseCursorChangedEventArgs(cursor));
			}
		}
		/// <summary>Processes mouse move events from the root container, this function
		/// should be called by the host on mouse move event to forward events to crow interfaces</summary>
		/// <returns>true if mouse is in the interface</returns>
		public virtual bool OnMouseMove (int x, int y)
		{
			int deltaX = x - MousePosition.X;
			int deltaY = y - MousePosition.Y;
			MousePosition = new Point (x, y);
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);

			if (ActiveWidget != null && DragAndDropOperation == null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				_activeWidget.onMouseMove (this, e);
				return true;
			}

			if (DragAndDropOperation != null)//drag source cant have hover event, so move has to be handle here
				DragAndDropOperation.DragSource.onMouseMove (this, e);

			if (_hoverWidget != null) {
				resetTooltip ();
				//check topmost graphicobject first
				Widget tmp = _hoverWidget;
				Widget topc = null;
				while (tmp is Widget) {
					topc = tmp;
					tmp = tmp.LogicalParent as Widget;
				}
				int idxhw = GraphicTree.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						//if logical parent of top container is a widget, that's a popup
						if (GraphicTree [i].LogicalParent is Interface) {
							if (GraphicTree [i].MouseIsIn (e.Position)) {
								while (_hoverWidget != null) {
									_hoverWidget.onMouseLeave (_hoverWidget, e);
									HoverWidget = _hoverWidget.FocusParent;
								}

								GraphicTree [i].checkHoverWidget (e);
								_hoverWidget.onMouseMove (this, e);
								return true;
							}
						}
						i++;
					}
				}

				if (_hoverWidget.MouseIsIn (e.Position)) {
					_hoverWidget.checkHoverWidget (e);
					_hoverWidget.onMouseMove (this, e);
					return true;
				}
				_hoverWidget.onMouseLeave (_hoverWidget, e);
				//seek upward from last focused graph obj's
				tmp = _hoverWidget.FocusParent;
				while (tmp != null) {
					HoverWidget = tmp;
					if (_hoverWidget.MouseIsIn (e.Position)) {
						_hoverWidget.checkHoverWidget (e);
						_hoverWidget.onMouseMove (_hoverWidget, e);
						return true;
					}
					_hoverWidget.onMouseLeave (_hoverWidget, e);
					tmp = _hoverWidget.FocusParent;
				}
			}

			//top level graphic obj's parsing
			lock (GraphicTree) {
				for (int i = 0; i < GraphicTree.Count; i++) {
					Widget g = GraphicTree [i];
					if (g.MouseIsIn (e.Position)) {
						g.checkHoverWidget (e);
						if (g is Window)
							PutOnTop (g);
						_hoverWidget.onMouseMove (_hoverWidget, e);
						return true;
					}
				}
			}
			HoverWidget = null;
			return false;
		}
		/// <summary>
		/// Forward the mouse down event from the host to the hover widget in the crow interface
		/// </summary>
		/// <returns>return true, if interface handled the event, false otherwise.</returns>
		/// <param name="button">Button index</param>
		public virtual bool OnMouseButtonDown (MouseButton button)
		{
			doubleClickTriggered = (lastMouseDown.ElapsedMilliseconds < DOUBLECLICK_TRESHOLD);
			lastMouseDown.Restart ();
			//mouseRepeatCount = -1;//stays negative until repeat delay is hit

			lastMouseDownEvent = new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Press);

			if (_hoverWidget == null)
				return false;

			_hoverWidget.onMouseDown (_hoverWidget, lastMouseDownEvent);

			ActiveWidget = _hoverWidget;
			return true;
		}
		/// <summary>
		/// Forward the mouse up event from the host to the crow interface
		/// </summary>
		/// <returns>return true, if interface handled the event, false otherwise.</returns>
		/// <param name="button">Button index</param>
		public virtual bool OnMouseButtonUp (MouseButton button)
		{
			mouseRepeatTimer.Reset ();

			MouseButtonEventArgs e = new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Repeat);
			if (_activeWidget == null)
				return false;

			_activeWidget.onMouseUp (_activeWidget, e);

			if (doubleClickTriggered)
				_activeWidget.onMouseDoubleClick (_activeWidget, e);
			else
				_activeWidget.onMouseClick (_activeWidget, e);

			ActiveWidget = null;
			//			if (!lastActive.MouseIsIn (Mouse.Position)) {
			//				ProcessMouseMove (Mouse.X, Mouse.Y);
			//			}
			return true;
		}
		/// <summary>
		/// Forward the mouse wheel event from the host to the crow interface
		/// </summary>
		/// <returns>return true, if interface handled the event, false otherwise.</returns>
		/// <param name="delta">wheel delta</param>
		public virtual bool OnMouseWheelChanged (float delta)
		{
			MouseWheelEventArgs e = new MouseWheelEventArgs ((int)delta);

			if (_hoverWidget == null)
				return false;
			_hoverWidget.onMouseWheel (_hoverWidget, e);
			return true;
		}

		public virtual bool OnKeyPress (char c)
		{
			if (_focusedWidget == null)
				return false;
			_focusedWidget.onKeyPress (_focusedWidget, new KeyPressEventArgs (c));
			return true;
		}
		public virtual bool OnKeyUp (Key key)
		{
			if (_focusedWidget == null)
				return false;
			_focusedWidget.onKeyUp (_focusedWidget, new KeyEventArgs (key, false));
			return true;


			//			if (keyboardRepeatThread != null) {
			//				keyboardRepeatOn = false;
			//				keyboardRepeatThread.Abort();
			//				keyboardRepeatThread.Join ();
			//			}
		}
		public virtual bool OnKeyDown (Key key)
		{
			//Keyboard.SetKeyState((Crow.Key)Key,true);
			lastKeyDownEvt = new KeyEventArgs (key, true);

			if (_focusedWidget == null)
				return false;
			_focusedWidget.onKeyDown (_focusedWidget, new KeyEventArgs (key, false));
			return true;

			//			keyboardRepeatThread = new Thread (keyboardRepeatThreadFunc);
			//			keyboardRepeatThread.IsBackground = true;
			//			keyboardRepeatThread.Start ();
		}

		public bool IsKeyDown (Key key)
		{
			return false;
		}
		#endregion

		#region Tooltip handling
		Stopwatch tooltipTimer = new Stopwatch ();
		Widget ToolTipContainer;
		volatile bool tooltipVisible;

		protected void initTooltip ()
		{
			ToolTipContainer = CreateInstance ("#Crow.Tooltip.template");
			Thread t = new Thread (toolTipThreadFunc);
			t.IsBackground = true;
			t.Start ();
		}
		void toolTipThreadFunc ()
		{
			while (true) {
				if (tooltipTimer.ElapsedMilliseconds > TOOLTIP_DELAY) {
					if (!tooltipVisible) {
						Widget g = _hoverWidget;
						while (g != null) {
							if (!string.IsNullOrEmpty (g.Tooltip)) {
								AddWidget (ToolTipContainer);
								ToolTipContainer.DataSource = g;
								ToolTipContainer.Top = MousePosition.Y + 10;
								ToolTipContainer.Left = MousePosition.X + 10;
								tooltipVisible = true;
								break;
							}
							g = g.LogicalParent as Widget;
						}
					}
				}
				Thread.Sleep (200);
			}

		}
		void resetTooltip ()
		{
			if (tooltipVisible) {
				//ToolTipContainer.DataSource = null;
				RemoveWidget (ToolTipContainer);
				tooltipVisible = false;
			}
			tooltipTimer.Restart ();
		}
		#endregion

		#region Contextual menu
		MenuItem ctxMenuContainer;
		protected void initContextMenus ()
		{
			ctxMenuContainer = CreateInstance ("#Crow.ContextMenu.template") as MenuItem;
			ctxMenuContainer.LayoutChanged += CtxMenuContainer_LayoutChanged;
		}

		void CtxMenuContainer_LayoutChanged (object sender, LayoutingEventArgs e)
		{
			Rectangle r = ctxMenuContainer.ScreenCoordinates (ctxMenuContainer.Slot);
			if (e.LayoutType == LayoutingType.Width || e.LayoutType == LayoutingType.X) {
				if (r.Right > this.clientRectangle.Right)
					ctxMenuContainer.Left = this.clientRectangle.Right - ctxMenuContainer.Slot.Width;
			} else if (e.LayoutType == LayoutingType.Width || e.LayoutType == LayoutingType.Y) {
				if (r.Bottom > this.clientRectangle.Bottom)
					ctxMenuContainer.Top = this.clientRectangle.Bottom - ctxMenuContainer.Slot.Height;
			}

		}

		public void ShowContextMenu (Widget go)
		{

			lock (UpdateMutex) {
				if (ctxMenuContainer.Parent == null)
					this.AddWidget (ctxMenuContainer);
				else
					ctxMenuContainer.IsOpened = true;

				//ctxMenuContainer.isPopup = true;
				ctxMenuContainer.LogicalParent = go;
				ctxMenuContainer.DataSource = go;

				PutOnTop (ctxMenuContainer, true);
			}
			ctxMenuContainer.Left = MousePosition.X - 5;
			ctxMenuContainer.Top = MousePosition.Y - 5;

			_hoverWidget = ctxMenuContainer;
			ctxMenuContainer.onMouseEnter (ctxMenuContainer, new MouseMoveEventArgs (MousePosition.X, MousePosition.Y, 0, 0));
		}
		#endregion

		#region Device Repeat Events
		volatile bool keyboardRepeatOn;
		volatile int keyboardRepeatCount;
		KeyEventArgs lastKeyDownEvt;

		void keyboardRepeatThreadFunc ()
		{
			keyboardRepeatOn = true;
			Thread.Sleep (Interface.DEVICE_REPEAT_DELAY);
			while (keyboardRepeatOn) {
				keyboardRepeatCount++;
				Thread.Sleep (Interface.DEVICE_REPEAT_INTERVAL);
			}
			keyboardRepeatCount = 0;
		}
		#endregion

		#region ILayoutable implementation
		public virtual bool PointIsIn (ref Point m)
		{
			return true;
		}
		public void RegisterClip (Rectangle r)
		{
			clipping.UnionRectangle (r);
		}
		public bool ArrangeChildren { get { return false; } }
		public int LayoutingTries {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}
		public LayoutingType RegisteredLayoutings {
			get { return LayoutingType.None; }
			set { throw new NotImplementedException (); }
		}
		public void RegisterForLayouting (LayoutingType layoutType) { throw new NotImplementedException (); }
		public bool UpdateLayout (LayoutingType layoutType) { throw new NotImplementedException (); }
		public Rectangle ContextCoordinates (Rectangle r) { return r; }
		public Rectangle ScreenCoordinates (Rectangle r) { return r; }

		public ILayoutable Parent {
			get { return null; }
			set { throw new NotImplementedException (); }
		}
		public ILayoutable LogicalParent {
			get { return null; }
			set { throw new NotImplementedException (); }
		}

		public Rectangle ClientRectangle {
			get { return clientRectangle; }
		}
		public Interface HostContainer {
			get { return this; }
		}
		public Rectangle getSlot () { return ClientRectangle; }
		#endregion

#if MEASURE_TIME
		public PerformanceMeasure clippingMeasure = new PerformanceMeasure ("Clipping", 1);
		public PerformanceMeasure layoutingMeasure = new PerformanceMeasure ("Layouting", 1);
		public PerformanceMeasure updateMeasure = new PerformanceMeasure ("Update", 1);
		public PerformanceMeasure drawingMeasure = new PerformanceMeasure ("Drawing", 1);
		public List<PerformanceMeasure> PerfMeasures = new List<PerformanceMeasure> ();
#endif
#if DEBUG_LAYOUTING
		public List<LQIList> LQIsTries = new List<LQIList>();
		public LQIList curLQIsTries = new LQIList();
		public List<LQIList> LQIs = new List<LQIList>();
		public LQIList curLQIs = new LQIList();
		//		public static LayoutingQueueItem[] MultipleRunsLQIs {
		//			get { return curUpdateLQIs.Where(l=>l.LayoutingTries>2 || l.DiscardCount > 0).ToArray(); }
		//		}
		public LayoutingQueueItem currentLQI;
#else
		public List<LQIList> LQIs = null;//still create the var for CrowIDE
#endif
	}
}

