//
//  OpenTKGameWindow.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using System.Threading;
using System.Xml;
using Cairo;


namespace Crow
{
	public class OpenTKGameWindow : Gtk.Window, ILayoutable, IGOLibHost
    {
		#region ctor
		public OpenTKGameWindow(int _width, int _height, string _title="Crow")
			: base(Gtk.WindowType.Toplevel)
		{
			currentWindow = this;

			Decorated = false;
			this.AddEvents ((int)Gdk.EventMask.PointerMotionMask);
			this.DeleteEvent += Win_DeleteEvent;
			this.Drawn += Win_Drawn;
			this.OverrideBackgroundColor (Gtk.StateFlags.Normal, Gdk.RGBA.Zero);
			this.Visual = Gdk.Global.DefaultRootWindow.Screen.RgbaVisual;
			//			this.AcceptFocus = false;
			//			this.CanFocus = false;

			this.ButtonPressEvent += OpenTKGameWindow_ButtonPressEvent;
			this.ButtonReleaseEvent += OpenTKGameWindow_ButtonReleaseEvent;
			this.MotionNotifyEvent += OpenTKGameWindow_MotionNotifyEvent;
			this.KeyPressEvent += OpenTKGameWindow_KeyPressEvent;

			this.SizeAllocated += OpenTKGameWindow_SizeAllocated;

			this.Show ();
			this.Maximize ();

			OnLoad (null);
			GLib.Idle.Add (new GLib.IdleHandler (idleHandler));
			GLib.Timeout.Add (10, new GLib.TimeoutHandler (updateHandler));
			interval.Start ();
		}

		Stopwatch interval = new Stopwatch();
		bool updateHandler(){
			update();
			return true;
		}
		bool idleHandler(){
			if (isDirty && interval.ElapsedMilliseconds > 1) {
				Debug.WriteLine (interval.ElapsedTicks.ToString ());
				QueueDrawArea (DirtyRect.X, DirtyRect.Y, DirtyRect.Width, DirtyRect.Height);
				interval.Restart ();
			}
			return true;
		}
		#endregion

		public List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		public Color Background = Color.Transparent;

		internal static OpenTKGameWindow currentWindow;

		Rectangles _redrawClip = new Rectangles();//should find another way to access it from child
		List<GraphicObject> _gobjsToRedraw = new List<GraphicObject>();

		#region IGOLibHost implementation
		public Rectangles clipping {
			get { return _redrawClip; }
			set { _redrawClip = value; }
		}
		public XCursor MouseCursor {
			set {
//				if (value == null) {
//					Cursor = null;
//					return;
//				}
//				Cursor = new MouseCursor
//					((int)value.Xhot, (int)value.Yhot, (int)value.Width, (int)value.Height,value.data);
			}
		}
		public List<GraphicObject> gobjsToRedraw {
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
		public void Quit ()
		{
			Gtk.Application.Quit ();
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

		#endregion

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
		#region Events
		//those events are raised only if mouse isn't in a graphic object
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		public event EventHandler<KeyboardKeyEventArgs> KeyboardKeyDown;
		#endregion

		#region graphic contexte
		Context ctx;
		Surface surf;
		byte[] bmp;

		void Win_Drawn (object o, Gtk.DrawnArgs args)
		{
			if (isDirty) {
				byte[] tmp = new byte[4 * DirtyRect.Width * DirtyRect.Height];
				for (int y = 0; y < DirtyRect.Height; y++) {
					Array.Copy(bmp,
						((DirtyRect.Top + y) * ClientRectangle.Width * 4) + DirtyRect.Left * 4,
						tmp, y * DirtyRect.Width * 4, DirtyRect.Width *4);
				}
				using (ImageSurface img = new ImageSurface (tmp, Format.Argb32, DirtyRect.Width, DirtyRect.Height, 4 * DirtyRect.Width)) {
					args.Cr.SetSourceSurface (img, DirtyRect.X, DirtyRect.Y);
					args.Cr.Paint();
				}

				isDirty = false;
				return;
			}
			if (bmp == null)
				return;
			using (ImageSurface img = new ImageSurface (bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height, 4 * ClientRectangle.Width)) {
				args.Cr.SetSourceSurface (img, ClientRectangle.X, ClientRectangle.Y);
				args.Cr.Paint();
			}
		}

		void Win_DeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			Gtk.Application.Quit ();
		}


		void OpenTKGameWindow_SizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			int stride = 4 * ClientRectangle.Width;
			int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
			bmp = new byte[bmpSize];

			foreach (GraphicObject g in GraphicObjects)
				g.RegisterForLayouting (LayoutingType.All);

			//_redrawClip.AddRectangle (ClientRectangle);
		}
		#endregion

		protected virtual void OnLoad (EventArgs e){
			Interface.LoadCursors ();
		}

		#if MEASURE_TIME
		public Stopwatch updateTime = new Stopwatch ();
		public Stopwatch layoutTime = new Stopwatch ();
		public Stopwatch guTime = new Stopwatch ();
		public Stopwatch drawingTime = new Stopwatch ();
		#endif

		bool isDirty = false;
		Rectangle DirtyRect;

		#region update
		void update ()
		{
			if (mouseRepeatCount > 0) {
				int mc = mouseRepeatCount;
				mouseRepeatCount -= mc;
				for (int i = 0; i < mc; i++) {
					//FocusedWidget.onMouseClick (this, new MouseButtonEventArgs (Mouse.X, Mouse.Y, MouseButton.Left, true));
				}
			}
			#if MEASURE_TIME
			layoutTime.Reset ();
			guTime.Reset ();
			drawingTime.Reset ();
			updateTime.Restart ();
			#endif

			#if MEASURE_TIME
			layoutTime.Start ();
			#endif
			//Debug.WriteLine ("======= Layouting queue start =======");

			while (Interface.LayoutingQueue.Count > 0) {
				LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
				lqi.ProcessLayouting ();
			}

			#if MEASURE_TIME
			layoutTime.Stop ();
			#endif

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

			#if MEASURE_TIME
			updateTime.Stop ();
			drawingTime.Start ();
			#endif

			using (surf = new ImageSurface (bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height, ClientRectangle.Width * 4)) {
				using (ctx = new Context (surf)){


					if (clipping.count > 0) {
						//Link.draw (ctx);
						clipping.clearAndClip(ctx);

						GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
						GraphicObjects.CopyTo (invGOList, 0);
						invGOList = invGOList.Reverse ().ToArray ();

						foreach (GraphicObject p in invGOList) {
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

						if (isDirty)
							DirtyRect += clipping.Bounds;
						else
							DirtyRect = clipping.Bounds;
						isDirty = true;

						DirtyRect.Left = Math.Max (0, DirtyRect.Left);
						DirtyRect.Top = Math.Max (0, DirtyRect.Top);
						DirtyRect.Width = Math.Min (ClientRectangle.Width - DirtyRect.Left, DirtyRect.Width);
						DirtyRect.Height = Math.Min (ClientRectangle.Height - DirtyRect.Top, DirtyRect.Height);

						clipping.Reset ();
					}

					#if MEASURE_TIME
					drawingTime.Stop ();
					#endif
					//surf.WriteToPng (@"/mnt/data/test.png");
				}
			}
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
		#endregion

		#region loading
		public GraphicObject LoadInterface (string path)
		{
			GraphicObject tmp = Interface.Load (path, this);
			AddWidget (tmp);
			return tmp;
		}
		#endregion


		MouseState mouse;
		KeyboardState Keyboard;

		void OpenTKGameWindow_MotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			int deltaX = (int)args.Event.X - mouse.X;
			int deltaY = (int)args.Event.Y - mouse.Y;
			MouseMoveEventArgs e = new MouseMoveEventArgs ((int)args.Event.X, (int)args.Event.Y, deltaX, deltaY);
			mouse.X = (int)args.Event.X;
			mouse.Y = (int)args.Event.Y;
			e.Mouse = mouse;

			if (_activeWidget != null) {
				//first, ensure object is still in the graphic tree
				if (_activeWidget.HostContainer == null) {
					activeWidget = null;
				} else {

					//send move evt even if mouse move outside bounds
					_activeWidget.onMouseMove (this, e);
					return;
				}
			}

			if (hoverWidget != null) {
				//first, ensure object is still in the graphic tree
				if (hoverWidget.HostContainer == null) {
					hoverWidget = null;
				} else {
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
								return;
							}
							i++;
						}
					}


					if (hoverWidget.MouseIsIn (e.Position)) {
						hoverWidget.checkHoverWidget (e);
						return;
					} else {
						hoverWidget.onMouseLeave (this, e);
						//seek upward from last focused graph obj's
						while (hoverWidget.Parent as GraphicObject != null) {
							hoverWidget = hoverWidget.Parent as GraphicObject;
							if (hoverWidget.MouseIsIn (e.Position)) {
								hoverWidget.checkHoverWidget (e);
								return;
							} else
								hoverWidget.onMouseLeave (this, e);
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
			hoverWidget = null;
			//MouseMove.Raise (this, otk_e);
	}

		void OpenTKGameWindow_ButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			MouseButtonEventArgs e = new MouseButtonEventArgs ((int)args.Event.X, (int)args.Event.Y, (Crow.MouseButton)args.Event.Button-1, false);
			mouse.DisableBit ((int)args.Event.Button-1);
			e.Mouse = mouse;

			if (_activeWidget == null) {
				//MouseButtonUp.Raise (this, otk_e);
				return;
			}

			if (mouseRepeatThread != null) {
				mouseRepeatOn = false;
				mouseRepeatThread.Abort();
				mouseRepeatThread.Join ();
			}

			_activeWidget.onMouseUp (this, e);
			activeWidget = null;
		}
		void OpenTKGameWindow_ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			MouseButtonEventArgs e = new MouseButtonEventArgs ((int)args.Event.X, (int)args.Event.Y, (Crow.MouseButton)args.Event.Button - 1, true);
			mouse.EnableBit ((int)args.Event.Button - 1);
			e.Mouse = mouse;

			if (hoverWidget == null) {
				//MouseButtonDown.Raise (this, otk_e);
				return;
			}

			hoverWidget.onMouseDown(hoverWidget,new BubblingMouseButtonEventArg(e));

			if (FocusedWidget == null)
				return;
			if (!FocusedWidget.MouseRepeat)
				return;
			mouseRepeatThread = new Thread (mouseRepeatThreadFunc);
			mouseRepeatThread.Start ();
		}

//		void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
//        {
//			MouseWheelEventArgs e = new MouseWheelEventArgs (otk_e.X, otk_e.Y, otk_e.Value, otk_e.Delta);
//			MouseState ms = e.Mouse;
//			update_mouseButtonStates (ref ms, otk_e.Mouse);
//			e.Mouse = ms;
//
//			if (hoverWidget == null) {
//				MouseWheelChanged.Raise (this, otk_e);
//				return;
//			}
//			hoverWidget.onMouseWheel (this, e);
//        }

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


	#region keyboard Handling
//		KeyboardState Keyboad = new KeyboardState ();
//		void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
//	{
////			if (_focusedWidget == null) {
//				KeyboardKeyDown.Raise (this, otk_e);
////				return;
////			}
//			Keyboad.SetKeyState ((Crow.Key)otk_e.Key, true);
//			KeyboardKeyEventArgs e = new KeyboardKeyEventArgs((Crow.Key)otk_e.Key, otk_e.IsRepeat,Keyboad);
//			_focusedWidget.onKeyDown (sender, e);
//        }
		void OpenTKGameWindow_KeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (_focusedWidget == null) {
//				KeyboardKeyDown.Raise (this, otk_e);
				return;
			}
			Keyboard.SetKeyState ((Crow.Key)args.Event.KeyValue, true);
			KeyboardKeyEventArgs e = new KeyboardKeyEventArgs((Crow.Key)args.Event.KeyValue, false, Keyboard);
			_focusedWidget.onKeyDown (o, e);
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
			get {
				int width, height;
				this.GetSize (out width, out height);
				return new Size(width, height);
			}
		}
		public IGOLibHost HostContainer {
			get { return this; }
		}
		public Rectangle getSlot () => ClientRectangle;
		public Rectangle getBounds () => ClientRectangle;
		#endregion
    }
}
