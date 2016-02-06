// Released to the public domain. Use, modify and relicense at will.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Cairo;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;



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
				g.registerClipRect ();
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

			redrawClip.AddRectangle (ClientRectangle);
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

			GL.TexSubImage2D (TextureTarget.Texture2D, 0,
				0, 0, ClientRectangle.Width, ClientRectangle.Height,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp);

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

			surf = new ImageSurface(bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height,ClientRectangle.Width*4);
			ctx = new Context(surf);

			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList,0);
			invGOList = invGOList.Reverse ().ToArray ();

			#if MEASURE_TIME
			layoutTime.Start ();
			#endif
			//Debug.WriteLine ("======= Layouting queue start =======");

			while (Interface.RegisteredGOForLayouting.Count > 0){
				while (Interface.RegisteredGOForLayouting.Count > 0){
					GraphicObject go = Interface.RegisteredGOForLayouting.Dequeue ();
					go.EnqueueForLayouting ();
					go.RegisteredLayoutings = LayoutingType.None;
				}
				while (Interface.LayoutingQueue.First != null) {
					LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
					lqi.ProcessLayouting ();
				}
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
				p.registerClipRect ();
			}

			#if MEASURE_TIME
			updateTime.Stop ();
			drawingTime.Start ();
			#endif

			lock (redrawClip) {
				if (redrawClip.count > 0) {					
					#if DEBUG_CLIP_RECTANGLE
					redrawClip.stroke (ctx, new Color(1.0,0,0,0.3));
					#endif
					redrawClip.clearAndClip (ctx);	

					//Link.draw (ctx);
					foreach (GraphicObject p in invGOList) {
						if (p.Visible) {

							ctx.Save ();
							if (redrawClip.count > 0) {
								Rectangles clip = redrawClip.intersectingRects (p.Slot);

								if (clip.count > 0)
									p.Paint (ref ctx, clip);
							}
							ctx.Restore ();
						}
					}
					ctx.ResetClip ();
					#if DEBUG_CLIP_RECTANGLE
					redrawClip.stroke (ctx, Color.Red.AdjustAlpha(0.1));
					#endif
					redrawClip.Reset ();
				}
			}
			#if MEASURE_TIME
			drawingTime.Stop ();
			#endif
			//surf.WriteToPng (@"/mnt/data/test.png");
			ctx.Dispose ();
			surf.Dispose ();
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

			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);

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
			foreach (GraphicObject g in GraphicObjects) {
				g.RegisterForLayouting (LayoutingType.All);
				//g.registerForGraphicUpdate();
			}
		}
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
				
			if (mouseRepeatThread != null) {
				mouseRepeatOn = false;
				mouseRepeatThread.Abort();
				mouseRepeatThread.Join ();
			}

			_activeWidget.onMouseUp (this, e);
			activeWidget = null;
        }
        void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
			if (_hoverWidget == null) {
				MouseButtonDown.Raise (this, e);
				return;
			}

			_hoverWidget.onMouseDown(_hoverWidget,new BubblingMouseButtonEventArg(e));

			if (FocusedWidget == null)
				return;
			if (!FocusedWidget.MouseRepeat)
				return;
			mouseRepeatThread = new Thread (mouseRepeatThreadFunc);
			mouseRepeatThread.Start ();
        }
        void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
        {
			if (_hoverWidget == null) {
				MouseWheelChanged.Raise (this, e);
				return;
			}
			_hoverWidget.onMouseWheel (this, e);
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
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {			
			if (_focusedWidget == null)
				return;
			_focusedWidget.onKeyDown (sender, e);
        }
        #endregion

		#region ILayoutable implementation

		//TODO:uneeded list, should be removed
		public List<LinkedListNode<LayoutingQueueItem>> RegisteredLQINodes { get; } = new List<LinkedListNode<LayoutingQueueItem>>();
		public void RegisterForLayouting (LayoutingType layoutType) { throw new NotImplementedException (); }
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