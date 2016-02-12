#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using System.IO;


namespace Crow
{
	class NUnitCrowWindow :  IValueChange, ILayoutable, IGOLibHost
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));			
		}
		#endregion

		public Rectangle ClientRectangle = new Rectangle(0,0,800,600);
		public List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		public Color Background = Color.Transparent;

		Rectangles _redrawClip = new Rectangles();//should find another way to access it from child
		List<GraphicObject> _gobjsToRedraw = new List<GraphicObject>();

		#region IGOLibHost implementation
		public Rectangles clipping {
			get {
				return _redrawClip;
			}
			set {
				_redrawClip = value;
			}
		}
		public List<GraphicObject> gobjsToRedraw {
			get {
				return _gobjsToRedraw;
			}
			set {
				_gobjsToRedraw = value;
			}
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
				g.RegisterForRedraw ();
			}
		}
		public void Quit ()
		{
		}

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
				_activeWidget = value;
			}
		}
		public GraphicObject hoverWidget
		{
			get { return _hoverWidget; }
			set { _hoverWidget = value; }
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

		#endregion

		#region Events
		//those events are raised only if mouse isn't in a graphic object
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		#endregion

		#region graphic contexte
		Context ctx;
		public Surface surf;
		string testId;
		#endregion

		#region update
		public Stopwatch updateTime = new Stopwatch ();
		public Stopwatch layoutTime = new Stopwatch ();
		public Stopwatch guTime = new Stopwatch ();
		public Stopwatch drawingTime = new Stopwatch ();

		public void Update ()
		{
			ctx = new Context(surf);

			guTime.Reset ();
			updateTime.Restart ();
			layoutTime.Restart ();

			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList, 0);
			invGOList = invGOList.Reverse ().ToArray ();

			//Debug.WriteLine ("======= Layouting queue start =======");

			while (Interface.LayoutingQueue.Count > 0) {
				LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
				lqi.ProcessLayouting ();
			}

			layoutTime.Stop ();

			//Debug.WriteLine ("otd:" + gobjsToRedraw.Count.ToString () + "-");
			//final redraw clips should be added only when layout is completed among parents,
			//that's why it take place in a second pass
			GraphicObject[] gotr = new GraphicObject[gobjsToRedraw.Count];
			gobjsToRedraw.CopyTo (gotr);
			gobjsToRedraw.Clear ();
			foreach (GraphicObject p in gotr) {
				p.IsQueuedForRedraw = false;
				p.Parent.RegisterClip (p.LastPaintedSlot);
				p.Parent.RegisterClip (p.getSlot());
			}

			guTime.Start ();

			using (ctx = new Context (surf)){
				if (clipping.count > 0) {
					//Link.draw (ctx);
					clipping.clearAndClip(ctx);

					foreach (GraphicObject p in invGOList) {
						if (!p.Visible)
							continue;

						ctx.Save ();

						p.Paint (ref ctx);

						ctx.Restore ();
					}

					#if DEBUG_CLIP_RECTANGLE
					clipping.stroke (ctx, Color.Red.AdjustAlpha(0.5));
					#endif

					clipping.Reset ();
				}
			}

			guTime.Stop ();
			updateTime.Stop ();


			sw.WriteLine ("{0}\t{1,8}\t{2,8}\t{3,8}\t{4,8}",
				testId,
				layoutTime.ElapsedTicks,
				guTime.ElapsedTicks,
				updateTime.ElapsedTicks,
				loadTime.ElapsedTicks);
			sw.Flush ();
			
//			Console.WriteLine("{3} => layout:{0}ms\tdraw{1}ms\tupdate:{2}ms",
//				layoutTime.ElapsedMilliseconds,
//				guTime.ElapsedMilliseconds,
//				updateTime.ElapsedMilliseconds,
//				testId);
			//surf.WriteToPng (@"ExpectedOutputs/" + testId + ".png");
			surf.WriteToPng (@"tmp.png");
		}						
		#endregion

		#region loading
		public Stopwatch loadTime = new Stopwatch ();
		public GraphicObject LoadTest (string id)
		{
			testId = id;
			loadTime.Start ();
			GraphicObject tmp = Interface.Load ("Interfaces/" + testId + ".crow", this);
			loadTime.Stop ();
			AddWidget (tmp);
			return tmp;
		}
		/// <summary> Remove all Graphic objects from top container </summary>
		public void ClearInterface()
		{
			int i = 0;
			while (GraphicObjects.Count>0) {
				GraphicObject g = GraphicObjects [i];
				g.Visible = false;
				g.ClearBinding ();
				GraphicObjects.RemoveAt (0);
			}
		}
		#endregion

		#region CTOR
		public NUnitCrowWindow (int width, int height)
		{
			ClientRectangle.Width = width;
			ClientRectangle.Height = height;

			surf = new ImageSurface(Format.Argb32, ClientRectangle.Width, ClientRectangle.Height);
			string path = "crow-" + DateTime.Now + ".txt";

			sw = new StreamWriter (path);

			sw.WriteLine ("ID        layout            draw          update            load");
			sw.WriteLine ("----------------------------------------------------------------");
			sw.Flush ();
		}
		~NUnitCrowWindow(){
			
			sw.Close ();
		}
		#endregion

		int frameCpt = 0;
		int idx = 0;
		StreamWriter sw;

		#region FPS
		int _fps = 0;

		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;

				_fps = value;

				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax));
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}

				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms"));
			}
		}

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		//public string update = "";
		#endregion


		#region Mouse Handling
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (_activeWidget != null) {
				//first, ensure object is still in the graphic tree
				if (_activeWidget.HostContainer == null) {
					activeWidget = null;
				} else {

					//send move evt even if mouse move outside bounds
					_activeWidget.onMouseMove (_activeWidget, e);
					return;
				}
			}

			if (_hoverWidget != null) {
				//first, ensure object is still in the graphic tree
				if (_hoverWidget.HostContainer == null) {
					hoverWidget = null;
				} else {
					//check topmost graphicobject first
					GraphicObject tmp = _hoverWidget;
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
								_hoverWidget.onMouseLeave (this, e);
								GraphicObjects [i].checkHoverWidget (e);
								return;
							}
							i++;
						}
					}


					if (_hoverWidget.MouseIsIn (e.Position)) {
						_hoverWidget.checkHoverWidget (e);
						return;
					} else {
						_hoverWidget.onMouseLeave (this, e);
						//seek upward from last focused graph obj's
						while (_hoverWidget.Parent as GraphicObject != null) {
							_hoverWidget = _hoverWidget.Parent as GraphicObject;
							if (_hoverWidget.MouseIsIn (e.Position)) {
								_hoverWidget.checkHoverWidget (e);
								return;
							} else
								_hoverWidget.onMouseLeave (this, e);
						}
					}
				}
			}

			//top level graphic obj's parsing
			for (int i = 0; i < GraphicObjects.Count; i++) {
				GraphicObject g = GraphicObjects[i];
				if (g.MouseIsIn (e.Position)) {
					g.checkHoverWidget (e);
					PutOnTop (g);
					return;
				}
			}
			_hoverWidget = null;
			MouseMove.Raise (this, e);
		}
		void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_activeWidget == null) {
				MouseButtonUp.Raise (this, e);
				return;
			}

			_activeWidget.onMouseUp (this, e);
			_activeWidget = null;
		}
		void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (_hoverWidget == null) {
				MouseButtonDown.Raise (this, e);
				return;
			}

			GraphicObject g = _hoverWidget;
			while (!g.Focusable) {				
				g = g.Parent as GraphicObject;
				if (g == null) {					
					return;
				}
			}

			_activeWidget = g;
			_activeWidget.onMouseDown (this, e);
		}

		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			if (_hoverWidget == null) {
				MouseWheelChanged.Raise (this, e);
				return;
			}
			_hoverWidget.onMouseWheel (this, e);
		}        
		#endregion

		#region keyboard Handling
		void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
		{			
			if (_focusedWidget == null)
				return;
			_focusedWidget.onKeyDown (sender, e);
		}
		#endregion

		#region ILayoutable implementation
		public void RegisterClip(Rectangle r){
			clipping.AddRectangle (r);
		}
		public void RegisterForLayouting (LayoutingType layoutType)
		{
			throw new NotImplementedException ();
		}			
		public int LayoutingTries {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}
		public ILayoutable LogicalParent {
			get { return null; }
			set { throw new NotImplementedException (); }
		}
		public ILayoutable Parent {
			get { return null; }
			set { throw new NotImplementedException (); }
		}
		public LayoutingType RegisteredLayoutings {
			get { return LayoutingType.None; }
			set { throw new NotImplementedException (); } 
		}
		public void RegisterForLayouting (int layoutType) { throw new NotImplementedException (); }
		public bool UpdateLayout (LayoutingType layoutType) { throw new NotImplementedException (); }
		public Rectangle ContextCoordinates (Rectangle r) => r;
		public Rectangle ScreenCoordinates (Rectangle r) => r;
		Rectangle ILayoutable.ClientRectangle {
			get { return new Size(this.ClientRectangle.Size.Width,this.ClientRectangle.Size.Height); }
		}
		public IGOLibHost HostContainer { get { return this; }}
		public Rectangle getSlot () => ClientRectangle;
		public Rectangle getBounds () => ClientRectangle;
		#endregion	
	}
}