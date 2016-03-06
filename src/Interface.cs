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
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Reflection.Emit;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Threading;
using Cairo;

namespace Crow
{
	public class Interface : ILayoutable
	{
		#region CTOR
		static Interface(){
			Interface.LoadCursors ();
		}
		public Interface(){
			Interface.CurrentInterface = this;
		}
		#endregion

		#region Static and constants
		/// <summary> Used to prevent spurious loading of templates </summary>
		internal static bool XmlSerializerInit = false;
		/// <summary> keep ressource path for debug msg </summary>
		internal static string CurrentGOMLPath = "";
		internal static int XmlLoaderCount = 0;

		public static int TabSize = 4;
		public static string LineBreak = "\r\n";
		public static bool FocusOnHover = false;
		/// <summary> Time to wait in millisecond before starting repeat loop</summary>
		public static int DeviceRepeatDelay = 600;
		/// <summary> Time interval in millisecond between device event repeat</summary>
		public static int DeviceRepeatInterval = 100;
		public static bool ReplaceTabsWithSpace = false;
		/// <summary> Allow rendering of interface in development environment </summary>
		public static bool DesignerMode = false;
		/// <summary> Threshold to catch borders for sizing </summary>
		public static int BorderThreshold = 5;
		public const int MaxCacheSize = 2048;
		public const int MaxLayoutingTries = 50;
		#endregion

		public Queue<LayoutingQueueItem> LayoutingQueue = new Queue<LayoutingQueueItem>();
		public Queue<GraphicObject> GraphicUpdateQueue = new Queue<GraphicObject>();

		public static void RegisterForGraphicUpdate(GraphicObject g)
		{
			lock (CurrentInterface.GraphicUpdateQueue) {
				if (g.IsQueueForGraphicUpdate)
					return;
				if (CurrentInterface == null)
					return;
				CurrentInterface.GraphicUpdateQueue.Enqueue (g);
				g.IsQueueForGraphicUpdate = true;
			}
		}
		public static void AddToRedrawList(GraphicObject g)
		{
			if (g.IsInRedrawList)
				return;
			if (Interface.CurrentInterface == null)
				return;
			Interface.CurrentInterface.RedrawList.Add (g);
			g.IsInRedrawList = true;
		}

		#region default values loading helpers
		public delegate void loadDefaultInvoker(object instance);
		public static Dictionary<String, loadDefaultInvoker> DefaultValuesLoader = new Dictionary<string, loadDefaultInvoker>();
		#endregion

		public static void LoadCursors(){
			//Load cursors
			XCursor.Cross = XCursorFile.Load("#Crow.Images.Icons.Cursors.cross").Cursors[0];
			XCursor.Default = XCursorFile.Load("#Crow.Images.Icons.Cursors.arrow").Cursors[0];
			XCursor.NW = XCursorFile.Load("#Crow.Images.Icons.Cursors.top_left_corner").Cursors[0];
			XCursor.NE = XCursorFile.Load("#Crow.Images.Icons.Cursors.top_right_corner").Cursors[0];
			XCursor.SW = XCursorFile.Load("#Crow.Images.Icons.Cursors.bottom_left_corner").Cursors[0];
			XCursor.SE = XCursorFile.Load("#Crow.Images.Icons.Cursors.bottom_right_corner").Cursors[0];
			XCursor.H = XCursorFile.Load("#Crow.Images.Icons.Cursors.sb_h_double_arrow").Cursors[0];
			XCursor.V = XCursorFile.Load("#Crow.Images.Icons.Cursors.sb_v_double_arrow").Cursors[0];
		}

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

		/// <summary>
		/// Pre-read first node to set GraphicObject class for loading
		/// and reset stream position to 0
		/// </summary>
		public static Type GetTopContainerOfGOMLStream (Stream stream)
		{
			string root = "Object";
			stream.Seek (0, SeekOrigin.Begin);
			using (XmlReader reader = XmlReader.Create (stream)) {
				while (reader.Read ()) {
					// first element is the root element
					if (reader.NodeType == XmlNodeType.Element) {
						root = reader.Name;
						break;
					}
				}
			}

			Type t = Type.GetType ("Crow." + root);

			stream.Seek (0, SeekOrigin.Begin);
			return t;
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
		public static GraphicObject Load (string path, object hostClass = null)
		{
			Interface.XmlLoaderCount ++;
			CurrentGOMLPath = path;
			GraphicObject tmp = null;
			using (Stream stream = GetStreamFromPath (path)) {
				tmp = Load(stream, GetTopContainerOfGOMLStream(stream), hostClass);
			}
			Interface.XmlLoaderCount --;
			return tmp;
		}
		public static GraphicObject Load (Stream stream, Type type, object hostClass = null)
		{
			#if DEBUG_LOAD
			Stopwatch loadingTime = new Stopwatch ();
			loadingTime.Start ();
			#endif

			GraphicObject result;


			XmlSerializerNamespaces xn = new XmlSerializerNamespaces ();
			xn.Add ("", "");

			XmlSerializerInit = true;
			XmlSerializer xs = new XmlSerializer (type);
			XmlSerializerInit = false;

			result = (GraphicObject)xs.Deserialize (stream);
			//result.DataSource = hostClass;

			#if DEBUG_LOAD
			FileStream fs = stream as FileStream;
			if (fs!=null)
				CurrentGOMLPath = fs.Name;
			loadingTime.Stop ();
			Debug.WriteLine ("GOML Loading ({2}->{3}): {0} ticks, {1} ms",
				loadingTime.ElapsedTicks,
				loadingTime.ElapsedMilliseconds,
			CurrentGOMLPath, result.ToString());
			#endif

			return result;
		}

		public void InterfaceLoad(string path){
			GraphicObject tmp = Interface.Load (path, this);
			AddWidget (tmp);
		}

		public GraphicObject LoadInterface (string path)
		{
			lock (UpdateMutex) {
				GraphicObject tmp = Interface.Load (path, this);
				AddWidget (tmp);
				return tmp;
			}
		}

		#endregion

		#if MEASURE_TIME
		public Stopwatch clippingTime = new Stopwatch ();
		public Stopwatch layoutTime = new Stopwatch ();
		public Stopwatch guTime = new Stopwatch ();
		public Stopwatch drawingTime = new Stopwatch ();
		#endif

		public List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		public Color Background = Color.Transparent;

		internal static Interface currentWindow;
		public static Interface CurrentInterface;

		Rectangles _redrawClip = new Rectangles();//should find another way to access it from child
		List<GraphicObject> _gobjsToRedraw = new List<GraphicObject>();

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
					_activeWidget.IsActive = true;
			}
		}
		public GraphicObject hoverWidget
		{
			get { return _hoverWidget; }
			set {
				if (_hoverWidget == value)
					return;
				_hoverWidget = value;
			}
		}
		public GraphicObject FocusedWidget {
			get { return _focusedWidget; }
			set {
				if (_focusedWidget == value)
					return;
				if (_focusedWidget != null)
					_focusedWidget.onUnfocused (this, null);
				_focusedWidget = value;
				if (_focusedWidget != null)
					_focusedWidget.onFocused (this, null);
			}
		}
		#endregion


		public void Update(){
			if (mouseRepeatCount > 0) {
				int mc = mouseRepeatCount;
				mouseRepeatCount -= mc;
				for (int i = 0; i < mc; i++) {
					FocusedWidget.onMouseClick (this, new MouseButtonEventArgs (Mouse.X, Mouse.Y, MouseButton.Left, true));
				}
			}
			if (!Monitor.TryEnter (UpdateMutex))
				return;

			processLayouting ();

			clippingRegistration ();

			processDrawing ();

			Monitor.Exit (UpdateMutex);

			//			if (ToolTip.isVisible) {
			//				ToolTip.panel.processkLayouting();
			//				if (ToolTip.panel.layoutIsValid)
			//					ToolTip.panel.Paint(ref ctx);
			//			}
			//			Debug.WriteLine("INTERFACE: layouting: {0} ticks \t graphical update {1} ticks \t drawing {2} ticks",
			//			    layoutTime.ElapsedTicks,
			//			    guTime.ElapsedTicks,
			//			    drawingTime.ElapsedTicks);
			//			Debug.WriteLine("INTERFACE: layouting: {0} ms \t graphical update {1} ms \t drawing {2} ms",
			//			    layoutTime.ElapsedMilliseconds,
			//			    guTime.ElapsedMilliseconds,
			//			    drawingTime.ElapsedMilliseconds);

			//			Debug.WriteLine("UPDATE: {0} ticks \t, {1} ms",
			//				updateTime.ElapsedTicks,
				//				updateTime.ElapsedMilliseconds);
		}
		void processLayouting(){
			#if MEASURE_TIME
			layoutTime.Restart();
			#endif
			lock (LayoutMutex) {
				//Debug.WriteLine ("======= Layouting queue start =======");
				LayoutingQueueItem lqi = null;
				while (Interface.CurrentInterface.LayoutingQueue.Count > 0) {
					lqi = Interface.CurrentInterface.LayoutingQueue.Dequeue ();
					lqi.ProcessLayouting ();
				}
			}
			#if MEASURE_TIME
			layoutTime.Stop ();
			#endif
		}
		void clippingRegistration(){
			#if MEASURE_TIME
			clippingTime.Restart ();
			#endif
			lock (CurrentInterface.GraphicUpdateQueue) {
				while (CurrentInterface.GraphicUpdateQueue.Count > 0) {
					GraphicObject g = CurrentInterface.GraphicUpdateQueue.Dequeue ();
					g.bmp = null;
					g.IsQueueForGraphicUpdate = false;
					AddToRedrawList (g);
				}
			}
			//Debug.WriteLine ("otd:" + gobjsToRedraw.Count.ToString () + "-");
			//final redraw clips should be added only when layout is completed among parents,
			//that's why it take place in a second pass
			foreach (GraphicObject p in RedrawList) {
				try {
					p.IsInRedrawList = false;
					if (p.Parent == null)
						continue;
					p.Parent.RegisterClip (p.LastPaintedSlot);
					p.Parent.RegisterClip (p.getSlot ());
				} catch (Exception ex) {
					Debug.WriteLine ("Error Register Clip: " + ex.ToString ());
				}
			}
			RedrawList.Clear ();
			#if MEASURE_TIME
			clippingTime.Stop ();
			#endif
		}
		void processDrawing(){
			#if MEASURE_TIME
			drawingTime.Restart();
			#endif
			using (surf = new ImageSurface (bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height, ClientRectangle.Width * 4)) {
				using (ctx = new Context (surf)){


					if (clipping.count > 0) {
						//Link.draw (ctx);
						clipping.clearAndClip(ctx);

						for (int i = GraphicObjects.Count -1; i >= 0 ; i--){
							GraphicObject p = GraphicObjects[i];
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
						lock (Interface.CurrentInterface.RenderMutex) {
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

							dirtyBmp = new byte[4 * DirtyRect.Width * DirtyRect.Height];
							for (int y = 0; y < DirtyRect.Height; y++) {
								Array.Copy (bmp,
									((DirtyRect.Top + y) * ClientRectangle.Width * 4) + DirtyRect.Left * 4,
									dirtyBmp, y * DirtyRect.Width * 4, DirtyRect.Width * 4);
							}
						}
						clipping.Reset ();
					}
					//surf.WriteToPng (@"/mnt/data/test.png");
				}
			}
			#if MEASURE_TIME
			drawingTime.Stop ();
			#endif
		}

		public Rectangles clipping {
			get { return _redrawClip; }
			set { _redrawClip = value; }
		}
		public List<GraphicObject> RedrawList {
			get { return _gobjsToRedraw; }
			set { _gobjsToRedraw = value; }
		}
		public void AddWidget(GraphicObject g)
		{
			g.Parent = this;
			GraphicObjects.Insert (0, g);

			g.RegisterForLayouting (LayoutingType.Sizing);
		}
		public void DeleteWidget(GraphicObject g)
		{
			g.Visible = false;//trick to ensure clip is added to refresh zone
			g.ClearBinding();
			GraphicObjects.Remove (g);
		}
		public void PutOnTop(GraphicObject g)
		{
			if (GraphicObjects.IndexOf(g) > 0)
			{
				GraphicObjects.Remove(g);
				GraphicObjects.Insert(0, g);
				//g.registerClipRect ();
			}
		}
		/// <summary> Remove all Graphic objects from top container </summary>
		public void ClearInterface()
		{
			int i = 0;
			while (GraphicObjects.Count>0) {
				//TODO:parent is not reset to null because object will be added
				//to ObjectToRedraw list, and without parent, it fails
				GraphicObject g = GraphicObjects [i];
				g.Visible = false;
				g.ClearBinding ();
				GraphicObjects.RemoveAt (0);
			}
		}
		public GraphicObject FindByName (string nameToFind)
		{
			foreach (GraphicObject w in GraphicObjects) {
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

				foreach (GraphicObject g in GraphicObjects)
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

			if (hoverWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//check topmost graphicobject first
				GraphicObject tmp = hoverWidget;
				GraphicObject topc = null;
				while (tmp is GraphicObject) {
					topc = tmp;
					tmp = tmp.Parent as GraphicObject;
				}
				int idxhw = GraphicObjects.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						if (GraphicObjects [i].MouseIsIn (e.Position)) {
							hoverWidget.onMouseLeave (this, e);
							GraphicObjects [i].checkHoverWidget (e);
							return true;
						}
						i++;
					}
				}


				if (hoverWidget.MouseIsIn (e.Position)) {
					hoverWidget.checkHoverWidget (e);
					return true;
				} else {
					hoverWidget.onMouseLeave (this, e);
					//seek upward from last focused graph obj's
					while (hoverWidget.Parent as GraphicObject != null) {
						hoverWidget = hoverWidget.Parent as GraphicObject;
						if (hoverWidget.MouseIsIn (e.Position)) {
							hoverWidget.checkHoverWidget (e);
							return true;
						} else
							hoverWidget.onMouseLeave (this, e);
					}
				}
			}

			//top level graphic obj's parsing
			for (int i = 0; i < GraphicObjects.Count; i++) {
				GraphicObject g = GraphicObjects[i];
				if (g.MouseIsIn (e.Position)) {
					g.checkHoverWidget (e);
					PutOnTop (g);
					return true;
				}
			}
			hoverWidget = null;
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

			_activeWidget.onMouseUp (this, e);
			activeWidget = null;
			return true;
		}
		public bool ProcessMouseButtonDown(int button)
		{
			Mouse.EnableBit (button);
			MouseButtonEventArgs e = new MouseButtonEventArgs () { Mouse = Mouse };

			if (hoverWidget == null)
				return false;

			hoverWidget.onMouseDown(hoverWidget,new BubblingMouseButtonEventArg(e));

			if (FocusedWidget == null)
				return true;
			if (!FocusedWidget.MouseRepeat)
				return true;
			mouseRepeatThread = new Thread (mouseRepeatThreadFunc);
			mouseRepeatThread.Start ();
			return true;
		}
		public bool ProcessMouseWheelChanged(float delta)
		{
			Mouse.SetScrollRelative (0, delta);
			MouseWheelEventArgs e = new MouseWheelEventArgs () { Mouse = Mouse, DeltaPrecise = delta };

			if (hoverWidget == null)
				return false;
			hoverWidget.onMouseWheel (this, e);
			return true;
		}
//		public bool ProcessKeyDown(int Key){
//
//		}
		volatile bool mouseRepeatOn;
		volatile int mouseRepeatCount;
		Thread mouseRepeatThread;
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
		#endregion

		#region ILayoutable implementation
		public void RegisterClip(Rectangle r){
			clipping.AddRectangle (r);
		}
		public bool ArrangeChildren { get { return false; }}
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
		public Rectangle ContextCoordinates (Rectangle r) => r;
		public Rectangle ScreenCoordinates (Rectangle r) => r;

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
		public Rectangle getSlot () => ClientRectangle;
		public Rectangle getBounds () => ClientRectangle;
		#endregion
	}
}

