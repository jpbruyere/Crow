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
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Crow
{
	public class OpenTKGameWindow : GameWindow, ILayoutable, IGOLibHost
    {
		#region ctor
//		public OpenTKGameWindow(int _width, int _height, string _title="golib")
//			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 1), _title,
//				GameWindowFlags.Fullscreen,
//				DisplayDevice.Default,
//				3,0,OpenTK.Graphics.GraphicsContextFlags.Default)
		public OpenTKGameWindow(int _width, int _height, string _title="Crow")
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 1),
				_title,GameWindowFlags.Default,DisplayDevice.GetDisplay(DisplayIndex.Second),
				3,3,OpenTK.Graphics.GraphicsContextFlags.Debug)
//		public OpenTKGameWindow(int _width, int _height, string _title="golib")
//			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8), _title)
		{
			//VSync = VSyncMode.On;
			currentWindow = this;
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
				if (value == null) {
					Cursor = null;
					return;
				}
				Cursor = new MouseCursor
					((int)value.Xhot, (int)value.Yhot, (int)value.Width, (int)value.Height,value.data);; }
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
			this.Exit ();
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
		public event EventHandler<OpenTK.Input.MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseClick;
		public event EventHandler<OpenTK.Input.MouseMoveEventArgs> MouseMove;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> KeyboardKeyDown;
		#endregion

		#region graphic contexte
		Context ctx;
		Surface surf;
		byte[] bmp;
		int texID;

		public QuadVAO uiQuad, uiQuad2;
		Crow.Shader shader;
		int[] viewport = new int[4];

		void createContext()
		{
			createOpenGLSurface ();

			if (uiQuad != null)
				uiQuad.Dispose ();
			uiQuad = new QuadVAO (0, 0, ClientRectangle.Width, ClientRectangle.Height, 0, 1, 1, -1);
			uiQuad2 = new QuadVAO (0, 0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1, 1);

			shader.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter
				(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);

			clipping.AddRectangle (ClientRectangle);
		}
		void createOpenGLSurface()
		{
			currentWindow = this;

			int stride = 4 * ClientRectangle.Width;
			int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
			bmp = new byte[bmpSize];

			//create texture
			if (GL.IsTexture(texID))
				GL.DeleteTexture (texID);
			GL.GenTextures(1, out texID);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texID);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				ClientRectangle.Width, ClientRectangle.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			shader.Texture = texID;
		}
		void OpenGLDraw()
		{
			GL.GetInteger (GetPName.Viewport, viewport);
			GL.Viewport (0, 0, ClientRectangle.Width, ClientRectangle.Height);

			shader.Enable ();

			if (isDirty) {
				byte[] tmp = new byte[4 * DirtyRect.Width * DirtyRect.Height];
				for (int y = 0; y < DirtyRect.Height; y++) {
					Array.Copy(bmp,
						((DirtyRect.Top + y) * ClientRectangle.Width * 4) + DirtyRect.Left * 4,
						tmp, y * DirtyRect.Width * 4, DirtyRect.Width *4);
				}
				GL.TexSubImage2D (TextureTarget.Texture2D, 0,
					DirtyRect.Left, DirtyRect.Top, DirtyRect.Width, DirtyRect.Height,
					OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, tmp);
				isDirty = false;
			}
			uiQuad.Render (PrimitiveType.TriangleStrip);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			shader.Disable ();
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);
		}
		#endregion

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
					FocusedWidget.onMouseClick (this, new MouseButtonEventArgs (Mouse.X, Mouse.Y, MouseButton.Left, true));
				}
			}
			#if MEASURE_TIME
			layoutTime.Reset ();
			guTime.Reset ();
			drawingTime.Reset ();
			updateTime.Restart ();
			#endif

			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList, 0);
			invGOList = invGOList.Reverse ().ToArray ();

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

		public virtual void OnRender(FrameEventArgs e)
		{
		}
		public virtual void GLClear()
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
		}

		#region Game win overrides
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			update ();
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GLClear ();


			base.OnRenderFrame(e);

			OnRender (e);
			OpenGLDraw ();


			SwapBuffers ();
		}
		protected override void OnLoad(EventArgs e)
	{
	    base.OnLoad(e);

			Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.WheelChanged += new EventHandler<OpenTK.Input.MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.ButtonDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(Mouse_Move);

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

			Console.WriteLine("\n\n*************************************");
			Console.WriteLine("GL version: " + GL.GetString (StringName.Version));
			Console.WriteLine("GL vendor: " + GL.GetString (StringName.Vendor));
			Console.WriteLine("GLSL version: " + GL.GetString (StringName.ShadingLanguageVersion));
			Console.WriteLine("*************************************\n");

			shader = new Crow.TexturedShader ();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize (e);
			createContext ();
			foreach (GraphicObject g in GraphicObjects)
				g.RegisterForLayouting (LayoutingType.All);
		}
		#endregion

	#region Mouse Handling
		void update_mouseButtonStates(ref MouseState e, OpenTK.Input.MouseState otk_e){
			for (int i = 0; i < MouseState.MaxButtons; i++) {
				if (otk_e.IsButtonDown ((OpenTK.Input.MouseButton)i))
					e.EnableBit (i);
			}
		}
		void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
	{
			MouseMoveEventArgs e = new MouseMoveEventArgs (otk_e.X, otk_e.Y, otk_e.XDelta, otk_e.YDelta);
			MouseState ms = e.Mouse;
			update_mouseButtonStates (ref ms, otk_e.Mouse);
			e.Mouse = ms;

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
			MouseMove.Raise (this, otk_e);
	}
		void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
	{
			MouseButtonEventArgs e = new MouseButtonEventArgs (otk_e.X, otk_e.Y, (Crow.MouseButton)otk_e.Button, otk_e.IsPressed);
			MouseState ms = e.Mouse;
			update_mouseButtonStates (ref ms, otk_e.Mouse);
			e.Mouse = ms;

			if (_activeWidget == null) {
				MouseButtonUp.Raise (this, otk_e);
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
		void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			MouseButtonEventArgs e = new MouseButtonEventArgs (otk_e.X, otk_e.Y, (Crow.MouseButton)otk_e.Button, otk_e.IsPressed);
			MouseState ms = e.Mouse;
			update_mouseButtonStates (ref ms, otk_e.Mouse);
			e.Mouse = ms;

			if (hoverWidget == null) {
				MouseButtonDown.Raise (this, otk_e);
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
		void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
	{
			MouseWheelEventArgs e = new MouseWheelEventArgs (otk_e.X, otk_e.Y, otk_e.Value, otk_e.Delta);
			MouseState ms = e.Mouse;
			update_mouseButtonStates (ref ms, otk_e.Mouse);
			e.Mouse = ms;

			if (hoverWidget == null) {
				MouseWheelChanged.Raise (this, otk_e);
				return;
			}
			hoverWidget.onMouseWheel (this, e);
	}

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

	#region keyboard Handling
		KeyboardState Keyboad = new KeyboardState ();
		void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
	{
//			if (_focusedWidget == null) {
				KeyboardKeyDown.Raise (this, otk_e);
//				return;
//			}
			Keyboad.SetKeyState ((Crow.Key)otk_e.Key, true);
			KeyboardKeyEventArgs e = new KeyboardKeyEventArgs((Crow.Key)otk_e.Key, otk_e.IsRepeat,Keyboad);
			_focusedWidget.onKeyDown (sender, e);
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

		Rectangle ILayoutable.ClientRectangle {
			get { return new Size(this.ClientRectangle.Size.Width,this.ClientRectangle.Size.Height); }
		}
		public IGOLibHost HostContainer {
			get { return this; }
		}
		public Rectangle getSlot () => ClientRectangle;
		public Rectangle getBounds () => ClientRectangle;
		#endregion
    }
}