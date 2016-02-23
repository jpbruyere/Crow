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
	public class OpenTKGameWindow : Gtk.Window
    {
		public Interface CrowInterface;


		#region ctor
		public OpenTKGameWindow(int _width, int _height, string _title="Crow")
			: base(Gtk.WindowType.Toplevel)
		{
			CrowInterface = new Interface ();
			CrowInterface.Quit += Quit;


			Decorated = false;
			this.AddEvents ((int)Gdk.EventMask.PointerMotionMask);
			this.DeleteEvent += Win_DeleteEvent;
			this.Drawn += Win_Drawn;
			this.OverrideBackgroundColor (Gtk.StateFlags.Normal, Gdk.RGBA.Zero);
			this.Visual = Gdk.Global.DefaultRootWindow.Screen.RgbaVisual;

			this.ButtonPressEvent += OpenTKGameWindow_ButtonPressEvent;
			this.ButtonReleaseEvent += OpenTKGameWindow_ButtonReleaseEvent;
			this.MotionNotifyEvent += OpenTKGameWindow_MotionNotifyEvent;
			this.KeyPressEvent += OpenTKGameWindow_KeyPressEvent;
			this.SizeAllocated += OpenTKGameWindow_SizeAllocated;

			this.Show ();
			this.Maximize ();

			GLib.Idle.Add (new GLib.IdleHandler (idleHandler));
			GLib.Timeout.Add (10, new GLib.TimeoutHandler (updateHandler));
			interval.Start ();
		}
		#endregion



		#region Timers
		Stopwatch interval = new Stopwatch();
		bool updateHandler(){
			CrowInterface.Update();
			return true;
		}
		bool idleHandler(){
			if (CrowInterface.IsDirty && interval.ElapsedMilliseconds > 1) {
				Debug.WriteLine (interval.ElapsedTicks.ToString ());
				QueueDrawArea (CrowInterface.DirtyRect.X, CrowInterface.DirtyRect.Y, CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height);
				interval.Restart ();
			}
			return true;
		}
		#endregion

<<<<<<< 70bcdc77ff42c74fcd8e6e7dffd4eb6b16f24f39
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

=======
		protected void Quit (object sender, EventArgs e)
		{
			Gtk.Application.Quit ();
		}
>>>>>>> CrowInterface object holding common functions, IGOLibHost removed
		void Win_Drawn (object o, Gtk.DrawnArgs args)
		{
			if (CrowInterface.IsDirty) {
				byte[] tmp = new byte[4 * CrowInterface.DirtyRect.Width * CrowInterface.DirtyRect.Height];
				for (int y = 0; y < CrowInterface.DirtyRect.Height; y++) {
					Array.Copy(CrowInterface.bmp,
						((CrowInterface.DirtyRect.Top + y) * CrowInterface.ClientRectangle.Width * 4) + CrowInterface.DirtyRect.Left * 4,
						tmp, y * CrowInterface.DirtyRect.Width * 4, CrowInterface.DirtyRect.Width *4);
				}
				using (ImageSurface img = new ImageSurface (tmp, Format.Argb32, CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height, 4 * CrowInterface.DirtyRect.Width)) {
					args.Cr.SetSourceSurface (img, CrowInterface.DirtyRect.X, CrowInterface.DirtyRect.Y);
					args.Cr.Paint();
				}

				CrowInterface.IsDirty = false;
				return;
			}
			if (CrowInterface.bmp == null)
				return;
			using (ImageSurface img = new ImageSurface (CrowInterface.bmp, Format.Argb32, CrowInterface.ClientRectangle.Width, CrowInterface.ClientRectangle.Height, 4 * CrowInterface.ClientRectangle.Width)) {
				args.Cr.SetSourceSurface (img, CrowInterface.ClientRectangle.X, CrowInterface.ClientRectangle.Y);
				args.Cr.Paint();
			}
		}
		void Win_DeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			Gtk.Application.Quit ();
		}
		void OpenTKGameWindow_SizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			CrowInterface.ProcessResize (new Rectangle (args.Allocation.X, args.Allocation.Y, args.Allocation.Width, args.Allocation.Height));
		}        
		void OpenTKGameWindow_MotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			CrowInterface.ProcessMouseMove ((int)args.Event.X, (int)args.Event.Y);
        }

		void OpenTKGameWindow_ButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			CrowInterface.ProcessMouseButtonUp ((int)args.Event.Button - 1);
		}
		void OpenTKGameWindow_ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			CrowInterface.ProcessMouseButtonDown ((int)args.Event.Button - 1);
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

		}

	#endregion
    }
}
