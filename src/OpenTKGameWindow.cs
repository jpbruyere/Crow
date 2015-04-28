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
using System.Xml;
using System.IO;



namespace go
{
	public class OpenTKGameWindow : GameWindow, ILayoutable, IGOLibHost
    {
		#region ctor
//		public OpenTKGameWindow(int _width, int _height, string _title="golib")
//			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 1), _title,
//				GameWindowFlags.Fullscreen,
//				DisplayDevice.Default,
//				3,0,OpenTK.Graphics.GraphicsContextFlags.Default)
		public OpenTKGameWindow(int _width, int _height, string _title="golib")
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8), 
				_title,GameWindowFlags.Default,DisplayDevice.Default,
				3,2,OpenTK.Graphics.GraphicsContextFlags.Debug|OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
//		public OpenTKGameWindow(int _width, int _height, string _title="golib")
//			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8), _title)
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

		internal static OpenTKGameWindow currentWindow;

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
		public void AddWidget(GraphicObject g)
		{
			g.Parent = this;
			GraphicObjects.Add (g);

			g.RegisterForLayouting ((int)LayoutingType.Sizing);
		}
		public void DeleteWidget(GraphicObject g)
		{
			g.Visible = false;//trick to ensure clip is added to refresh zone
			GraphicObjects.Remove (g);
		}

		#region Events
		//those events are raised only if mouse isn't in a graphic object
		public event EventHandler<MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<MouseButtonEventArgs> MouseClick;
		public event EventHandler<MouseMoveEventArgs> MouseMove;
		#endregion

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
		bool recreateContext = true;

		Context ctx;
		Surface surf;
		byte[] bmp;
		int texID;

		QuadVAO uiQuad;
		go.GLBackend.Shader shader;
		Matrix4 projectionMatrix, 
				modelviewMatrix;

		Rectangle dirtyZone = Rectangle.Empty;

		void createContext()
		{
			createOpenGLSurface ();
			if (uiQuad != null)
				uiQuad.Dispose ();
			uiQuad = new QuadVAO (0, 0, ClientRectangle.Width, ClientRectangle.Height,0,1,1,-1);
			projectionMatrix = Matrix4.CreateOrthographicOffCenter 
				(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
			modelviewMatrix = Matrix4.Identity;
			redrawClip.AddRectangle (ClientRectangle);
			recreateContext = false;
		}
		void createOpenGLSurface()
		{
			currentWindow = this;

			int stride = 4 * ClientRectangle.Width;
			int bmpSize = Math.Abs (stride) * ClientRectangle.Height;
			bmp = new byte[bmpSize];

			//create texture
			if (texID > 0)
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
		}
		void OpenGLDraw()
		{
			shader.Enable ();
			shader.ProjectionMatrix = projectionMatrix;
			shader.ModelViewMatrix = modelviewMatrix;
			shader.Color = new Vector4(1f,1f,1f,1f);
			//if (dirtyZone != Rectangle.Empty) {
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			GL.TexSubImage2D (TextureTarget.Texture2D, 0,
				0, 0, ClientRectangle.Width, ClientRectangle.Height,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp);

			uiQuad.Render (PrimitiveType.TriangleStrip);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			shader.Disable ();
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
//
//			foreach (GraphicObject p in invGOList) {
//				if (p.Visible) {
//					layoutTime.Start ();
//					while(!p.LayoutIsValid)
//						p.UpdateLayout ();
//					layoutTime.Stop ();
//				}
//			}
			while (Interface.LayoutingQueue.Count > 0) {
				LayoutingQueueItem lqi = Interface.LayoutingQueue.Dequeue ();
				lqi.ProcessLayouting ();
			}

			//Debug.WriteLine ("otd:" + gobjsToRedraw.Count.ToString () + "-");
			//redraw clip should be added when layout is complete among parents,
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
			
		public void LoadInterface<T>(string path, out T result)
		{
			Interface.Load<T> (path, out result, this);
			AddWidget (result as GraphicObject);
		}
		public T LoadInterface<T> (string Path)
		{
			T result;
			Interface.Load<T> (Path, out result, this);
			AddWidget (result as GraphicObject);
			return result;
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
			if (recreateContext)
				createContext ();
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

			int matl = GL.GetInteger (GetPName.MaxArrayTextureLayers);
			int mts = GL.GetInteger (GetPName.MaxTextureSize);
			shader = new go.GLBackend.TexturedShader ();
		}
		protected override void OnUnload(EventArgs e)
		{
			if (texID > 0)
				GL.DeleteTexture (texID);
			//ctx.Dispose ();
			//surf.Dispose ();
		}

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
			MouseMove.Raise (this, e);
        }
        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
			if (_activeWidget == null)
				return;

			_activeWidget.onMouseButtonUp (this, e);
			_activeWidget = null;
			MouseButtonUp.Raise (this, e);
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
			_activeWidget.onMouseButtonDown (this, e);
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

		public void RegisterForLayouting (int layoutType)
		{
			throw new NotImplementedException ();
		}

		public void UpdateLayout (LayoutingType layoutType)
		{
			throw new NotImplementedException ();
		}

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

		public IGOLibHost TopContainer {
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