// Released to the public domain. Use, modify and relicense at will.
//#define DEBUG_CLIP_RECTANGLE

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
//using System.IO;
//using GLU = OpenTK.Graphics.Glu;
using Cairo;
using System.Threading;

using System.Drawing.Imaging;
//using System.Xml.Serialization;
//using System.Reflection;



namespace go
{
	public class OpenTKGameWindow : GameWindow, ILayoutable
    {
		#region ctor
//		public OpenTKGameWindow(int _width, int _height, string _title="golib")
//			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 1), _title,
//				GameWindowFlags.Fullscreen,
//				DisplayDevice.Default,
//				3,0,OpenTK.Graphics.GraphicsContextFlags.Default)
		public OpenTKGameWindow(int _width, int _height, string _title="golib")
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 1), _title)
		{
			VSync = VSyncMode.On;
		}        
		#endregion

		#if _WIN32 || _WIN64
		public const string rootDir = @"d:\";
		#elif __linux__
		public const string rootDir = @"/mnt/data/";
		#endif

		public List<GraphicObject> GraphicObjects = new List<GraphicObject>();
		public Color Background = Color.Transparent;

		public static Rectangles redrawClip = new Rectangles();//should find another way to access it from child
		public static List<GraphicObject> gobjsToRedraw = new List<GraphicObject>();
		internal static OpenTKGameWindow currentWindow;

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

		#region graphic contexte
		Context ctx;
		Surface surf;
		byte[] bmp;
		int texID;
		int dispList;
		Rectangle dirtyZone = Rectangle.Empty;

		void createOpenGLSurface()
		{
			currentWindow = this;

			int stride = 4 * ClientRectangle.Width;
			int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
			bmp = new byte[bmpSize];

			if (dispList > 0)
				GL.DeleteLists (dispList, 1);

			//create texture
			if (texID > 0)
				GL.DeleteTexture (texID);
			GL.GenTextures(1, out texID);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texID);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
				ClientRectangle.Width, ClientRectangle.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		void OpenGLDraw()
		{
			//if (dirtyZone != Rectangle.Empty) {
				GL.BindTexture (TextureTarget.Texture2D, texID);
				GL.TexSubImage2D (TextureTarget.Texture2D, 0,
					0, 0, ClientRectangle.Width, ClientRectangle.Height,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp);
//				GL.TexSubImage2D (TextureTarget.Texture2D, 0,
//					dirtyZone.X, dirtyZone.Y, ClientRectangle.Width, dirtyZone.Height,
//					OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp);
			//				dirtyZone = Rectangle.Empty;
//				if (dispList > 0)
//					GL.DeleteLists (dispList, 1);
				//surf.WriteToPng (@"/home/jp/test.png");
			//}

//			if (dispList > 0) {
//				GL.CallList (dispList);
//				return;
//			}
//
//			dispList = GL.GenLists (1);
//			GL.NewList (dispList, ListMode.CompileAndExecute);
//			{
			GL.Viewport(0, 0, ClientRectangle.Width, ClientRectangle.Height);
				GL.PushAttrib (AttribMask.EnableBit);
				GL.Color4 (Color.White);
				GL.ActiveTexture (TextureUnit.Texture0);
				GL.BindTexture (TextureTarget.Texture2D, texID);
				GL.Enable (EnableCap.Texture2D);

				GL.MatrixMode (MatrixMode.Modelview);

				GL.PushMatrix ();
				GL.LoadIdentity ();

				GL.MatrixMode (MatrixMode.Projection);

				GL.PushMatrix ();
				GL.LoadIdentity ();

				Matrix4 ortho2D = Matrix4.CreateOrthographicOffCenter 
					(ClientRectangle.Left, ClientRectangle.Right, ClientRectangle.Bottom, ClientRectangle.Top, 0, 1);
				GL.LoadMatrix (ref ortho2D);

				GL.Disable (EnableCap.Lighting);
				GL.Enable (EnableCap.AlphaTest);
				GL.AlphaFunc (AlphaFunction.Greater, 0.0f);
				GL.Enable (EnableCap.Blend);
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

				GL.Begin (PrimitiveType.Quads);
				{
					GL.TexCoord2 (0, 0);
					GL.Vertex2 (ClientRectangle.Left, ClientRectangle.Top);
					GL.TexCoord2 (0, 1);
					GL.Vertex2 (ClientRectangle.Left, ClientRectangle.Bottom);
					GL.TexCoord2 (1, 1);
					GL.Vertex2 (ClientRectangle.Right, ClientRectangle.Bottom);
					GL.TexCoord2 (1, 0);
					GL.Vertex2 (ClientRectangle.Right, ClientRectangle.Top);
				}
				GL.End ();

				GL.PopMatrix ();

				GL.MatrixMode (MatrixMode.Modelview);
				GL.PopMatrix ();

				GL.PopAttrib ();

//			}
//			GL.EndList ();

		}
			
		#endregion

		public Stopwatch updateTime = new Stopwatch ();
		public Stopwatch layoutTime = new Stopwatch ();
		public Stopwatch guTime = new Stopwatch ();
		public Stopwatch drawingTime = new Stopwatch ();

		void update ()
		{
			updateTime.Restart ();
			layoutTime.Reset ();
			guTime.Reset ();
			drawingTime.Reset ();

			surf = new ImageSurface(bmp, Format.Argb32, ClientRectangle.Width, ClientRectangle.Height,ClientRectangle.Width*4);
			ctx = new Context(surf);


			GraphicObject[] invGOList = new GraphicObject[GraphicObjects.Count];
			GraphicObjects.CopyTo (invGOList,0);
			invGOList = invGOList.Reverse ().ToArray ();

			foreach (GraphicObject p in invGOList) {
				if (p.Visible) {
					layoutTime.Start ();
					while(!p.LayoutIsValid)
						p.UpdateLayout ();
					layoutTime.Stop ();
				}
			}

			Debug.WriteLine ("otd:" + gobjsToRedraw.Count.ToString () + "-");
			//redraw clip should be added when layout is complete among parents,
			//that's why it take place in a second pass
			foreach (GraphicObject p in gobjsToRedraw) {
				p.registerClipRect ();
			}
			gobjsToRedraw.Clear ();

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
								Rectangles clip = redrawClip.intersectingRects (p.ContextCoordinates(p.Slot.Size));

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
			updateTime.Stop ();
//			Debug.WriteLine("UPDATE: {0} ticks \t, {1} ms",
//				updateTime.ElapsedTicks,
//				updateTime.ElapsedMilliseconds);

		}						

		public void AddWidget(GraphicObject g)
		{
			g.Parent = this;
			GraphicObjects.Add (g);
		}
		public void LoadInterface<T>(string path, out T result)
		{
			GraphicObject.Load<T> (path, out result, this);
			AddWidget (result as GraphicObject);
		}

		#region Game win overrides
		protected override void OnUpdateFrame(FrameEventArgs e)
		{	
			base.OnUpdateFrame(e);
			update ();
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			if (recreateContext) {
				createOpenGLSurface ();
				redrawClip.AddRectangle (ClientRectangle);
				recreateContext = false;
			}
			OpenGLDraw ();
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

			createOpenGLSurface ();
        }
        protected override void OnUnload(EventArgs e)
        {
			if (dispList > 0)
				GL.DeleteLists (dispList, 1);
			if (texID > 0)
				GL.DeleteTexture (texID);
			//ctx.Dispose ();
			surf.Dispose ();
        }
		bool recreateContext=true;
        protected override void OnResize(EventArgs e)
		{
			base.OnResize (e);
			recreateContext = true;
		}
		#endregion

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

            go.Mouse.Position = e.Position;

			if (_activeWidget != null) {
				//send move evt even if mouse move outside bounds
				_activeWidget.onMouseMove (this, e);
				return;
			}

			if (_hoverWidget != null) {
				if (_hoverWidget.MouseIsIn (e.Position)) {
					_hoverWidget.onMouseMove (this, e);
					return;
				} else {
					_hoverWidget.onMouseLeave (this, e);
					//seek upward from last focused graph obj's
					while (_hoverWidget.Parent as GraphicObject!=null) {
						_hoverWidget = _hoverWidget.Parent as GraphicObject;
						if (_hoverWidget.MouseIsIn (e.Position)) {
							_hoverWidget.onMouseMove (this, e);
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
					g.onMouseMove (this, e);
					PutOnTop (g);
					return;
				}
			}
			_hoverWidget = null;
        }
        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
			if (_activeWidget == null)
				return;

			_activeWidget.onMouseButtonUp (this, e);
			_activeWidget = null;
        }
        void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
			if (_hoverWidget == null)
				return;

			GraphicObject g = _hoverWidget;
			while (!g.Focusable) {
				g = g.Parent as GraphicObject;
				if (g == null)
					return;
			}

			_activeWidget = g;
			_activeWidget.onMouseButtonDown (this, e);
        }
        void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
        {
			if (_hoverWidget == null)
				return;
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

		Rectangle ILayoutable.ClientRectangle {
			get { return new Size(this.ClientRectangle.Size.Width,this.ClientRectangle.Size.Height); }
		}

		public Rectangle getSlot ()
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
		public Rectangle rectInScreenCoord (Rectangle r)
		{
			throw new NotImplementedException ();
		}

		public Rectangle renderBoundsInContextCoordonate {
			get { return ClientRectangle; }
		}

		public Rectangle ClientBoundsInContextCoordonate {
			get {
				throw new NotImplementedException ();
			}
		}

		public Rectangle renderBoundsInBackendSurfaceCoordonate {
			get { return ClientRectangle; }
		}

		public Rectangle ClientBoundsInBackendSurfaceCoordonate {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public Rectangle ScreenCoordBounds {
			get { return ClientRectangle; }
		}

		public Rectangle ScreenCoordClientBounds {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		#endregion
    }
}