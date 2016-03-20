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
using Crow;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.Crow
{
	public class XwtContainer : Xwt.Canvas, IPropertyPadProvider,ICommandDelegator
    {
		public Interface CrowInterface;

		#region ICommandDelegator implementation

		public object GetDelegatedCommandTarget ()
		{
			return CrowInterface.hoverWidget;
		}

		#endregion

		#region IPropertyPadProvider implementation

		public object GetActiveComponent ()
		{
			return CrowInterface.activeWidget;
		}
		public object GetProvider ()
		{
			return CrowInterface.activeWidget;
		}
		public void OnEndEditing (object obj)
		{

		}
		public void OnChanged (object obj)
		{
			(obj as GraphicObject).RegisterForGraphicUpdate ();
			QueueDraw ();
		}
		#endregion

		#region ctor
		public XwtContainer() : this(0,0,"test"){}
		public XwtContainer(int _width, int _height, string _title="Crow")
			: base()
		{
			CrowInterface = new Interface ();
			CrowInterface.Quit += Quit;


			//this.OverrideBackgroundColor (Gtk.StateFlags.Normal, Gdk.RGBA.Zero);
			//this.Visual = Gdk.Global.DefaultRootWindow.Screen.RgbaVisual;

			this.ButtonPressEvent += OpenTKGameWindow_ButtonPressEvent;
			this.ButtonReleaseEvent += OpenTKGameWindow_ButtonReleaseEvent;
			this.MotionNotifyEvent += OpenTKGameWindow_MotionNotifyEvent;
			this.KeyPressEvent += OpenTKGameWindow_KeyPressEvent;
			this.SizeAllocated += OpenTKGameWindow_SizeAllocated;

			this.Show ();

			//GLib.Idle.Add (new GLib.IdleHandler (idleHandler));
			GLib.Timeout.Add (10, new GLib.TimeoutHandler (updateHandler));
		}
		#endregion



		#region Timers
		bool updateHandler(){
			CrowInterface.Update();
			return true;
		}
		bool idleHandler(){
			if (CrowInterface.IsDirty) {
				QueueDrawArea (CrowInterface.DirtyRect.X, CrowInterface.DirtyRect.Y, CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height);
				return false;
			}
			return true;
		}
		#endregion

		protected void Quit (object sender, EventArgs e)
		{
			Gtk.Application.Quit ();
		}
			
		static double[] dashed = {2.0};
		protected override void OnDraw (Xwt.Drawing.Context ctx, Xwt.Rectangle dirtyRect)
		{
			base.OnDraw (ctx, dirtyRect);
			if (CrowInterface.IsDirty) {
				using (MemoryStream ms = new MemoryStream (CrowInterface.dirtyBmp)) {
					//Xwt.Drawing.ImageBuilder im = new Xwt.Drawing.ImageBuilder(CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height);
					//bmp.
					using (Xwt.Drawing.Image img = Xwt.Drawing.BitmapImage.FromStream (ms)) {
						img.WithSize (CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height);
						ctx.DrawImage (img, CrowInterface.DirtyRect.X, CrowInterface.DirtyRect.Y);
						ctx.Fill ();
					}
				}
				CrowInterface.IsDirty = false;
				GLib.Idle.Add (new GLib.IdleHandler (idleHandler));
				return;
			}
			if (CrowInterface.bmp != null) {
				using (ImageSurface img = new ImageSurface (CrowInterface.bmp, Format.Argb32, CrowInterface.ClientRectangle.Width, CrowInterface.ClientRectangle.Height, 4 * CrowInterface.ClientRectangle.Width)) {
					cr.SetSourceSurface (img, CrowInterface.ClientRectangle.X, CrowInterface.ClientRectangle.Y);
					cr.Paint ();
				}
			}
			if (CrowInterface.hoverWidget != null) {
				cr.Rectangle (CrowInterface.hoverWidget.ScreenCoordinates (
					CrowInterface.hoverWidget.getSlot ()));
				cr.LineWidth = 1;
				cr.SetDash (dashed, 0);
				//cr.Color = Crow.Color.Yellow;
				cr.Stroke ();
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
