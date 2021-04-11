// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Crow.Cairo;
using Crow.IML;
using Glfw;
using Path = System.IO.Path;

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
		public void NotifyValueChanged (string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		public void NotifyValueChanged (object _value, [CallerMemberName] string caller = null)
		{
			NotifyValueChanged (caller, _value);
		}
		#endregion

		internal static List<Assembly> crowAssemblies = new List<Assembly> ();

		#region CTOR
		static Interface ()
		{
			CROW_CONFIG_ROOT =
				System.IO.Path.Combine (
					Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
					".config");
			CROW_CONFIG_ROOT = System.IO.Path.Combine (CROW_CONFIG_ROOT, "crow");
			if (!Directory.Exists (CROW_CONFIG_ROOT))
				Directory.CreateDirectory (CROW_CONFIG_ROOT);

			FontRenderingOptions = new FontOptions ();
			FontRenderingOptions.Antialias = Antialias.Subpixel;
			FontRenderingOptions.HintMetrics = HintMetrics.On;
			FontRenderingOptions.HintStyle = HintStyle.Full;
			FontRenderingOptions.SubpixelOrder = SubpixelOrder.Default;

			preloadCrowAssemblies ();
		}
		static void preloadCrowAssemblies () {
			//ensure all assemblies are loaded, because IML could contains classes not instanciated in source
			Assembly ea = Assembly.GetEntryAssembly ();
			System.IO.FileStream[] files = ea.GetFiles ();
			foreach (AssemblyName an in ea.GetReferencedAssemblies()) {
				try {
					Assembly a = Assembly.ReflectionOnlyLoad (an.Name);
					if (a == Assembly.GetExecutingAssembly ())
							continue;
					if (a.GetCustomAttribute (typeof (CrowAttribute)) != null)
							crowAssemblies.Add (a);
				} catch {

				}											
			}
		}

		public Interface (int width, int height, IntPtr glfwWindowHandle) : this (width, height, false, false)
		{
			hWin = glfwWindowHandle;
		}
		public Interface (int width = 800, int height = 600, bool startUIThread = true, bool createSurface = true)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			//CurrentInterface = this;
			clientRectangle = new Rectangle (0, 0, width, height);

			if (createSurface)
				initSurface ();

			if (startUIThread) {
				Thread t = new Thread (InterfaceThread) {
					IsBackground = true
				};
				t.Start ();
			}

			PerformanceMeasure.InitMeasures ();
		}
		#endregion

#if MEASURE_TIME
		public PerformanceMeasure[] PerfMeasures => PerformanceMeasure.Measures;
#endif
		/** GLFW callback may return a custom pointer, this list makes the link between the GLFW window pointer and the
			manage VkWindow instance. */
		static Dictionary<IntPtr, Interface> windows = new Dictionary<IntPtr, Interface> ();
		/** GLFW window native pointer and current native handle for mouse cursor */
		IntPtr hWin;
		Cursor currentCursor;
		bool ownWindow;

		public IntPtr WindowHandle => hWin;

		protected void registerGlfwCallbacks ()
		{
			windows.Add (hWin, this);
			Glfw3.SetKeyCallback (hWin, HandleKeyDelegate);
			Glfw3.SetMouseButtonPosCallback (hWin, HandleMouseButtonDelegate);
			Glfw3.SetCursorPosCallback (hWin, HandleCursorPosDelegate);
			Glfw3.SetScrollCallback (hWin, HandleScrollDelegate);
			Glfw3.SetCharCallback (hWin, HandleCharDelegate);
			Glfw3.SetWindowSizeCallback (hWin, HandleWindowSizeDelegate);
		}

		protected void initSurface ()
		{
			Glfw3.Init ();

			Glfw3.WindowHint (WindowAttribute.ClientApi, 0);
			Glfw3.WindowHint (WindowAttribute.Resizable, 1);
			Glfw3.WindowHint (WindowAttribute.Decorated, 1);

			hWin = Glfw3.CreateWindow (clientRectangle.Width, clientRectangle.Height, "win name", MonitorHandle.Zero, IntPtr.Zero);
			if (hWin == IntPtr.Zero)
				throw new Exception ("[GLFW3] Unable to create vulkan Window");
			ownWindow = true;

			registerGlfwCallbacks ();

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
		}
		internal Dictionary<string, MethodInfo> knownExtMethods = new Dictionary<string, MethodInfo> ();
		internal MethodInfo SearchExtMethod (Type t, string methodName) {
			string key = t.Name + "." + methodName;
			if (knownExtMethods.ContainsKey (key))
				return knownExtMethods [key];

			//System.Diagnostics.Debug.WriteLine ($"*** search extension method: {t};{methodName} => key={key}");

			MethodInfo mi = null;
			if (!CompilerServices.TryGetExtensionMethods (Assembly.GetEntryAssembly (), t, methodName, out mi)) {
				if (!CompilerServices.TryGetExtensionMethods (t.Module.Assembly, t, methodName, out mi)) {
					foreach (Assembly a in crowAssemblies) {
						if (CompilerServices.TryGetExtensionMethods (a, t, methodName, out mi))
							break;
					}
					if (mi == null)
						CompilerServices.TryGetExtensionMethods (Assembly.GetExecutingAssembly (), t, methodName, out mi);//crow Assembly
				}
			}

			//add key even if mi is null to prevent searching again and again for propertyless bindings
			knownExtMethods.Add (key, mi);
			return mi;
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

		public string WindowTitle {			
			set => Glfw3.SetWindowTitle (hWin, value);
		}

		public bool Running {
			get => !Glfw3.WindowShouldClose (hWin);
			set => Glfw3.SetWindowShouldClose (hWin, value == true ? 0 : 1);
		}
		public virtual void InterfaceThread ()
		{
			while (!Glfw3.WindowShouldClose (hWin)) {
				Update ();
				Thread.Sleep (UPDATE_INTERVAL);
			}
		}
		protected virtual void OnInitialized ()
		{
			/*try {
				Load ("#main.crow").DataSource = this;
			} catch { }*/
			Initialized.Raise (this, null);
		}
		/// <summary>
		/// load styling, init default tooltips and context menus, load main.crow resource if exists.
		/// </summary>
		public void Init () {
			DbgLogger.StartEvent (DbgEvtType.IFaceInit);
			initDictionaries ();
			loadStyling ();
			loadThemeFiles ();
			initTooltip ();
			initContextMenus ();
			OnInitialized ();
			DbgLogger.EndEvent (DbgEvtType.IFaceInit);
		}
		/// <summary>
		/// call Init() then enter the running loop performing ProcessEvents until running==false.
		/// </summary>
		public virtual void Run () {
			Init ();

			while (!Glfw3.WindowShouldClose (hWin)) {
				Glfw3.PollEvents ();
				UpdateFrame ();
			}
		}
		public virtual void UpdateFrame () { Thread.Sleep (1); }

		public virtual void Quit () => Glfw3.SetWindowShouldClose (hWin, 1);

		public bool Shift => Glfw3.GetKey(hWin, Key.LeftShift) == InputAction.Press ||
			Glfw3.GetKey (hWin, Key.RightShift) == InputAction.Press;
		public bool Ctrl => Glfw3.GetKey (hWin, Key.LeftControl) == InputAction.Press ||
			Glfw3.GetKey (hWin, Key.RightControl) == InputAction.Press;
		public bool Alt => Glfw3.GetKey (hWin, Key.LeftAlt) == InputAction.Press ||
			Glfw3.GetKey (hWin, Key.RightAlt) == InputAction.Press;

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
				disposeContextMenus ();

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
		//initial capacity for layouting and clipping queues.
		const int INIT_QUEUE_CAPACITY = 512;
		/// <summary>Time interval in milisecond between Updates of the interface</summary>
		public static int UPDATE_INTERVAL = 5;
		/// <summary>Crow configuration root path</summary>
		public static string CROW_CONFIG_ROOT;
		/// <summary>If true, mouse focus is given when mouse is over control</summary>
		public static bool FOCUS_ON_HOVER = false;
		/// <summary>If true, newly focused window will be put on top</summary>
		public static bool RAISE_WIN_ON_FOCUS = true;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 3;
		/// <summary> delay before tooltip appears </summary>
		public static int TOOLTIP_DELAY = 500;
		/// <summary>Double click threshold in milisecond</summary>
		public static int DOUBLECLICK_TRESHOLD = 320;//max duration between two mouse_down evt for a dbl clk in milisec.
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DEVICE_REPEAT_DELAY = 600;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DEVICE_REPEAT_INTERVAL = 100;
		public static float WheelIncrement = 1;
		/// <summary>Tabulation size in Text controls</summary>
		public static int TAB_SIZE = 4;
		[Obsolete]public static string LineBreak = "\n";
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
		//protected static Interface CurrentInterface;
		#endregion

		#region Events
		//public event EventHandler<MouseCursorChangedEventArgs> MouseCursorChanged;
		////public event EventHandler Quit;
		public event EventHandler Initialized;

		public event EventHandler StartDragOperation;
		public event EventHandler EndDragOperation;
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
		public Queue<LayoutingQueueItem> LayoutingQueue = new Queue<LayoutingQueueItem> (INIT_QUEUE_CAPACITY);
		/// <summary>Store discarded lqi between two updates</summary>
		public Queue<LayoutingQueueItem> DiscardQueue;
		/// <summary>Main drawing queue, holding layouted controls</summary>
		public Queue<Widget> ClippingQueue = new Queue<Widget>(INIT_QUEUE_CAPACITY);
		//TODO:use object instead for complex copy paste
		public string Clipboard {
			get => Glfw3.GetClipboardString (hWin);
			set => Glfw3.SetClipboardString (hWin, value);
		}
		/// <summary>each IML and fragments (such as inline Templates) are compiled as a Dynamic Method stored here
		/// on the first instance creation of a IML item.
		/// </summary>
		public Dictionary<String, Instantiator> Instantiators;		
		/// <summary>
		/// default templates dic by metadata token
		/// </summary>
		public Dictionary<string, Instantiator> DefaultTemplates;
		/// <summary>
		/// Item templates stored with their index
		/// </summary>
		public Dictionary<String, ItemTemplate> ItemTemplates;

		public List<CrowThread> CrowThreads = new List<CrowThread>();//used to monitor thread finished

		public bool DragAndDropInProgress => DragAndDropOperation != null;
		public Widget DropTarget => DragAndDropOperation?.DropTarget;

		public DragDropEventArgs DragAndDropOperation = null;
		internal Widget dragndropHover;

		public Surface DragImage = null;
		public Rectangle DragImageBounds;
		public bool DragImageFolowMouse;//prevent dragImg to be moved by mouse
		public void ClearDragImage () {
			lock (UpdateMutex) {
				if (DragImage == null)
					return;
				clipping.UnionRectangle (DragImageBounds);				
				DragImage.Dispose();
				DragImage = null;
				DragImageBounds = default;
			}
		}
		public void CreateDragImage (Surface img, Rectangle bounds, bool followMouse = true) {
			lock (UpdateMutex) {
				if (DragImage != null)
					ClearDragImage ();
				DragImage = img;
				DragImageBounds = bounds;
				DragImageFolowMouse = followMouse;
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
		public Dictionary<String, LoaderInvoker> DefaultValuesLoader;
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
		public Dictionary<string, string> StylingConstants;
		/// <summary> parse all styling data's during application startup and build global Styling Dictionary </summary>
		protected virtual void loadStyling() {
			//fetch styling info in this order, if member styling is alreadey referenced in previous
			//assembly, it's ignored.
			loadThemeStyle ();

			loadStylingFromAssembly (Assembly.GetExecutingAssembly ());
			foreach (Assembly a in crowAssemblies) {
				loadStylingFromAssembly (a);
			}
			loadStylingFromAssembly (Assembly.GetEntryAssembly ());
		}
		/// <summary> Search for .style resources in assembly </summary>
		protected void loadStylingFromAssembly (Assembly assembly) {
			if (assembly == null)
				return;
			foreach (string s in assembly
				.GetManifestResourceNames ()
				.Where (r => r.EndsWith (".style", StringComparison.OrdinalIgnoreCase))) {
				using (StyleReader sr = new StyleReader (assembly.GetManifestResourceStream (s))) 
					sr.Parse (StylingConstants, Styling, s);
			}
		}
		public void LoadStyle (string stylePath) {
			using (Stream s = new FileStream (stylePath, FileMode.Open))
				LoadStyle (s, stylePath);

		}
		public void LoadStyle (Stream stream, string resId) {
			using (StyleReader sr = new StyleReader (stream))
				sr.Parse (StylingConstants, Styling, resId);
		}
		#endregion

		#region Theming
		string theme;
		public string Theme {
			get => theme;
			set {
				if (theme == value)
					return;
				theme = value;
				NotifyValueChanged (theme);

				if (StylingConstants == null)//iFace not yet initialized
					return;
				lock (UpdateMutex) {
					DbgLogger.StartEvent (DbgEvtType.IFaceReloadTheme);
					disposeContextMenus ();
					initDictionaries ();
					loadStyling ();
					loadThemeFiles ();					
					initContextMenus ();
					DbgLogger.EndEvent (DbgEvtType.IFaceReloadTheme);
				}
			}
		}
		protected void initDictionaries () {
			const int initCapacity = 20;
			StylingConstants = new Dictionary<string, string> (initCapacity);
			Styling = new Dictionary<string, Style> (initCapacity);
			DefaultValuesLoader = new Dictionary<string, LoaderInvoker> (initCapacity);
			Instantiators = new Dictionary<string, Instantiator> (initCapacity);
			DefaultTemplates = new Dictionary<string, Instantiator> (initCapacity);			
			ItemTemplates = new Dictionary<string, ItemTemplate> (initCapacity);
		}
		void loadThemeFiles () {
			if (string.IsNullOrEmpty (Theme))
				return;
			try {
				if (Directory.Exists (theme)) {
					string path = Path.Combine (Theme, "Images");
					if (Directory.Exists (path)) {
						foreach (string pic in Directory.GetFiles (path, "*.*", SearchOption.AllDirectories)) {
							string resId = $"#{pic.Substring (path.Length + 1).Replace (Path.DirectorySeparatorChar, '.')}";
							using (Stream s = new FileStream (pic, FileMode.Open, FileAccess.Read)) {
								if (resId.EndsWith (".svg", StringComparison.OrdinalIgnoreCase))
									sharedPictures[resId] = SvgPicture.CreateSharedPicture (s);
								else
									sharedPictures[resId] = BmpPicture.CreateSharedPicture (s);
							}
						}
					}
					if (Directory.Exists (path)) {
						path = Path.Combine (Theme, "DefaultTemplates");
						foreach (string iml in Directory.GetFiles (path, "*.*", SearchOption.AllDirectories)) {
							string resId = $"#{iml.Substring (path.Length + 1).Replace (Path.DirectorySeparatorChar, '.')}";
							//int mdTok = Instantiator.tryGetGOType (resId.Substring (6, resId.Length - 15)).MetadataToken;
							using (Stream s = new FileStream (iml, FileMode.Open, FileAccess.Read))
								DefaultTemplates[resId] = new IML.Instantiator (this, s, resId);
						}
					}
					path = Path.Combine (Theme, "IML");
					if (Directory.Exists (path)) {
						foreach (string iml in Directory.GetFiles (path, "*.*", SearchOption.AllDirectories)) {
							string resId = $"#{iml.Substring (path.Length + 1).Replace (Path.DirectorySeparatorChar, '.')}";
							using (Stream s = new FileStream (iml, FileMode.Open, FileAccess.Read))
								Instantiators[path] = new Instantiator (this, s, path);
						}
					}
					path = Path.Combine (Theme, "ItemTemplates");
					if (Directory.Exists (path)) {
						foreach (string iml in Directory.GetFiles (path, "*.*", SearchOption.AllDirectories)) {
							string resId = $"#{iml.Substring (path.Length + 1).Replace (Path.DirectorySeparatorChar, '.')}";
							using (Stream s = new FileStream (iml, FileMode.Open, FileAccess.Read))
								ItemTemplates[path] = new ItemTemplate (this, s, path);
						}
					}
					return;
				}
				using (ZipArchive archive = ZipFile.Open (Theme, ZipArchiveMode.Read)) {
					foreach (ZipArchiveEntry entry in archive.Entries.Where (e => e.FullName.StartsWith ("Images"))) {
						Console.WriteLine (entry.FullName);
                    }
					foreach (ZipArchiveEntry entry in archive.Entries.Where (e => e.FullName.StartsWith ("IML"))) {
						Console.WriteLine (entry.FullName);
					}
				}
			} catch (Exception e) {
				throw new Exception ($"[Theme] Error reading theme ({Theme})", e);
			}
		}
		void loadThemeStyle () {
			if (string.IsNullOrEmpty (Theme))
				return;
            try {
				if (Directory.Exists (theme)) {
					string stylePath = Directory.GetFiles (theme, "*.style").FirstOrDefault ();
					using (Stream s = new FileStream (stylePath, FileMode.Open, FileAccess.Read)) {
						using (StyleReader sr = new StyleReader (s))
							sr.Parse (StylingConstants, Styling, stylePath);
					}
					return;
				}
				using (ZipArchive archive = ZipFile.Open (Theme, ZipArchiveMode.Read)) {
					ZipArchiveEntry zipStyle = archive.Entries.FirstOrDefault (e => e.FullName.EndsWith (".style", StringComparison.OrdinalIgnoreCase));
					if (zipStyle != null) {
						using (Stream s = zipStyle.Open ()) {
							using (StyleReader sr = new StyleReader (s))
								sr.Parse (StylingConstants, Styling, zipStyle.FullName);
						}
					}
				}
			} catch (Exception e) {
				throw new Exception ($"[Theme] Error reading theme style ({Theme})", e);
            }
		}
		#endregion

		#region Load/Save
		/// <summary>
		/// share a single store for picture resources among usage in different controls
		/// </summary>
		internal Dictionary<string, sharedPicture> sharedPictures = new Dictionary<string, sharedPicture> ();

		static bool tryGetResource (Assembly a, string resId, out Stream stream) {
			stream = null;
			if (a == null)
				return false;
			stream = a.GetManifestResourceStream (resId);
			return stream != null;
		}


		public virtual Stream GetStreamFromPath (string path)
		{
			if (path.StartsWith ("#", StringComparison.Ordinal)) {
				Stream stream = null;
				string resId = path.Substring (1);
				if (tryGetResource (Assembly.GetEntryAssembly (), resId, out stream))
					return stream;
				string[] assemblyNames = resId.Split ('.');
				if (tryGetResource (AppDomain.CurrentDomain.GetAssemblies ()
					.FirstOrDefault (aa => aa.GetName ().Name == assemblyNames[0]), resId, out stream))
					return stream;
				if (assemblyNames.Length > 3)
					if (tryGetResource (AppDomain.CurrentDomain.GetAssemblies ()
						.FirstOrDefault (aa => aa.GetName ().Name == $"{assemblyNames[0]}.{assemblyNames[1]}"), resId, out stream))
						return stream;
				foreach (Assembly ca in crowAssemblies) 
					if (tryGetResource (ca, resId, out stream))
						return stream;
				throw new Exception ("Resource not found: " + path);
			} 
			if (!File.Exists (path))
				throw new FileNotFoundException ($"File not found: {path}", path);
			return new FileStream (path, FileMode.Open, FileAccess.Read);
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
				DbgLogger.StartEvent (DbgEvtType.IFaceLoad);

				Widget tmp = CreateInstance (path);
				AddWidget (tmp);

				DbgLogger.EndEvent (DbgEvtType.IFaceLoad);
				return tmp;
			}
		}
		/// <summary>
		/// Create an instance of a GraphicObject linked to this interface but not added to the GraphicTree
		/// </summary>
		/// <returns>new instance of graphic object created</returns>
		/// <param name="path">path of the iml file to load</param>
		public virtual Widget CreateInstance (string path)
			=> GetInstantiator (path).CreateInstance ();
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
		public virtual ItemTemplate GetItemTemplate(string path){
			if (!ItemTemplates.ContainsKey(path))
				ItemTemplates [path] = new ItemTemplate(this, path);
			return ItemTemplates [path] as ItemTemplate;
		}
		#endregion

		#region focus
		Widget _activeWidget;	//button is pressed on widget
		Widget _hoverWidget;		//mouse is over
		internal Widget _focusedWidget;	//has keyboard (or other perif) focus

		/// <summary>Widget is focused and button is down or another perif action is occuring
		/// , it can not lose focus while Active</summary>
		public Widget ActiveWidget
		{
			get => _activeWidget;
			internal set
			{
				if (_activeWidget == value)
					return;

				if (_activeWidget != null) {
					debugRegisterClip (_activeWidget);
					_activeWidget.IsActive = false;
				}

				_activeWidget = value;

				NotifyValueChanged ("ActiveWidget", _activeWidget);
				DbgLogger.AddEvent (DbgEvtType.ActiveWidget, _activeWidget);

				if (_activeWidget != null)
					_activeWidget.IsActive = true;
			}
		}
		/// <summary>Pointer is over the widget</summary>
		public virtual Widget HoverWidget
		{
			get => _hoverWidget;
			set {
				if (_hoverWidget == value)
					return;

				if (_hoverWidget != null) {
					debugRegisterClip (_hoverWidget);
					_hoverWidget.IsHover = false;
				}

				_hoverWidget = value;

				NotifyValueChanged ("HoverWidget", _hoverWidget);
				DbgLogger.AddEvent (DbgEvtType.HoverWidget, _hoverWidget);

				if (FOCUS_ON_HOVER) {
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
					_hoverWidget.IsHover = true;
			}
		}
		/// <summary>Widget has the keyboard or mouse focus</summary>
		public Widget FocusedWidget {
			get => _focusedWidget;
			set {
				if (_focusedWidget == value)
					return;
				if (_focusedWidget != null) {
					debugRegisterClip (_focusedWidget);
					_focusedWidget.HasFocus = false;
				}
				_focusedWidget = value;

				NotifyValueChanged ("FocusedWidget", _focusedWidget);				
				if (_focusedWidget != null)
					_focusedWidget.HasFocus = true;
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
				DbgLogger.AddEvent (DbgEvtType.GOEnqueueForRepaint, g);
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

			if (lastMouseDownEvent != null) {
				if (mouseRepeatTimer.ElapsedMilliseconds > DEVICE_REPEAT_INTERVAL) {
					if (_hoverWidget != null && _hoverWidget.MouseRepeat) {
						_hoverWidget.onMouseDown (_hoverWidget, lastMouseDownEvent);
						mouseRepeatTimer.Restart ();
					}
				} else if (lastMouseDown.ElapsedMilliseconds > DEVICE_REPEAT_DELAY)
					mouseRepeatTimer.Start ();
			}

			if (!Monitor.TryEnter (UpdateMutex))
				return;
			
			DbgLogger.StartEvent (DbgEvtType.Update);
			PerformanceMeasure.Begin (PerformanceMeasure.Kind.Update);

			processLayouting ();

			clippingRegistration ();

			if (ctx == null) {
				using (ctx = new Context (surf)) 
					processDrawing (ctx);
			}else
				processDrawing (ctx);

			PerformanceMeasure.End (PerformanceMeasure.Kind.Update);
			DbgLogger.EndEvent (DbgEvtType.Update, true);

			Monitor.Exit (UpdateMutex);

			PerformanceMeasure.Notify ();
		}
		/// <summary>Layouting loop, this is the first step of the udpate and process registered
		/// Layouting queue items. Failing LQI's are requeued in this cycle until MaxTry is reached which
		/// trigger an enqueue for the next Update Cycle</summary>
		protected virtual void processLayouting(){
			if (Monitor.TryEnter (LayoutMutex)) {
				if (LayoutingQueue.Count == 0) {
					Monitor.Exit (LayoutMutex);
					return;
                }
				DbgLogger.StartEvent (DbgEvtType.ProcessLayouting);
				PerformanceMeasure.Begin (PerformanceMeasure.Kind.Layouting);

				DiscardQueue = new Queue<LayoutingQueueItem> (LayoutingQueue.Count);
				//Debug.WriteLine ("======= Layouting queue start =======");
				 				
				while (LayoutingQueue.Count > 0) {
					LayoutingQueueItem lqi = LayoutingQueue.Dequeue ();
					lqi.ProcessLayouting ();
				}
				LayoutingQueue = DiscardQueue;

				PerformanceMeasure.End (PerformanceMeasure.Kind.Layouting);
				DbgLogger.EndEvent (DbgEvtType.ProcessLayouting, true);

				Monitor.Exit (LayoutMutex);
				DiscardQueue = null;
			}
		}
		/// <summary>Degueue Widget to clip from DrawingQueue and register the last painted slot and the new one
		/// Clipping rectangles are added at each level of the tree from leef to root, that's the way for the painting
		/// operation to known if it should go down in the tree for further graphic updates and repaints</summary>
		void clippingRegistration(){
			if (ClippingQueue.Count == 0)
				return;

			DbgLogger.StartEvent (DbgEvtType.ClippingRegistration);
			PerformanceMeasure.Begin (PerformanceMeasure.Kind.Clipping);

			Widget g = null;
			while (ClippingQueue.Count > 0) {
				lock (ClippingMutex) {
					g = ClippingQueue.Dequeue ();
					g.IsQueueForClipping = false;
				}
				g.ClippingRegistration ();
			}

			PerformanceMeasure.End (PerformanceMeasure.Kind.Clipping);
			DbgLogger.EndEvent (DbgEvtType.ClippingRegistration, true);
		}
		/// <summary>Clipping Rectangles drive the drawing process. For compositing, each object under a clip rectangle should be
		/// repainted. If it contains also clip rectangles, its cache will be update, or if not cached a full redraw will take place</summary>
		protected virtual void processDrawing(Context ctx){

			DbgLogger.StartEvent (DbgEvtType.ProcessDrawing);

			if (DragImage != null)
				clipping.UnionRectangle(DragImageBounds);

			if (!clipping.IsEmpty) {
				PerformanceMeasure.Begin (PerformanceMeasure.Kind.Drawing);				

				ctx.PushGroup ();

				for (int i = 0; i < clipping.NumRectangles; i++)
					ctx.Rectangle (clipping.GetRectangle (i));

				ctx.ClipPreserve ();
				ctx.Operator = Operator.Clear;
				ctx.Fill ();
				ctx.Operator = Operator.Over;				

				for (int i = GraphicTree.Count -1; i >= 0 ; i--){
					Widget p = GraphicTree[i];
					if (!p.IsVisible)
						continue;
					if (clipping.Contains (p.Slot) == RegionOverlap.Out)
						continue;

					ctx.Save ();
					p.Paint (ctx);
					ctx.Restore ();
				}

				if (DragAndDropOperation != null) {
					if (DragImage != null) {
						DirtyRect += DragImageBounds;
						if (DragImageFolowMouse) {
							DragImageBounds.X = MousePosition.X - DragImageBounds.Width / 2;
							DragImageBounds.Y = MousePosition.Y - DragImageBounds.Height / 2;
						}
						ctx.Save ();
						ctx.ResetClip ();
						ctx.SetSource (DragImage, DragImageBounds.X, DragImageBounds.Y);
						ctx.PaintWithAlpha (0.8);
						ctx.Restore ();
						DirtyRect += DragImageBounds;
						IsDirty = true;
					}
				}

#if DEBUG_CLIP_RECTANGLE
				ctx.LineWidth = 1;
				ctx.SetSource(1,0,0,0.5);
				for (int i = 0; i < clipping.NumRectangles; i++)
					ctx.Rectangle(clipping.GetRectangle(i));
				ctx.Stroke ();
#endif

				ctx.PopGroupToSource ();

				ctx.Paint ();
					
				surf.Flush ();

				clipping.Dispose ();
				clipping = new Region ();

				PerformanceMeasure.End (PerformanceMeasure.Kind.Drawing);
				IsDirty = true;
			}

			drawTextCursor (ctx);

			debugHighlightFocus (ctx);
			
			DbgLogger.EndEvent (DbgEvtType.ProcessDrawing, true);
		}
		#endregion

		[Conditional ("DEBUG_HIGHLIGHT_FOCUS")]
		internal void debugRegisterClip (Widget w) {
			RegisterClip (w.ScreenCoordinates (w.Slot));
		}
		[Conditional ("DEBUG_HIGHLIGHT_FOCUS")]
		void debugHighlightFocus (Context ctx) {
			if (HoverWidget!= null) {
				ctx.SetSource (Colors.Purple);
				ctx.Rectangle (HoverWidget.ScreenCoordinates (HoverWidget.Slot), 1);
			}
			if (FocusedWidget != null) {
				ctx.SetSource (Colors.Blue);
				ctx.Rectangle (FocusedWidget.ScreenCoordinates (FocusedWidget.Slot), 1);
			}
			if (ActiveWidget != null) {
				ctx.SetSource (Colors.Yellow);
				ctx.Rectangle (ActiveWidget.ScreenCoordinates (ActiveWidget.Slot), 1);
			}
			/*if (DragAndDropInProgress) {

            }*/
			surf.Flush ();
		}

		#region Blinking text cursor
		/// <summary>
		/// Text cursor blinking frequency.
		/// </summary>
		public static long TEXT_CURSOR_BLINK_FREQUENCY = 400;
		internal Rectangle? textCursor = null;//last printed cursor, used to clear it.
		public bool forceTextCursor = true;//when true, cursor is printed even if blinkingCursor.elapsed is not reached.
		Stopwatch blinkingCursor = Stopwatch.StartNew ();
		void drawTextCursor (Context ctx) {
			if (forceTextCursor) {
				if (FocusedWidget is IEditableTextWidget lab) {
					if (lab.DrawCursor (ctx, out Rectangle c)) {
						if (textCursor != null && c != textCursor.Value)
							RegisterClip (textCursor.Value);
						textCursor = c;
						surf.Flush ();
					} else if (textCursor != null)
						RegisterClip (textCursor.Value);
				}
				blinkingCursor.Restart ();
				forceTextCursor = false;
			} else if (textCursor != null && blinkingCursor.ElapsedMilliseconds > TEXT_CURSOR_BLINK_FREQUENCY) {
				RegisterClip (textCursor.Value);
				textCursor = null;
				blinkingCursor.Restart ();
			} else if (FocusedWidget is IEditableTextWidget lab) {
				if (blinkingCursor.ElapsedMilliseconds > TEXT_CURSOR_BLINK_FREQUENCY) {
					if (lab.DrawCursor (ctx, out Rectangle c)) {
						textCursor = c;
						surf.Flush ();
						blinkingCursor.Restart ();
					}
				}
			}
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

				switch (Environment.OSVersion.Platform) {
				case PlatformID.MacOSX:
					break;
				case PlatformID.Unix:
					surf.SetSize (clientRectangle.Width, clientRectangle.Height);
					break;
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
					if (ownWindow) {
						surf.Dispose ();
						IntPtr hWin32 = Glfw3.GetWin32Window (hWin);
						IntPtr hdc = Glfw3.GetWin32DC (hWin32);
						surf = new Win32Surface (hdc);
					}else
						surf.SetSize (clientRectangle.Width, clientRectangle.Height);
					break;
				case PlatformID.Xbox:
				case PlatformID.WinCE:
					throw new PlatformNotSupportedException ("Unable to create cairo surface.");
				}

				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}
		}

		#region Mouse and Keyboard Handling
		MouseCursor cursor = MouseCursor.top_left_arrow;
		[Obsolete]void loadCursors ()
		{
			const int minimumSize = 24;

			if (XCursor.Cursors.ContainsKey (MouseCursor.arrow))
				return;
			//Load cursors
			XCursor.Cursors [MouseCursor.arrow] = XCursorFile.Load (this, "#Crow.Cursors.arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.base_arrow_down] = XCursorFile.Load (this, "#Crow.Cursors.base_arrow_down").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.base_arrow_up] = XCursorFile.Load (this, "#Crow.Cursors.base_arrow_up").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.boat] = XCursorFile.Load (this, "#Crow.Cursors.boat").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_left_corner] = XCursorFile.Load (this, "#Crow.Cursors.bottom_left_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_right_corner] = XCursorFile.Load (this, "#Crow.Cursors.bottom_right_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_side] = XCursorFile.Load (this, "#Crow.Cursors.bottom_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.bottom_tee] = XCursorFile.Load (this, "#Crow.Cursors.bottom_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.center_ptr] = XCursorFile.Load (this, "#Crow.Cursors.center_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.circle] = XCursorFile.Load (this, "#Crow.Cursors.circle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.cross] = XCursorFile.Load (this, "#Crow.Cursors.cross").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.cross_reverse] = XCursorFile.Load (this, "#Crow.Cursors.cross_reverse").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.crosshair] = XCursorFile.Load (this, "#Crow.Cursors.crosshair").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.dot] = XCursorFile.Load (this, "#Crow.Cursors.dot").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.dot_box_mask] = XCursorFile.Load (this, "#Crow.Cursors.dot_box_mask").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.double_arrow] = XCursorFile.Load (this, "#Crow.Cursors.double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draft_large] = XCursorFile.Load (this, "#Crow.Cursors.draft_large").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draft_small] = XCursorFile.Load (this, "#Crow.Cursors.draft_small").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.draped_box] = XCursorFile.Load (this, "#Crow.Cursors.draped_box").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.exchange] = XCursorFile.Load (this, "#Crow.Cursors.exchange").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.fleur] = XCursorFile.Load (this, "#Crow.Cursors.fleur").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.gumby] = XCursorFile.Load (this, "#Crow.Cursors.gumby").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand] = XCursorFile.Load (this, "#Crow.Cursors.hand").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand1] = XCursorFile.Load (this, "#Crow.Cursors.hand1").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.hand2] = XCursorFile.Load (this, "#Crow.Cursors.hand2").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.help] = XCursorFile.Load (this, "#Crow.Cursors.help").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ibeam] = XCursorFile.Load (this, "#Crow.Cursors.ibeam").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_ptr] = XCursorFile.Load (this, "#Crow.Cursors.left_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_ptr_watch] = XCursorFile.Load (this, "#Crow.Cursors.left_ptr_watch").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_side] = XCursorFile.Load (this, "#Crow.Cursors.left_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.left_tee] = XCursorFile.Load (this, "#Crow.Cursors.left_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ll_angle] = XCursorFile.Load (this, "#Crow.Cursors.ll_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.lr_angle] = XCursorFile.Load (this, "#Crow.Cursors.lr_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.move] = XCursorFile.Load (this, "#Crow.Cursors.move").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.pencil] = XCursorFile.Load (this, "#Crow.Cursors.pencil").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.pirate] = XCursorFile.Load (this, "#Crow.Cursors.pirate").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.plus] = XCursorFile.Load (this, "#Crow.Cursors.plus").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.question_arrow] = XCursorFile.Load (this, "#Crow.Cursors.question_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_ptr] = XCursorFile.Load (this, "#Crow.Cursors.right_ptr").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_side] = XCursorFile.Load (this, "#Crow.Cursors.right_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.right_tee] = XCursorFile.Load (this, "#Crow.Cursors.right_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sailboat] = XCursorFile.Load (this, "#Crow.Cursors.sailboat").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_down_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_down_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_h_double_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_h_double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_left_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_left_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_right_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_right_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_up_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_up_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sb_v_double_arrow] = XCursorFile.Load (this, "#Crow.Cursors.sb_v_double_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.shuttle] = XCursorFile.Load (this, "#Crow.Cursors.shuttle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.sizing] = XCursorFile.Load (this, "#Crow.Cursors.sizing").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.target] = XCursorFile.Load (this, "#Crow.Cursors.target").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.tcross] = XCursorFile.Load (this, "#Crow.Cursors.tcross").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_left_arrow] = XCursorFile.Load (this, "#Crow.Cursors.top_left_arrow").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_left_corner] = XCursorFile.Load (this, "#Crow.Cursors.top_left_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_right_corner] = XCursorFile.Load (this, "#Crow.Cursors.top_right_corner").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_side] = XCursorFile.Load (this, "#Crow.Cursors.top_side").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.top_tee] = XCursorFile.Load (this, "#Crow.Cursors.top_tee").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.trek] = XCursorFile.Load (this, "#Crow.Cursors.trek").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ul_angle] = XCursorFile.Load (this, "#Crow.Cursors.ul_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.ur_angle] = XCursorFile.Load (this, "#Crow.Cursors.ur_angle").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.watch] = XCursorFile.Load (this, "#Crow.Cursors.watch").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.X_cursor] = XCursorFile.Load (this, "#Crow.Cursors.X_cursor").Cursors.First (c => c.Width >= minimumSize);
			XCursor.Cursors [MouseCursor.xterm] = XCursorFile.Load (this, "#Crow.Cursors.xterm").Cursors.First (c => c.Width >= minimumSize);
		}

		Stopwatch lastMouseDown = Stopwatch.StartNew (), mouseRepeatTimer = new Stopwatch ();
		bool doubleClickTriggered;	//next mouse up will trigger a double click
		MouseButtonEventArgs lastMouseDownEvent;

		public Point MousePosition { get; set; } = default;
		public bool IsDown (MouseButton button) => Glfw3.GetMouseButton (hWin, button) != InputAction.Release;

		public MouseCursor MouseCursor {
			get => cursor;
			set {
			
				if (value == cursor)
					return;
				cursor = value;

				currentCursor?.Dispose ();
				switch (cursor) {
                case MouseCursor.arrow:
				case MouseCursor.top_left_arrow:
					currentCursor = new Cursor (CursorShape.Arrow);
					break;
                case MouseCursor.crosshair:
					currentCursor = new Cursor (CursorShape.Crosshair);
					break;
                case MouseCursor.hand:
					currentCursor = new Cursor (CursorShape.Hand);
					break;
                case MouseCursor.ibeam:
					currentCursor = new Cursor (CursorShape.IBeam);
					break;
                default:
					currentCursor = XCursor.Create (this, cursor);
					break;
                }                                
				
				currentCursor.Set (hWin);
				//MouseCursorChanged.Raise (this,new MouseCursorChangedEventArgs(cursor));
			}
		}
		
		Point stickyMouseDelta = default;
		internal Widget stickedWidget = null;

		Widget HoverOrDropTarget {
			get => DragAndDropInProgress ? dragndropHover : HoverWidget;
			set {
				if (DragAndDropInProgress) {
					dragndropHover = value;
				} else
					HoverWidget = value;
            }
        }
		public virtual void ForceMousePosition () {
			Glfw3.SetCursorPosition (hWin, MousePosition.X, MousePosition.Y);
		}

		/// <summary>Processes mouse move events from the root container, this function
		/// should be called by the host on mouse move event to forward events to crow interfaces</summary>
		/// <returns>true if mouse is in the interface</returns>
		public virtual bool OnMouseMove (int x, int y)
		{
			DbgLogger.StartEvent (DbgEvtType.MouseMove);

			int deltaX = x - MousePosition.X;
			int deltaY = y - MousePosition.Y;

			if (!DragAndDropInProgress) {
				if (stickedWidget != null && ActiveWidget == null) {
					stickyMouseDelta.X += deltaX;
					stickyMouseDelta.Y += deltaY;

					if (Math.Abs (stickyMouseDelta.X) > stickedWidget.StickyMouse || Math.Abs (stickyMouseDelta.Y) > stickedWidget.StickyMouse) {
						stickedWidget = null;
						stickyMouseDelta = default;
					} else {
						ForceMousePosition ();
						DbgLogger.EndEvent (DbgEvtType.MouseMove);
						return true;
					}
				}
			}

			MousePosition = new Point (x, y);
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);

			if (!(DragAndDropInProgress || ActiveWidget == null)) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				ActiveWidget.onMouseMove (this, e);
				DbgLogger.EndEvent (DbgEvtType.MouseMove);
				return true;
			}

			if (HoverOrDropTarget != null) {
				resetTooltip ();

				//check topmost graphicobject first				
				Widget topContainer = HoverOrDropTarget;
				while (topContainer.LogicalParent is Widget w)
					topContainer = w;					
				
				int indexOfTopContainer = GraphicTree.IndexOf (topContainer);
				if (indexOfTopContainer != 0) {
                    for (int i = 0; i < indexOfTopContainer; i++) {
						//if logical parent of top container is a Interface, that's not a popup.
						if (GraphicTree [i].LogicalParent is Interface) {
							if (GraphicTree [i].MouseIsIn (e.Position)) {
								//mouse is in another top container than the actual one,
								//so we must leave first the current top container starting from HoverWidget
								if (DragAndDropInProgress) {
									DragAndDropOperation.DropTarget?.onDragLeave (this, DragAndDropOperation);
									GraphicTree[i].checkHoverWidget (e);
									DragAndDropOperation.DragSource.onDrag (this, e);
								} else {
									while (HoverWidget != null) {
										HoverWidget.onMouseLeave (this, e);
										HoverWidget = HoverWidget.FocusParent;
									}
									GraphicTree[i].checkHoverWidget (e);
									HoverWidget.onMouseMove (this, e);
								}
								DbgLogger.EndEvent (DbgEvtType.MouseMove);
								return true;
							}
						}
					}
				}

				if (HoverOrDropTarget.MouseIsIn (e.Position)) {
					HoverOrDropTarget.checkHoverWidget (e);
					if (DragAndDropInProgress)
						DragAndDropOperation.DragSource.onDrag (this, e);
					else
						HoverWidget.onMouseMove (this, e);
					DbgLogger.EndEvent (DbgEvtType.MouseMove);
					return true;
				} else {
					if (DragAndDropInProgress && dragndropHover == DragAndDropOperation.DropTarget)
						DragAndDropOperation.DropTarget.onDragLeave (this, DragAndDropOperation);
					//seek upward from last focused graph obj's	
					while (HoverOrDropTarget.FocusParent != null) {
						if (!DragAndDropInProgress)
							HoverWidget.onMouseLeave (this, e);
						HoverOrDropTarget = HoverOrDropTarget.FocusParent;
						if (HoverOrDropTarget.MouseIsIn (e.Position)) {
							HoverOrDropTarget.checkHoverWidget (e);
							if (DragAndDropInProgress)
								DragAndDropOperation.DragSource?.onDrag (this, e);
							else
								HoverWidget.onMouseMove (this, e);
							DbgLogger.EndEvent (DbgEvtType.MouseMove);
							return true;
						}						
					}
				}
			}

			//top level graphic obj's parsing
			lock (GraphicTree) {
				for (int i = 0; i < GraphicTree.Count; i++) {
					Widget g = GraphicTree [i];
					if (DragAndDropInProgress && DragAndDropOperation.DragSource == g)
						continue;
					if (g.MouseIsIn (e.Position)) {
						g.checkHoverWidget (e);
						if (!DragAndDropInProgress) {
							if (g is Window && FOCUS_ON_HOVER && g.Focusable) {
								FocusedWidget = g;
								if (RAISE_WIN_ON_FOCUS)
									PutOnTop (g);
							}
							HoverWidget.onMouseMove (this, e);
						}
						DbgLogger.EndEvent (DbgEvtType.MouseMove);
						return true;
					}
				}
			}
			HoverOrDropTarget = null;
			DbgLogger.EndEvent (DbgEvtType.MouseMove);
			return false;
		}
		/// <summary>
		/// Forward the mouse down event from the host to the hover widget in the crow interface
		/// </summary>
		/// <returns>return true, if interface handled the event, false otherwise.</returns>
		/// <param name="button">Button index</param>
		public virtual bool OnMouseButtonDown (MouseButton button)
		{
			DbgLogger.StartEvent (DbgEvtType.MouseDown);
			doubleClickTriggered = (lastMouseDown.ElapsedMilliseconds < DOUBLECLICK_TRESHOLD);
			lastMouseDown.Restart ();

			lastMouseDownEvent = new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Press);

			if (HoverWidget == null) {
				DbgLogger.EndEvent (DbgEvtType.MouseDown);
				return false;
			}

			HoverWidget.onMouseDown (this, lastMouseDownEvent);

			ActiveWidget = HoverWidget;
			DbgLogger.EndEvent (DbgEvtType.MouseDown);
			return true;
		}
		/// <summary>
		/// Forward the mouse up event from the host to the crow interface
		/// </summary>
		/// <returns>return true, if interface handled the event, false otherwise.</returns>
		/// <param name="button">Button index</param>
		public virtual bool OnMouseButtonUp (MouseButton button)
		{
			DbgLogger.StartEvent (DbgEvtType.MouseUp);
			mouseRepeatTimer.Reset ();
			lastMouseDownEvent = null;

			if (DragAndDropInProgress) {				
				if (DragAndDropOperation.DropTarget != null)
					DragAndDropOperation.DragSource.onDrop (this, DragAndDropOperation);
				else
					DragAndDropOperation.DragSource.onEndDrag (this, DragAndDropOperation);
				DragAndDropOperation = null;
				if (ActiveWidget != null) {
					ActiveWidget.onMouseUp (_activeWidget, new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Release));
					ActiveWidget = null;
				}
				DbgLogger.EndEvent (DbgEvtType.MouseUp);
				return true;
            }

			if (_activeWidget == null) {
				DbgLogger.EndEvent (DbgEvtType.MouseUp);
				return false;
			}

			_activeWidget.onMouseUp (_activeWidget, new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Release));

			if (_activeWidget == null) {
				Debug.WriteLine ("[BUG]Mystery reset of _activeWidget");
				DbgLogger.EndEvent (DbgEvtType.MouseUp | DbgEvtType.Error);
				return true;
			}

			if (doubleClickTriggered)
				_activeWidget.onMouseDoubleClick (_activeWidget, new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Press));
			else
				_activeWidget.onMouseClick (_activeWidget, new MouseButtonEventArgs (MousePosition.X, MousePosition.Y, button, InputAction.Press));

			ActiveWidget = null;
			//			if (!lastActive.MouseIsIn (Mouse.Position)) {
			//				ProcessMouseMove (Mouse.X, Mouse.Y);
			//			}
			DbgLogger.EndEvent (DbgEvtType.MouseUp);
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

		public bool IsKeyDown (Key key) => Glfw3.GetKey (hWin, key) == InputAction.Press;
		#endregion

		#region Tooltip handling
		Stopwatch tooltipTimer = new Stopwatch ();
		Widget ToolTipContainer;
		volatile bool tooltipVisible;

		protected void initTooltip ()
		{
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
								if (g.Tooltip.StartsWith("#", StringComparison.Ordinal)) {
									//custom tooltip container
									ToolTipContainer = CreateInstance (g.Tooltip);
								} else
									ToolTipContainer = CreateInstance ("#Crow.Tooltip.template");
								ToolTipContainer.LayoutChanged += ToolTipContainer_LayoutChanged;
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
				ToolTipContainer.LayoutChanged -= ToolTipContainer_LayoutChanged;
				ToolTipContainer.DataSource = null;
				RemoveWidget (ToolTipContainer);
				tooltipVisible = false;
				ToolTipContainer.Dispose ();
				ToolTipContainer = null;
			}
			tooltipTimer.Restart ();
		}
		void ToolTipContainer_LayoutChanged (object sender, LayoutingEventArgs e)
		{
			Widget ttc = sender as Widget;				
					
			//tooltip container datasource is the widget triggering the tooltip
			Rectangle r = ScreenCoordinates ((ttc.DataSource as Widget).Slot);

			if (e.LayoutType == LayoutingType.X) {
				if (ttc.Slot.Right > clientRectangle.Right)
					ttc.Left = clientRectangle.Right - ttc.Slot.Width;						
			}else if (e.LayoutType == LayoutingType.Y) {
				if (ttc.Slot.Bottom > clientRectangle.Bottom)
					ttc.Top = clientRectangle.Bottom - ttc.Slot.Height;
			}/*
				if (ttc.Slot.Height < tc.ClientRectangle.Height) {
					if (PopDirection.HasFlag (Alignment.Bottom)) {
						if (r.Bottom + ttc.Slot.Height > tc.ClientRectangle.Bottom)
							ttc.Top = r.Top - ttc.Slot.Height;
						else
							ttc.Top = r.Bottom;
					} else if (PopDirection.HasFlag (Alignment.Top)) {
						if (r.Top - ttc.Slot.Height < tc.ClientRectangle.Top)
							ttc.Top = r.Bottom;
						else
							ttc.Top = r.Top - ttc.Slot.Height;
					} else
						ttc.Top = r.Top;
				} else
					ttc.Top = 0;
			}*/

		}
		#endregion

		#region Contextual menu
		MenuItem ctxMenuContainer;
		protected void initContextMenus ()
		{
			ctxMenuContainer = CreateInstance ("#Crow.ContextMenu.template") as MenuItem;
			ctxMenuContainer.LayoutChanged += CtxMenuContainer_LayoutChanged;
			ctxMenuContainer.Focusable = true;
		}
		protected void disposeContextMenus () {
			if (ctxMenuContainer == null)
				return;
			ctxMenuContainer.LayoutChanged -= CtxMenuContainer_LayoutChanged;
			ctxMenuContainer.Dispose ();
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

				ctxMenuContainer.BubbleMouseEvent = false;
				ctxMenuContainer.LogicalParent = go;
				ctxMenuContainer.DataSource = go;
				

				PutOnTop (ctxMenuContainer, true);
			}
			ctxMenuContainer.Left = MousePosition.X - 5;
			ctxMenuContainer.Top = MousePosition.Y - 5;

			//OnMouseMove (MousePosition.X, MousePosition.Y);
			HoverWidget = ctxMenuContainer;
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
		public virtual bool PointIsIn (ref Point m) => true;
		public void RegisterClip (Rectangle r)
		{
			clipping.UnionRectangle (r);
		}
		public bool ArrangeChildren => false;
		public int LayoutingTries {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}
		public LayoutingType RegisteredLayoutings {
			get => LayoutingType.None;
			set { throw new NotImplementedException (); }
		}
		public void RegisterForLayouting (LayoutingType layoutType) { throw new NotImplementedException (); }
		public bool UpdateLayout (LayoutingType layoutType) { throw new NotImplementedException (); }
		public Rectangle ContextCoordinates (Rectangle r) => r;
		public Rectangle ScreenCoordinates (Rectangle r) => r;

		public ILayoutable Parent {
			get => null;
			set { throw new NotImplementedException (); }
		}
		public ILayoutable LogicalParent {
			get => null;
			set { throw new NotImplementedException (); }
		}

		public Rectangle ClientRectangle => clientRectangle; 
		public Interface HostContainer {
			get { return this; }
		}
		public Rectangle getSlot () => ClientRectangle;
		public void ChildrenLayoutingConstraints(ILayoutable layoutable, ref LayoutingType layoutType){	}
		#endregion
	}
}

