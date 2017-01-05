//
//  Interface.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
		public static int DoubleClick = 200;//ms
		internal Stopwatch clickTimer = new Stopwatch();
		internal GraphicObject eligibleForDoubleClick = null; 
		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
		//TODO: shold be declared in graphicObject
		public static bool FocusOnHover = false;
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DeviceRepeatDelay = 700;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DeviceRepeatInterval = 40;
		public static bool ReplaceTabsWithSpace = false;
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;
		/// <summary> Disable caching for a widget if this threshold is reached </summary>
		public const int MaxCacheSize = 2048;
		/// <summary> Above this count, the layouting is discard for the widget and it
		/// will not be rendered on screen </summary>
		public const int MaxLayoutingTries = 3;
		public const int MaxDiscardCount = 5;
		/// <summary> Global font rendering settings for Cairo </summary>
		public static FontOptions FontRenderingOptions;
		#endregion

		internal static Interface CurrentInterface;

		public Dictionary<string,object> Ressources = new Dictionary<string, object>();
		public Queue<LayoutingQueueItem> LayoutingQueue = new Queue<LayoutingQueueItem> ();
		public Queue<LayoutingQueueItem> DiscardQueue;
		public Queue<LayoutingQueueItem> ProcessedLayoutingQueue;
		public Queue<GraphicObject> DrawingQueue = new Queue<GraphicObject>();
		public string Clipboard;//TODO:use object instead for complex copy paste
		public void EnqueueForRepaint(GraphicObject g)
		{
			lock (DrawingQueue) {
				if (g.IsQueueForRedraw)
					return;
				DrawingQueue.Enqueue (g);
				g.IsQueueForRedraw = true;
			}
		}
		//fast compiled IML instantiators
		public static Dictionary<String, Instantiator> Instantiators = new Dictionary<string, Instantiator>();

		#region Default values and Style loading
		/// Default values of properties from GraphicObjects are retrieve from XML Attributes.
		/// The reflexion process used to retrieve those values being very slow, it is compiled in MSIL
		/// and injected as a dynamic method referenced in the DefaultValuesLoader Dictionnary.
		/// The compilation is done on the first object instancing, and is also done for custom widgets
		public delegate void LoaderInvoker(object instance);
		public static Dictionary<String, LoaderInvoker> DefaultValuesLoader = new Dictionary<string, LoaderInvoker>();
		public static Dictionary<string, Style> Styling;
		/// <summary> parse all styling data's and build global Styling Dictionary </summary>
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
		public static Dictionary<String, string> DefaultTemplates = new Dictionary<string, string>();
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
		public GraphicObject Load (string path)
		{
			try {
				return GetInstantiator (path).CreateInstance (this);
			} catch (Exception ex) {
				throw new Exception ("Error loading <" + path + ">:", ex);
			}
		}
		/// <summary>
		/// fetch it from cache or create it
		/// </summary>
		public static Instantiator GetInstantiator(string path){
			if (!Instantiators.ContainsKey(path))
				Instantiators [path] = new Instantiator(path);
			return Instantiators [path];
		}
		public static ItemTemplate GetItemTemplate(string path){
			if (!Instantiators.ContainsKey(path))
				Instantiators [path] = new ItemTemplate(path);
			return Instantiators [path] as ItemTemplate;
		}
		public GraphicObject LoadInterface (string path)
		{
			lock (UpdateMutex) {
				GraphicObject tmp = Load (path);
				AddWidget (tmp);

				return tmp;
			}
		}
		#endregion

		#if MEASURE_TIME
		public PerformanceMeasure clippingMeasure = new PerformanceMeasure("Clipping", 100);
		public PerformanceMeasure layoutingMeasure = new PerformanceMeasure("Layouting", 100);
		public PerformanceMeasure updateMeasure = new PerformanceMeasure("Update", 100);
		public PerformanceMeasure drawingMeasure = new PerformanceMeasure("Drawing", 100);
		#endif

		public List<GraphicObject> GraphicTree = new List<GraphicObject>();

		Rectangles _redrawClip = new Rectangles();

		Context ctx;
		Surface surf;
		public byte[] bmp;
		public byte[] dirtyBmp;
		public bool IsDirty = false;
		public Rectangle DirtyRect;
		public object LayoutMutex = new object();
		public object RenderMutex = new object();
		public object UpdateMutex = new object();

		#region focus
		GraphicObject _activeWidget;	//button is pressed on widget
		GraphicObject _hoverWidget;		//mouse is over
		GraphicObject _focusedWidget;	//has keyboard (or other perif) focus

		public GraphicObject activeWidget
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
		public void Update(){
			if (mouseRepeatCount > 0) {
				int mc = mouseRepeatCount;
				mouseRepeatCount -= mc;
				for (int i = 0; i < mc; i++) {
					FocusedWidget.onMouseClick (this, new MouseButtonEventArgs (Mouse.X, Mouse.Y, MouseButton.Left, true));
				}
			}
			if (keyboardRepeatCount > 0) {
				int mc = keyboardRepeatCount;
				keyboardRepeatCount -= mc;
				for (int i = 0; i < mc; i++) {
					_focusedWidget.onKeyDown (this, lastKeyDownEvt);
				}
			}

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
		void processLayouting(){
			#if MEASURE_TIME
			layoutingMeasure.StartCycle();
			#endif
			DiscardQueue = new Queue<LayoutingQueueItem> ();
			lock (LayoutMutex) {
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
			}
			DiscardQueue = null;

			#if MEASURE_TIME
			layoutingMeasure.StopCycle();
			#endif
		}
		void clippingRegistration(){
			#if MEASURE_TIME
			clippingMeasure.StartCycle();
			#endif
			GraphicObject g = null;
			while (DrawingQueue.Count > 0) {
				lock (DrawingQueue)
					g = DrawingQueue.Dequeue ();
				g.IsQueueForRedraw = false;
				g.Parent.RegisterClip (g.LastPaintedSlot);
				g.Parent.RegisterClip (g.getSlot ());
			}

			#if MEASURE_TIME
			clippingMeasure.StopCycle();
			#endif
		}
		void processDrawing(){
			#if MEASURE_TIME
			drawingMeasure.StartCycle();
			#endif
			using (surf = new ImageSurface (bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height, ClientRectangle.Width * 4)) {
				using (ctx = new Context (surf)){
					if (clipping.count > 0) {
						//Link.draw (ctx);
						clipping.clearAndClip(ctx);

						for (int i = GraphicTree.Count -1; i >= 0 ; i--){
							GraphicObject p = GraphicTree[i];
							if (!p.Visible)
								continue;
							if (!clipping.intersect (p.Slot))
								continue;
							ctx.Save ();

							p.Paint (ref ctx);

							ctx.Restore ();
						}

						#if DEBUG_CLIP_RECTANGLE
						clipping.stroke (ctx, Color.Red.AdjustAlpha(0.5));
						#endif
						lock (RenderMutex) {
							if (IsDirty)
								DirtyRect += clipping.Bounds;
							else
								DirtyRect = clipping.Bounds;
							IsDirty = true;

							DirtyRect.Left = Math.Max (0, DirtyRect.Left);
							DirtyRect.Top = Math.Max (0, DirtyRect.Top);
							DirtyRect.Width = Math.Min (ClientRectangle.Width - DirtyRect.Left, DirtyRect.Width);
							DirtyRect.Height = Math.Min (ClientRectangle.Height - DirtyRect.Top, DirtyRect.Height);
							DirtyRect.Width = Math.Max (0, DirtyRect.Width);
							DirtyRect.Height = Math.Max (0, DirtyRect.Height);

							if (DirtyRect.Width > 0) {
								dirtyBmp = new byte[4 * DirtyRect.Width * DirtyRect.Height];
								for (int y = 0; y < DirtyRect.Height; y++) {
									Array.Copy (bmp,
										((DirtyRect.Top + y) * ClientRectangle.Width * 4) + DirtyRect.Left * 4,
										dirtyBmp, y * DirtyRect.Width * 4, DirtyRect.Width * 4);
								}
							} else
								IsDirty = false;
						}
						clipping.Reset ();
					}
					//surf.WriteToPng (@"/mnt/data/test.png");
				}
			}
			#if MEASURE_TIME
			drawingMeasure.StopCycle();
			#endif
		}

		public Rectangles clipping {
			get { return _redrawClip; }
			set { _redrawClip = value; }
		}
		public void AddWidget(GraphicObject g)
		{
			g.Parent = this;
			GraphicTree.Insert (0, g);
			g.RegisteredLayoutings = LayoutingType.None;
			g.RegisterForLayouting (LayoutingType.Sizing | LayoutingType.ArrangeChildren);
		}
		public void DeleteWidget(GraphicObject g)
		{
			g.Visible = false;//trick to ensure clip is added to refresh zone
			GraphicTree.Remove (g);
		}
		public void PutOnTop(GraphicObject g)
		{
			if (GraphicTree.IndexOf(g) > 0)
			{
				GraphicTree.Remove(g);
				GraphicTree.Insert(0, g);
				EnqueueForRepaint (g);
			}
		}
		/// <summary> Remove all Graphic objects from top container </summary>
		public void ClearInterface()
		{
			int i = 0;
			while (GraphicTree.Count>0) {
				//TODO:parent is not reset to null because object will be added
				//to ObjectToRedraw list, and without parent, it fails
				GraphicObject g = GraphicTree [i];
				g.DataSource = null;
				g.Visible = false;
				GraphicTree.RemoveAt (0);
			}
			#if DEBUG_LAYOUTING
			LQIsTries = new List<LQIList>();
			curLQIsTries = new LQIList();
			LQIs = new List<LQIList>();
			curLQIs = new LQIList();
			#endif
		}
		public GraphicObject FindByName (string nameToFind)
		{
			foreach (GraphicObject w in GraphicTree) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}



		public void ProcessResize(Rectangle bounds){
			lock (UpdateMutex) {
				clientRectangle = bounds;

				int stride = 4 * ClientRectangle.Width;
				int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
				bmp = new byte[bmpSize];

				foreach (GraphicObject g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				clipping.AddRectangle (clientRectangle);
			}
		}

		XCursor cursor = XCursor.Default;
		public MouseState Mouse;
		public KeyboardState Keyboard;
		Rectangle clientRectangle;

		public event EventHandler<MouseCursorChangedEventArgs> MouseCursorChanged;
		public event EventHandler Quit;

		#region Mouse Handling

		public XCursor MouseCursor {
			set {
				if (value == cursor)
					return;
				cursor = value;
				MouseCursorChanged.Raise (this,new MouseCursorChangedEventArgs(cursor));
			}
		}
		public bool ProcessMouseMove(int x, int y)
		{
			int deltaX = x - Mouse.X;
			int deltaY = y - Mouse.Y;
			Mouse.X = x;
			Mouse.Y = y;
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);
			e.Mouse = Mouse;

			if (_activeWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				_activeWidget.onMouseMove (this, e);
				return true;
			}

			if (HoverWidget != null) {
				//TODO, ensure object is still in the graphic tree
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
			activeWidget = null;
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


		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{			
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{			
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
		}
		public override void onMouseClick (object sender, MouseButtonEventArgs e)
		{			
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{			
		}
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{			
		}
		#endregion

		#region Keyboard
		public bool ProcessKeyDown(int Key){
			Keyboard.SetKeyState((Crow.Key)Key,true);
			if (_focusedWidget == null)
				return false;
			KeyboardKeyEventArgs e = lastKeyDownEvt = new KeyboardKeyEventArgs((Crow.Key)Key, false, Keyboard);
			lastKeyDownEvt.IsRepeat = true;
			_focusedWidget.onKeyDown (this, e);

			keyboardRepeatThread = new Thread (keyboardRepeatThreadFunc);
			keyboardRepeatThread.IsBackground = true;
			keyboardRepeatThread.Start ();

			return true;
		}
		public bool ProcessKeyUp(int Key){
			Keyboard.SetKeyState((Crow.Key)Key,false);
			if (_focusedWidget == null)
				return false;
			KeyboardKeyEventArgs e = new KeyboardKeyEventArgs((Crow.Key)Key, false, Keyboard);

			_focusedWidget.onKeyUp (this, e);

			if (keyboardRepeatThread != null) {
				keyboardRepeatOn = false;
				keyboardRepeatThread.Abort();
				keyboardRepeatThread.Join ();
			}
			return true;
		}
		public bool ProcessKeyPress(char Key){
			if (_focusedWidget == null)
				return false;
			KeyPressEventArgs e = new KeyPressEventArgs(Key);
			_focusedWidget.onKeyPress (this, e);
			return true;
		}
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

		#region ILayoutable implementation
		public override void RegisterClip(Rectangle r){ clipping.AddRectangle (r); }
		public override LayoutingType RegisteredLayoutings {
			get { return LayoutingType.None; }
			set { throw new NotImplementedException (); }
		}
		public override Rectangle ContextCoordinates (Rectangle r) { return r;}
		public override Rectangle ScreenCoordinates (Rectangle r) { return r; }
		public override Rectangle ClientRectangle { get { return clientRectangle; }}
		public override Rectangle getSlot () { return ClientRectangle; }
		public override Measure WidthPolicy { get { return Measure.Stretched; }}
		public override Measure HeightPolicy { get { return Measure.Stretched; }}
		public override Measure Width {
			get { return clientRectangle.Width;	}
			set {}
		}
		public override Measure Height {
			get { return clientRectangle.Height;	}
			set {}
		}
		public override bool MouseIsIn (Point m){ return true; }
		public override void RegisterForLayouting (LayoutingType layoutType)
		{
			//base.RegisterForLayouting (layoutType);
			return;
		}
		#endregion
	}
}

