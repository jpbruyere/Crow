//
// Interface.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Cairo;
using System.Globalization;

namespace Crow
{
	/// <summary>
	/// The Interface Class is the top container of the application.
	/// It provides the Dirty bitmap and zone of the interface to be drawn on screen
	///
	/// The Interface contains :
	/// 	- rendering and layouting queues and logic.
	/// 	- helpers to load XML interfaces files
	/// 	- global constants and variables of CROW
	/// 	- Keyboard and Mouse logic
	/// 	- the resulting bitmap of the interface
	/// </summary>
	public class Interface : GraphicObject
	{
		#region CTOR
		static Interface(){
			loadCursors ();
			loadStyling ();
			findAvailableTemplates ();

			FontRenderingOptions = new FontOptions ();
			FontRenderingOptions.Antialias = Antialias.Subpixel;
			FontRenderingOptions.HintMetrics = HintMetrics.On;
			FontRenderingOptions.HintStyle = HintStyle.Medium;
			FontRenderingOptions.SubpixelOrder = SubpixelOrder.Rgb;
		}
		public Interface(){
			CurrentInterface = this;
			CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		}
		#endregion

		#region Static and constants
		/// <summary>If true, mouse focus is given when mouse is over control</summary>
		public static bool FocusOnHover = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;
		/// <summary>Double click threshold in milisecond</summary>
		public static int DoubleClick = 200;//max duration between two mouse_down evt for a dbl clk in milisec.
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DeviceRepeatDelay = 700;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DeviceRepeatInterval = 40;
		/// <summary>Tabulation size in Text controls</summary>
		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
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
		internal static Interface CurrentInterface;
		internal Stopwatch clickTimer = new Stopwatch();
		internal GraphicObject eligibleForDoubleClick = null;
		#endregion

		#region Events
		public event EventHandler<MouseCursorChangedEventArgs> MouseCursorChanged;
		public event EventHandler Quit;

		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		public event EventHandler<KeyboardKeyEventArgs> KeyboardKeyDown;
		public event EventHandler<KeyboardKeyEventArgs> KeyboardKeyUp;
		#endregion

		#region Public Fields
		/// <summary>Graphic Tree of this interface</summary>
		public List<GraphicObject> GraphicTree = new List<GraphicObject>();
		/// <summary>Interface's resulting bitmap</summary>
		public byte[] bmp;
		/// <summary>resulting bitmap limited to last redrawn part</summary>
		public byte[] dirtyBmp;
		/// <summary>True when host has to repaint Interface</summary>
		public bool IsDirty = false;
		/// <summary>Coordinate of the dirty bmp on the original bmp</summary>
		public Rectangle DirtyRect;
		/// <summary>Locked for each layouting operation</summary>
		public object LayoutMutex = new object();
		/// <summary>Sync mutex between host and Crow for rendering operations (bmp, dirtyBmp,...)</summary>
		public object RenderMutex = new object();
		/// <summary>Global lock of the update cycle</summary>
		public object UpdateMutex = new object();
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
		public Queue<GraphicObject> DrawingQueue = new Queue<GraphicObject>();
		public string Clipboard;//TODO:use object instead for complex copy paste
		/// <summary>each IML and fragments (such as inline Templates) are compiled as a Dynamic Method stored here
		/// on the first instance creation of a IML item.
		/// </summary>
		public static Dictionary<String, Instantiator> Instantiators = new Dictionary<string, Instantiator>();
		public List<CrowThread> CrowThreads = new List<CrowThread>();//used to monitor thread finished
		#endregion

		#region Private Fields
		/// <summary>Client rectangle in the host context</summary>
		Rectangle clientRectangle;
		/// <summary>Clipping rectangles on the root context</summary>
		Region clipping = new Region();
		/// <summary>Main Cairo context</summary>
		Context ctx;
		/// <summary>Main Cairo surface</summary>
		Surface surf;
		#endregion

		#region Default values and Style loading
		/// Default values of properties from GraphicObjects are retrieve from XML Attributes.
		/// The reflexion process used to retrieve those values being very slow, it is compiled in MSIL
		/// and injected as a dynamic method referenced in the DefaultValuesLoader Dictionnary.
		/// The compilation is done on the first object instancing, and is also done for custom widgets
		public delegate void LoaderInvoker(object instance);
		/// <summary>Store one loader per StyleKey</summary>
		public static Dictionary<String, LoaderInvoker> DefaultValuesLoader = new Dictionary<string, LoaderInvoker>();
		/// <summary>Store dictionnary of member/value per StyleKey</summary>
		public static Dictionary<string, Style> Styling;
		/// <summary> parse all styling data's during application startup and build global Styling Dictionary </summary>
		static void loadStyling() {
			Styling = new Dictionary<string, Style> ();

			//fetch styling info in this order, if member styling is alreadey referenced in previous
			//assembly, it's ignored.
			loadStylingFromAssembly (Assembly.GetEntryAssembly ());
			loadStylingFromAssembly (Assembly.GetExecutingAssembly ());
		}
		/// <summary> Search for .style resources in assembly </summary>
		static void loadStylingFromAssembly (Assembly assembly) {
			foreach (string s in assembly
				.GetManifestResourceNames ()
				.Where (r => r.EndsWith (".style", StringComparison.OrdinalIgnoreCase))) {
				new StyleReader (assembly, s)
					.Dispose ();
			}
		}
		static void loadCursors(){
			//Load cursors
			XCursor.Cross = XCursorFile.Load("#Crow.Images.Icons.Cursors.cross").Cursors[0];
			XCursor.Default = XCursorFile.Load("#Crow.Images.Icons.Cursors.arrow").Cursors[0];
			XCursor.NW = XCursorFile.Load("#Crow.Images.Icons.Cursors.top_left_corner").Cursors[0];
			XCursor.NE = XCursorFile.Load("#Crow.Images.Icons.Cursors.top_right_corner").Cursors[0];
			XCursor.SW = XCursorFile.Load("#Crow.Images.Icons.Cursors.bottom_left_corner").Cursors[0];
			XCursor.SE = XCursorFile.Load("#Crow.Images.Icons.Cursors.bottom_right_corner").Cursors[0];
			XCursor.H = XCursorFile.Load("#Crow.Images.Icons.Cursors.sb_h_double_arrow").Cursors[0];
			XCursor.V = XCursorFile.Load("#Crow.Images.Icons.Cursors.sb_v_double_arrow").Cursors[0];
			XCursor.Text = XCursorFile.Load("#Crow.Images.Icons.Cursors.ibeam").Cursors[0];
		}
		#endregion

		#region Templates
		/// <summary>Store one default templates resource ID per class.
		/// Resource ID must be 'fullClassName.template' (not case sensitive)
		/// Those found in application assembly have priority to the default Crow's one
		/// </summary>
		public static Dictionary<string, string> DefaultTemplates = new Dictionary<string, string>();
		/// <summary>Finds available default templates at startup</summary>
		static void findAvailableTemplates(){
			searchTemplatesIn (Assembly.GetEntryAssembly ());
			searchTemplatesIn (Assembly.GetExecutingAssembly ());
		}
		static void searchTemplatesIn(Assembly assembly){
			foreach (string resId in assembly
				.GetManifestResourceNames ()
				.Where (r => r.EndsWith (".template", StringComparison.OrdinalIgnoreCase))) {
				string clsName = resId.Substring (0, resId.Length - 9);
				if (DefaultTemplates.ContainsKey (clsName))
					continue;
				DefaultTemplates[clsName] = "#" + resId;
			}
		}
		#endregion

		#region Load/Save
		/// <summary>Open file or find a resource from path string</summary>
		/// <returns>A file or resource stream</returns>
		/// <param name="path">This could be a normal file path, or an embedded ressource ID
		/// Resource ID's must be prefixed with '#' character</param>
		public static Stream GetStreamFromPath (string path)
		{
			Stream stream = null;

			if (path.StartsWith ("#")) {
				string resId = path.Substring (1);
				//try/catch added to prevent nunit error
				try {
					stream = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceStream (resId);
				} catch{}
				if (stream == null)//try to find ressource in Crow assembly
					stream = System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream (resId);
				if (stream == null)
					throw new Exception ("Resource not found: " + path);
			} else {
				if (!File.Exists (path))
					throw new FileNotFoundException ("File not found: ", path);
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			}
			return stream;
		}

		/// <summary>Create an instance of a GraphicObject and add it to the GraphicTree
		/// of this Interface</summary>
		public GraphicObject LoadInterface (string path)
		{
			lock (UpdateMutex) {
				GraphicObject tmp = Load (path);
				AddWidget (tmp);

				return tmp;
			}
		}
		/// <summary>Create an instance of a GraphicObject linked to this interface but
		/// not added to the GraphicTree</summary>
		public GraphicObject Load (string path)
		{
			try {
				return GetInstantiator (path).CreateInstance (this);
			} catch (Exception ex) {
				throw new Exception ("Error loading <" + path + ">:", ex);
			}
		}
		/// <summary>Fetch it from cache or create it</summary>
		public static Instantiator GetInstantiator(string path){
			if (!Instantiators.ContainsKey(path))
				Instantiators [path] = new Instantiator(path);
			return Instantiators [path];
		}
		/// <summary>Item templates have additional properties for recursivity and
		/// custom display per item type</summary>
		public static ItemTemplate GetItemTemplate(string path){
			if (!Instantiators.ContainsKey(path))
				Instantiators [path] = new ItemTemplate(path);
			return Instantiators [path] as ItemTemplate;
		}
		//TODO: .Net xml serialisation is no longer used, it has been replaced with instantiators
		public static void Save<T> (string file, T graphicObject)
		{
			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");
			XmlSerializer xs = new XmlSerializer (typeof(T));

			xs = new XmlSerializer (typeof(T));
			using (Stream s = new FileStream (file, FileMode.Create)) {
				xs.Serialize (s, graphicObject, xn);
			}
		}
		#endregion

		#region focus
		GraphicObject _activeWidget;	//button is pressed on widget
		GraphicObject _hoverWidget;		//mouse is over
		GraphicObject _focusedWidget;	//has keyboard (or other perif) focus

		/// <summary>Widget is focused and button is down or another perif action is occuring
		/// , it can not lose focus while Active</summary>
		public GraphicObject ActiveWidget
		{
			get { return _activeWidget; }
			set
			{
				if (_activeWidget == value)
					return;

				if (_activeWidget != null)
					_activeWidget.IsActive = false;

				_activeWidget = value;

				if (_activeWidget != null)
				{
					_activeWidget.IsActive = true;
					#if DEBUG_FOCUS
					Debug.WriteLine("Active => " + _activeWidget.ToString());
				}else
					Debug.WriteLine("Active => null");
					#else
				}
					#endif
			}
		}
		/// <summary>Pointer is over the widget</summary>
		public GraphicObject HoverWidget
		{
			get { return _hoverWidget; }
			set {
				if (_hoverWidget == value)
					return;
				_hoverWidget = value;
				#if DEBUG_FOCUS
				if (_hoverWidget != null)
				Debug.WriteLine("Hover => " + _hoverWidget.ToString());
				else
				Debug.WriteLine("Hover => null");
				#endif
			}
		}
		/// <summary>Widget has the keyboard or mouse focus</summary>
		public GraphicObject FocusedWidget {
			get { return _focusedWidget; }
			set {
				if (_focusedWidget == value)
					return;
				if (_focusedWidget != null)
					_focusedWidget.HasFocus = false;
				_focusedWidget = value;
				if (_focusedWidget != null)
					_focusedWidget.HasFocus = true;
			}
		}
		#endregion


		#region UPDATE Loops
		/// <summary>Enqueue Graphic object for Repaint, DrawingQueue is locked because
		/// GraphObj's property Set methods could trigger an update from another thread</summary>
		public void EnqueueForRepaint(GraphicObject g)
		{
			lock (DrawingQueue) {
				if (g.IsQueueForRedraw)
					return;
				DrawingQueue.Enqueue (g);
				g.IsQueueForRedraw = true;
			}
		}
		/// <summary>Main Update loop, executed in this interface thread, lock the UpdateMutex
		/// Steps:
		/// 	- execute device Repeat events
		/// 	- Layouting
		/// 	- Clipping
		/// 	- Drawing
		/// Result: the Interface bitmap is drawn in memory (byte[] bmp) and a dirtyRect and bitmap are available
		/// </summary>
		public void Update(){
			if (mouseRepeatCount > 0) {
				int mc = mouseRepeatCount;
				mouseRepeatCount -= mc;
				if (_focusedWidget != null) {
					for (int i = 0; i < mc; i++) {
						_focusedWidget.onMouseClick (this, new MouseButtonEventArgs (Mouse.X, Mouse.Y, MouseButton.Left, true));
					}
				}
			}
			if (keyboardRepeatCount > 0) {
				int mc = keyboardRepeatCount;
				keyboardRepeatCount -= mc;
				if (_focusedWidget != null) {
					for (int i = 0; i < mc; i++) {
						_focusedWidget.onKeyDown (this, lastKeyDownEvt);
					}
				}
			}
			CrowThread[] tmpThreads;
			lock (CrowThreads) {
				tmpThreads = new CrowThread[CrowThreads.Count];
				Array.Copy (CrowThreads.ToArray (), tmpThreads, CrowThreads.Count);
			}
			for (int i = 0; i < tmpThreads.Length; i++)
				tmpThreads [i].CheckState ();

			if (!Monitor.TryEnter (UpdateMutex))
				return;

			#if MEASURE_TIME
			updateMeasure.StartCycle();
			#endif

			processLayouting ();

			#if DEBUG_LAYOUTING
			if (curLQIsTries.Count > 0){
				LQIsTries.Add(curLQIsTries);
				curLQIsTries = new LQIList();
				LQIs.Add(curLQIs);
				curLQIs = new LQIList();
			}
			#endif

			clippingRegistration ();

			processDrawing ();

			#if MEASURE_TIME
			updateMeasure.StopCycle();
			#endif

			Monitor.Exit (UpdateMutex);
		}
		/// <summary>Layouting loop, this is the first step of the udpate and process registered
		/// Layouting queue items. Failing LQI's are requeued in this cycle until MaxTry is reached which
		/// trigger an enqueue for the next Update Cycle</summary>
		void processLayouting(){
			#if MEASURE_TIME
			layoutingMeasure.StartCycle();
			#endif

			if (Monitor.TryEnter (LayoutMutex)) {
				DiscardQueue = new Queue<LayoutingQueueItem> ();
				//Debug.WriteLine ("======= Layouting queue start =======");
				LayoutingQueueItem lqi;
				while (LayoutingQueue.Count > 0) {
					lqi = LayoutingQueue.Dequeue ();
					#if DEBUG_LAYOUTING
					currentLQI = lqi;
					curLQIsTries.Add(currentLQI);
					#endif
					lqi.ProcessLayouting ();
				}
				LayoutingQueue = DiscardQueue;
				Monitor.Exit (LayoutMutex);
				DiscardQueue = null;
			}

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
			GraphicObject g = null;
			while (DrawingQueue.Count > 0) {
				lock (DrawingQueue)
					g = DrawingQueue.Dequeue ();
				lock (g)
					g.ClippingRegistration ();
			}

			#if MEASURE_TIME
			clippingMeasure.StopCycle();
			#endif
		}
		/// <summary>Clipping Rectangles drive the drawing process. For compositing, each object under a clip rectangle should be
		/// repainted. If it contains also clip rectangles, its cache will be update, or if not cached a full redraw will take place</summary>
		unsafe void processDrawing(){
			#if MEASURE_TIME
			drawingMeasure.StartCycle();
			#endif
			using (surf = new ImageSurface (bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height, ClientRectangle.Width * 4)) {
				using (ctx = new Context (surf)){
					if (!clipping.IsEmpty) {

						for (int i = 0; i < clipping.NumRectangles; i++)
							ctx.Rectangle(clipping.GetRectangle(i));
						ctx.ClipPreserve();
						ctx.Operator = Operator.Clear;
						ctx.Fill();
						ctx.Operator = Operator.Over;

						for (int i = GraphicTree.Count -1; i >= 0 ; i--){
							GraphicObject p = GraphicTree[i];
							if (!p.Visible)
								continue;
							if (clipping.Contains (p.nativeHnd->Slot) == RegionOverlap.Out)
								continue;

							ctx.Save ();
							p.Paint (ref ctx);
							ctx.Restore ();
						}

						#if DEBUG_CLIP_RECTANGLE
						clipping.stroke (ctx, Color.Red.AdjustAlpha(0.5));
						#endif
						lock (RenderMutex) {
//							Array.Copy (bmp, dirtyBmp, bmp.Length);

							IsDirty = true;
							if (IsDirty)
								DirtyRect += clipping.Extents;
							else
								DirtyRect = clipping.Extents;

							DirtyRect.Left = Math.Max (0, DirtyRect.Left);
							DirtyRect.Top = Math.Max (0, DirtyRect.Top);
							DirtyRect.Width = Math.Min (ClientRectangle.Width - DirtyRect.Left, DirtyRect.Width);
							DirtyRect.Height = Math.Min (ClientRectangle.Height - DirtyRect.Top, DirtyRect.Height);
							DirtyRect.Width = Math.Max (0, DirtyRect.Width);
							DirtyRect.Height = Math.Max (0, DirtyRect.Height);

							if (DirtyRect.Width > 0 && DirtyRect.Height >0) {
								dirtyBmp = new byte[4 * DirtyRect.Width * DirtyRect.Height];
								for (int y = 0; y < DirtyRect.Height; y++) {
									Array.Copy (bmp,
										((DirtyRect.Top + y) * ClientRectangle.Width * 4) + DirtyRect.Left * 4,
										dirtyBmp, y * DirtyRect.Width * 4, DirtyRect.Width * 4);
								}

							} else
								IsDirty = false;
						}
						clipping.Dispose ();
						clipping = new Region ();
					}
					//surf.WriteToPng (@"/mnt/data/test.png");
				}
			}
			#if MEASURE_TIME
			drawingMeasure.StopCycle();
			#endif
		}
		#endregion

		#region GraphicTree handling
		/// <summary>Add widget to the Graphic tree of this interface and register it for layouting</summary>
		public void AddWidget(GraphicObject g, bool isOverlay = false)
		{
			g.Parent = this;
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

			lock (UpdateMutex)
				GraphicTree.Insert (ptr, g);

			g.RegisteredLayoutings = LayoutingType.None;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
		/// <summary>Set visible state of widget to false and remove if from the graphic tree</summary>
		public void DeleteWidget(GraphicObject g)
		{
			if (_hoverWidget != null) {
				if (g.Contains (_hoverWidget))
					HoverWidget = null;
			}
			lock (UpdateMutex) {
				g.DataSource = null;
				g.Visible = false;
				GraphicTree.Remove (g);
			}
		}
		/// <summary> Put widget on top of other root widgets</summary>
		public void PutOnTop(GraphicObject g, bool isOverlay = false)
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
		/// <summary> Remove all Graphic objects from top container </summary>
		public void ClearInterface()
		{
			lock (UpdateMutex) {
				while (GraphicTree.Count > 0) {
					//TODO:parent is not reset to null because object will be added
					//to ObjectToRedraw list, and without parent, it fails
					GraphicObject g = GraphicTree [0];
					g.DataSource = null;
					g.Visible = false;
					GraphicTree.RemoveAt (0);
				}
			}
			#if DEBUG_LAYOUTING
			LQIsTries = new List<LQIList>();
			curLQIsTries = new LQIList();
			LQIs = new List<LQIList>();
			curLQIs = new LQIList();
			#endif
		}

		/// <summary>Search a Graphic object in the tree named 'nameToFind'</summary>
		public GraphicObject FindByName (string nameToFind)
		{
			foreach (GraphicObject w in GraphicTree) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		#endregion

		public void ProcessResize(Rectangle bounds){
			lock (UpdateMutex) {
				clientRectangle = bounds;
				int stride = 4 * ClientRectangle.Width;
				int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
				bmp = new byte[bmpSize];
				dirtyBmp = new byte[bmpSize];

				foreach (GraphicObject g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}
		}

		#region Mouse and Keyboard Handling
		XCursor cursor = XCursor.Default;

		public MouseState Mouse;
		public KeyboardState Keyboard;

		public XCursor MouseCursor {
			set {
				if (value == cursor)
					return;
				cursor = value;
				MouseCursorChanged.Raise (this,new MouseCursorChangedEventArgs(cursor));
			}
		}
		/// <summary>Processes mouse move events from the root container</summary>
		/// <returns><c>true</c>if mouse is in the interface</returns>
		public bool ProcessMouseMove(int x, int y)
		{
			int deltaX = x - Mouse.X;
			int deltaY = y - Mouse.Y;
			Mouse.X = x;
			Mouse.Y = y;
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);
			e.Mouse = Mouse;

			if (ActiveWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				ActiveWidget.onMouseMove (this, e);
				return true;
			}

			if (HoverWidget != null) {
				//check topmost graphicobject first
				GraphicObject tmp = HoverWidget;
				GraphicObject topc = null;
				while (!(tmp is Interface)) {
					topc = tmp;
					tmp = tmp.LogicalParent;
				}
				int idxhw = GraphicTree.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						if (GraphicTree [i].localLogicalParentIsNull) {
							if (GraphicTree [i].MouseIsIn (e.Position)) {
								while (HoverWidget != null) {
									HoverWidget.onMouseLeave (HoverWidget, e);
									HoverWidget = HoverWidget.LogicalParent;
								}

								GraphicTree [i].checkHoverWidget (e);
								return true;
							}
						}
						i++;
					}
				}

				if (HoverWidget.MouseIsIn (e.Position)) {
					HoverWidget.checkHoverWidget (e);
					return true;
				} else {
					HoverWidget.onMouseLeave (HoverWidget, e);
					//seek upward from last focused graph obj's
					while (!(HoverWidget.LogicalParent is Interface)) {
						HoverWidget = HoverWidget.LogicalParent;
						if (HoverWidget.MouseIsIn (e.Position)) {
							HoverWidget.checkHoverWidget (e);
							return true;
						} else
							HoverWidget.onMouseLeave (HoverWidget, e);
					}
				}
			}

			//top level graphic obj's parsing
			lock (GraphicTree) {
				for (int i = 0; i < GraphicTree.Count; i++) {
					GraphicObject g = GraphicTree [i];
					if (g.MouseIsIn (e.Position)) {
						g.checkHoverWidget (e);
						if (g is Window)
							PutOnTop (g);
						return true;
					}
				}
			}
			HoverWidget = null;
			return false;
		}
		public bool ProcessMouseButtonUp(int button)
		{
			Mouse.DisableBit (button);
			MouseButtonEventArgs e = new MouseButtonEventArgs () { Mouse = Mouse };

			if (_activeWidget == null)
				return false;

			if (mouseRepeatThread != null) {
				mouseRepeatOn = false;
				mouseRepeatThread.Abort();
				mouseRepeatThread.Join ();
			}

			_activeWidget.onMouseUp (_activeWidget, e);
			ActiveWidget = null;
			return true;
		}
		public bool ProcessMouseButtonDown(int button)
		{
			Mouse.EnableBit (button);
			MouseButtonEventArgs e = new MouseButtonEventArgs () { Mouse = Mouse };

			if (HoverWidget == null)
				return false;

			HoverWidget.onMouseDown(HoverWidget,new BubblingMouseButtonEventArg(e));

			if (FocusedWidget == null)
				return true;
			if (!FocusedWidget.MouseRepeat)
				return true;
			mouseRepeatThread = new Thread (mouseRepeatThreadFunc);
			mouseRepeatThread.IsBackground = true;
			mouseRepeatThread.Start ();
			return true;
		}
		public bool ProcessMouseWheelChanged(float delta)
		{
			Mouse.SetScrollRelative (0, delta);
			MouseWheelEventArgs e = new MouseWheelEventArgs () { Mouse = Mouse, DeltaPrecise = delta };

			if (HoverWidget == null)
				return false;
			HoverWidget.onMouseWheel (this, e);
			return true;
		}
		public bool ProcessKeyDown(int Key){
			Keyboard.SetKeyState((Crow.Key)Key,true);

			KeyboardKeyEventArgs e = lastKeyDownEvt = new KeyboardKeyEventArgs((Crow.Key)Key, false, Keyboard);
			lastKeyDownEvt.IsRepeat = true;

			KeyboardKeyDown.Raise (this, e);

			keyboardRepeatThread = new Thread (keyboardRepeatThreadFunc);
			keyboardRepeatThread.IsBackground = true;
			keyboardRepeatThread.Start ();

			if (_focusedWidget == null)
				return false;
			_focusedWidget.onKeyDown (this, e);

			return true;
		}
		public bool ProcessKeyUp(int Key){
			Keyboard.SetKeyState ((Crow.Key)Key, false);
			KeyboardKeyEventArgs e = new KeyboardKeyEventArgs((Crow.Key)Key, false, Keyboard);

			KeyboardKeyUp (this, e);

			if (keyboardRepeatThread != null) {
				keyboardRepeatOn = false;
				keyboardRepeatThread.Abort();
				keyboardRepeatThread.Join ();
			}
			if (_focusedWidget == null)
				return false;
			_focusedWidget.onKeyUp (this, e);
			return true;
		}
		public bool ProcessKeyPress(char Key){
			if (_focusedWidget == null)
				return false;
			KeyPressEventArgs e = new KeyPressEventArgs(Key);
			_focusedWidget.onKeyPress (this, e);
			return true;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e) {}
		public override void onMouseUp (object sender, MouseButtonEventArgs e) {}
		public override void onMouseDown (object sender, MouseButtonEventArgs e) {}
		public override void onMouseClick (object sender, MouseButtonEventArgs e) {}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e) {}
		public override void onMouseWheel (object sender, MouseWheelEventArgs e) {}

		#endregion

		#region Device Repeat Events
		volatile bool mouseRepeatOn, keyboardRepeatOn;
		volatile int mouseRepeatCount, keyboardRepeatCount;
		Thread mouseRepeatThread, keyboardRepeatThread;
		KeyboardKeyEventArgs lastKeyDownEvt;
		void mouseRepeatThreadFunc()
		{
			mouseRepeatOn = true;
			Thread.Sleep (Interface.DeviceRepeatDelay);
			while (mouseRepeatOn) {
				mouseRepeatCount++;
				Thread.Sleep (Interface.DeviceRepeatInterval);
			}
			mouseRepeatCount = 0;
		}
		void keyboardRepeatThreadFunc()
		{
			keyboardRepeatOn = true;
			Thread.Sleep (Interface.DeviceRepeatDelay);
			while (keyboardRepeatOn) {
				keyboardRepeatCount++;
				Thread.Sleep (Interface.DeviceRepeatInterval);
			}
			keyboardRepeatCount = 0;
		}
		#endregion

		public override void RegisterClip(Rectangle r){
			clipping.UnionRectangle (r);
		}
		public override bool ArrangeChildren { get { return false; }}
		public override void RegisterForLayouting (LayoutingType layoutType) { }
		public override bool UpdateLayout (LayoutingType layoutType) { throw new NotImplementedException (); }
		public override Rectangle ContextCoordinates (Rectangle r) { return r;}
		public override Rectangle ScreenCoordinates (Rectangle r) { return r; }
		public override GraphicObject Parent {
			get { return null; }
			set { throw new NotImplementedException (); }
		}
		public override Rectangle ClientRectangle {
			get { return clientRectangle; }
		}
		public override Rectangle getSlot () { return ClientRectangle; }
		public override Measure WidthPolicy { get { return Measure.Stretched; }}
		public override Measure HeightPolicy { get { return Measure.Stretched; }}
		public override Measure Width {
			set { throw new NotImplementedException (); }
			get { return clientRectangle.Width;	}
		}
		public override Measure Height {
			set { throw new NotImplementedException (); }
			get { return clientRectangle.Height;	}
		}
		public override bool MouseIsIn (Point m) => true;

		#if MEASURE_TIME
		public PerformanceMeasure clippingMeasure = new PerformanceMeasure("Clipping", 100);
		public PerformanceMeasure layoutingMeasure = new PerformanceMeasure("Layouting", 100);
		public PerformanceMeasure updateMeasure = new PerformanceMeasure("Update", 100);
		public PerformanceMeasure drawingMeasure = new PerformanceMeasure("Drawing", 100);
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

