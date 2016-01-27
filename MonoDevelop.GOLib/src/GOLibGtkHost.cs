using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crow;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Diagnostics;
using OpenTK.Input;
using MonoDevelop.DesignerSupport;
using System.IO;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.GOLib
{
	public class GOLibGtkHost : Gtk.DrawingArea, ILayoutable, IGOLibHost, IPropertyPadProvider,ICommandDelegator
	{
		#region ICommandDelegator implementation

		public object GetDelegatedCommandTarget ()
		{
			return hoverWidget;
		}

		#endregion

		#region IPropertyPadProvider implementation

		public object GetActiveComponent ()
		{
			return activeWidget;
		}
		public object GetProvider ()
		{
			return activeWidget;
		}
		public void OnEndEditing (object obj)
		{

		}
		public void OnChanged (object obj)
		{
			(obj as GraphicObject).registerForGraphicUpdate ();
			QueueDraw ();
		}
		#endregion
	
		#region Events
		//those events are raised only if mouse isn't in a graphic object
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		#endregion

		string _path;
		GraphicObject goWidget;

		public GOLibGtkHost ()
		{
			this.ExposeEvent += onExpose;
			this.ButtonPressEvent += GOLibGtkHost_ButtonPressEvent;
			this.ButtonReleaseEvent += GOLibGtkHost_ButtonReleaseEvent;
			this.MotionNotifyEvent += GOLibGtkHost_MotionNotifyEvent;

			this.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask |
				Gdk.EventMask.PointerMotionMask | Gdk.EventMask.PointerMotionHintMask;
		}
		static double[] dashed = {2.0};

		void onExpose(object o, Gtk.ExposeEventArgs args)
		{
			Gtk.DrawingArea area = (Gtk.DrawingArea) o;
			Cairo.Context cr =  Gdk.CairoHelper.Create(area.GdkWindow);
			_redrawClip.AddRectangle (this.ClientRectangle);

			//LoggingService.LogInfo ("expose event");

			update (cr);

			if (_hoverWidget != null) {
				cr.Rectangle (_hoverWidget.ScreenCoordinates(_hoverWidget.getSlot ()));
				cr.LineWidth = 1;
				cr.SetDash (dashed, 0);
				cr.Color = Crow.Color.Yellow;
				cr.Stroke ();
			}
			((IDisposable) cr.Target).Dispose();                                      
			((IDisposable) cr).Dispose();
		}
		// TODO: should find a more safer way to link gtk button to otk.
		void GOLibGtkHost_ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			MouseButtonEventArgs e = new MouseButtonEventArgs 
				((int)args.Event.X, (int)args.Event.Y, 
					gtkButtonIdToOpenTkButton(args.Event.Button), true);
			Mouse_ButtonDown (o, e);

			DesignerSupport.DesignerSupport.Service.SetPadContent (this, this);
		}
		void GOLibGtkHost_ButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			MouseButtonEventArgs e = new MouseButtonEventArgs 
				((int)args.Event.X, (int)args.Event.Y, 
					gtkButtonIdToOpenTkButton(args.Event.Button), false);
			Mouse_ButtonUp (o, e);
		}
		int lastX,LastY;
		void GOLibGtkHost_MotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			
			Mouse_Move(this, new MouseMoveEventArgs (
				(int)args.Event.X,(int)args.Event.Y,
				(int)args.Event.X-lastX, (int)args.Event.Y-LastY
			));
			lastX = (int)args.Event.X;
			LastY = (int)args.Event.Y;

			QueueDraw ();
		}

		MouseButton gtkButtonIdToOpenTkButton(uint gtkMouseButton)
		{
			switch (gtkMouseButton) {
			case 1:
				return MouseButton.Left;
			case 2:
				return MouseButton.Middle;
			case 3:
				return MouseButton.Right;
			case 4:
				return MouseButton.Button4;
			case 5:
				return MouseButton.Button5;
			}
			return MouseButton.LastButton;
		}



		public void Load(string path)
		{
			load(Interface.Load (path));
		}

		public void Load(Stream stream)
		{
			GraphicObject tmp = null;
			try {
				tmp = Interface.Load (stream, Interface.GetTopContainerOfGOMLStream (stream));
			} catch (Exception ex) {
				return;	
			} 
			load (tmp);
			QueueDraw ();
		}

		void load(GraphicObject go)
		{
			if (goWidget != null)
				DeleteWidget (goWidget);
			goWidget = go;
			this.AddWidget (goWidget);
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

		public List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		public Color Background = Color.Transparent;

		Rectangles _redrawClip = new Rectangles();//should find another way to access it from child
		List<GraphicObject> _gobjsToRedraw = new List<GraphicObject>();

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
				_focusedWidget.onFocused (this, null);
			}
		}

		#endregion

		Rectangle dirtyZone = Rectangle.Empty;

		#region Chrono's
		public Stopwatch updateTime = new Stopwatch ();
		public Stopwatch layoutTime = new Stopwatch ();
		public Stopwatch guTime = new Stopwatch ();
		public Stopwatch drawingTime = new Stopwatch ();
		#endregion

		#region update
		void update (Cairo.Context ctx)
		{
			updateTime.Restart ();
			layoutTime.Reset ();
			guTime.Reset ();
			drawingTime.Reset ();

			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList,0);
			invGOList = invGOList.Reverse ().ToArray ();

			Crow.Size newSize = this.ClientRectangle.Size;
			if (lastSize != newSize) {
				foreach (GraphicObject g in GraphicObjects)
					g.RegisterForLayouting ((int)LayoutingType.All);
				lastSize = newSize;
			}

			//Debug.WriteLine ("======= Layouting queue start =======");
			while (Interface.LayoutingQueue.Count > 0) {
				LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
				lqi.ProcessLayouting ();
			}

			//Debug.WriteLine ("otd:" + gobjsToRedraw.Count.ToString () + "-");
			//final redraw clips should be added only when layout is completed among parents,
			//that's why it take place in a second pass
			GraphicObject[] gotr = new GraphicObject[gobjsToRedraw.Count];
			gobjsToRedraw.CopyTo (gotr);
			gobjsToRedraw.Clear ();
			foreach (GraphicObject p in gotr) {
				p.registerClipRect ();
			}


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
					dirtyZone = redrawClip.Bounds;
					//					#if DEBUG_CLIP_RECTANGLE
					//					redrawClip.stroke (ctx, Color.Red.AdjustAlpha(0.1));
					//					#endif
					redrawClip.Reset ();
				}
			}
			//surf.WriteToPng (@"/mnt/data/test.png");

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
			updateTime.Stop ();
			//			Debug.WriteLine("UPDATE: {0} ticks \t, {1} ms",
			//				updateTime.ElapsedTicks,
			//				updateTime.ElapsedMilliseconds);
		}						
		#endregion
					

//		public void LoadInterface<T>(string path, out T result)
//		{
//			GraphicObject.Load<T> (path, out result, this);
//			AddWidget (result as GraphicObject);
//		}
//		public T LoadInterface<T> (string Path)
//		{
//			T result;
//			GraphicObject.Load<T> (Path, out result, this);
//			AddWidget (result as GraphicObject);
//			return result;
//		}



		public void PutOnTop(GraphicObject g)
		{
			if (GraphicObjects.IndexOf(g) > 0)
			{
				GraphicObjects.Remove(g);
				GraphicObjects.Insert(0, g);
			}
		}

		#region Mouse Handling
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
//			if (_activeWidget != null) {
//				//send move evt even if mouse move outside bounds
//				_activeWidget.onMouseMove (_activeWidget, e);
//				return;
//			}

			if (_hoverWidget != null) {
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
					while (_hoverWidget.Parent as GraphicObject!=null) {
						_hoverWidget = _hoverWidget.Parent as GraphicObject;
						if (_hoverWidget.MouseIsIn (e.Position)) {
							_hoverWidget.checkHoverWidget (e);
							return;
						} else
							_hoverWidget.onMouseLeave (this, e);
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
//			if (_activeWidget == null)
//				return;

			//_activeWidget.onMouseButtonUp (this, e);
			//_activeWidget = null;
			MouseButtonUp.Raise (this, e);
		}
		void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (_hoverWidget == null) {
				MouseButtonDown.Raise (this, e);
				return;
			}
				
			_activeWidget = _hoverWidget;
			//_activeWidget.onMouseButtonDown (this, e);
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

		public void Quit ()
		{
			throw new NotImplementedException ();
		}
		

		#region ILayoutable implementation

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

		public bool SizeIsValid {
			get { return true; }
			set { throw new NotImplementedException ();	}
		}

		public void RegisterForLayouting (int layoutType)
		{
			throw new NotImplementedException ();
		}


		public void UpdateLayout (LayoutingType layoutType)
		{
			throw new NotImplementedException ();
		}

		public bool PositionIsValid {
			get {
				return true;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public bool LayoutIsValid {
			get {
				return true;//tester tout les enfants a mon avis
			}
			set {
				throw new NotImplementedException ();
			}
		}
		Size lastSize;
		public Rectangle ClientRectangle {
			get {	
				Crow.Size newSize = new Crow.Size (Allocation.Width, Allocation.Height);
				return newSize;
			}
		}

		public IGOLibHost TopContainer {
			get { return this as IGOLibHost; }
		}

		public Rectangle getSlot ()
		{
			return ClientRectangle;
		}
		public Rectangle getBounds ()//redundant but fill ILayoutable implementation
		{
			return ClientRectangle;
		}

		public bool WIsValid {
			get {
				return true;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public bool HIsValid {
			get {
				return true;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public bool XIsValid {
			get {
				return true;
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public bool YIsValid {
			get {
				return true;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public virtual void InvalidateLayout ()
		{
			//			foreach (GraphicObject g in GraphicObjects) {
			//				g.InvalidateLayout ();
			//			}
		}

		#endregion
	}
}

