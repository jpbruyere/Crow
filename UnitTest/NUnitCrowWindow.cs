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
		public Rectangles redrawClip {
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

			g.RegisterForLayouting ((int)LayoutingType.Sizing);
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
				g.registerClipRect ();
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
			guTime.Reset ();
			updateTime.Restart ();
			layoutTime.Restart ();

			ctx = new Context(surf);

			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList,0);
			invGOList = invGOList.Reverse ().ToArray ();

			//Debug.WriteLine ("======= Layouting queue start =======");
				while (Interface.LayoutingQueue.Count > 0) {
					//					Stopwatch lqiProcTime = new Stopwatch ();
					//					lqiProcTime.Start ();
					LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
					lqi.ProcessLayouting ();
					//					lqiProcTime.Stop ();
					//					if (lqiProcTime.ElapsedMilliseconds > 10) {
					//						Debug.WriteLine("lqi {2}: {0} ticks \t, {1} ms",
					//							updateTime.ElapsedTicks,
					//							updateTime.ElapsedMilliseconds, lqi.ToString());
					//					}
				}

			//final redraw clips should be added only when layout is completed among parents,
			//that's why it take place in a second pass
			GraphicObject[] gotr = new GraphicObject[gobjsToRedraw.Count];
			gobjsToRedraw.CopyTo (gotr);
			gobjsToRedraw.Clear ();
			foreach (GraphicObject p in gotr) {
				p.registerClipRect ();
			}

			layoutTime.Stop ();
			guTime.Start ();

			lock (redrawClip) {
				if (redrawClip.count > 0) {					
					//					#if DEBUG_CLIP_RECTANGLE
					//					redrawClip.stroke (ctx, new Color(1.0,0,0,0.3));
					//					#endif
					redrawClip.clearAndClip (ctx);//rajouté après, tester si utile	

					//Link.draw (ctx);
					foreach (GraphicObject p in invGOList) {
						if (p.Visible) {
							drawingTime.Start ();

							ctx.Save ();
							if (redrawClip.count > 0) {
								Rectangles clip = redrawClip.intersectingRects (p.Slot);

								if (clip.count > 0)
									p.Paint (ref ctx, clip);
							}
							ctx.Restore ();

							drawingTime.Stop ();
						}
					}
					ctx.ResetClip ();
					//					#if DEBUG_CLIP_RECTANGLE
					//					redrawClip.stroke (ctx, Color.Red.AdjustAlpha(0.1));
					//					#endif
					redrawClip.Reset ();
				}
			}
			guTime.Stop ();
			updateTime.Stop ();
			ctx.Dispose ();
			Console.WriteLine("{3} => layout:{0,8} t\tdraw:{1,8} t\tupdate:{2,8} t",
			    layoutTime.ElapsedTicks,
			    guTime.ElapsedTicks,
			    updateTime.ElapsedTicks,
				testId);
//			Console.WriteLine("{3} => layout:{0}ms\tdraw{1}ms\tupdate:{2}ms",
//				layoutTime.ElapsedMilliseconds,
//				guTime.ElapsedMilliseconds,
//				updateTime.ElapsedMilliseconds,
//				testId);
			surf.WriteToPng (@"ExpectedOutputs/" + testId + ".png");
			surf.WriteToPng (@"tmp.png");
		}						
		#endregion

		#region loading
		public GraphicObject LoadTest (string id)
		{
			testId = id;
			GraphicObject tmp = Interface.Load ("Interfaces/" + testId + ".crow", this);
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

		public NUnitCrowWindow (int width, int height)
		{
			ClientRectangle.Width = width;
			ClientRectangle.Height = height;

			surf = new ImageSurface(Format.Argb32, ClientRectangle.Width, ClientRectangle.Height);
		}

		int frameCpt = 0;
		int idx = 0;


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

		//TODO:uneeded list, should be removed
		public List<LayoutingQueueItem> RegisteredLQIs { get; } = new List<LayoutingQueueItem>();
		public void RegisterForLayouting (int layoutType) { throw new NotImplementedException (); }
		public void UpdateLayout (LayoutingType layoutType) { throw new NotImplementedException (); }
		public Rectangle ContextCoordinates (Rectangle r)
		{
			return r;
		}
		public Rectangle ScreenCoordinates (Rectangle r)
		{
			return r;
		}

		public ILayoutable Parent {
			get {
				return null;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		Rectangle ILayoutable.ClientRectangle {
			get { return new Size(this.ClientRectangle.Size.Width,this.ClientRectangle.Size.Height); }
		}
		public IGOLibHost HostContainer {
			get { return this; }
		}

		public Rectangle getSlot ()
		{
			return ClientRectangle;
		}
		public Rectangle getBounds ()//redundant but fill ILayoutable implementation
		{
			return ClientRectangle;
		}			
		#endregion	
	}
}