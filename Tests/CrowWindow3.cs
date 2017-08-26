//
// CrowWindow.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using OpenTK.Platform;
using Crow.SDL2;

namespace Crow
{
	public class CrowWindow3 : GameWindow, IValueChange
    {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		#region FPS
		int frameCpt = 0;
		int _fps = 0;
		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;

				_fps = value;
				#if MEASURE_TIME
				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax));
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}
				#endif
				if (frameCpt % 3 == 0) {
					ValueChanged.Raise (this, new ValueChangeEventArgs ("fps", _fps));
					#if MEASURE_TIME
					foreach (PerformanceMeasure m in PerfMeasures)
						m.NotifyChanges ();
					#endif
				}
			}
		}

		#if MEASURE_TIME
		public List<PerformanceMeasure> PerfMeasures;
		public PerformanceMeasure glDrawMeasure = new PerformanceMeasure("OpenGL Draw", 10);

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endif
		#endregion

		#region ctor
		public CrowWindow3(int _width = 800, int _height = 600, string _title="Crow",
			int colors = 32, int depth = 24, int stencil = 0, int samples = 1,
			int major=3, int minor=0)
			: this(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.Default, major, minor,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
		}
		public CrowWindow3 (int width, int height, OpenTK.Graphics.GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags)
			: base(width,height,mode,title,options,device,major,minor,flags)
		{
		}
		#endregion

		IntPtr hCairoGLCtx;
		Cairo.GLXDevice dev;
		Cairo.GLSurface surf;

		public Interface CrowInterface;

		protected Matrix4 projection;
		public int texID;
		public bool mouseIsInInterface = false;

		void initCrow() {			
			CrowInterface = new Interface ();
			CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			#if MEASURE_TIME
			PerfMeasures = new List<PerformanceMeasure> (
				new PerformanceMeasure[] {
					CrowInterface.updateMeasure,
					CrowInterface.layoutingMeasure,
					CrowInterface.clippingMeasure,
					CrowInterface.drawingMeasure,
					glDrawMeasure
				}
			);
			#endif

//			Thread t = new Thread (interfaceThread);
//			t.IsBackground = true;
//			t.Start ();
		}

		void initCairo(){
			SysWMInfo sdlInfo;
			IntPtr hWnd = this.WindowInfo.Handle; 
			SDL.GL.SetAttribute (ContextAttribute.SHARE_WITH_CURRENT_CONTEXT, 1);
			int test;
			SDL.GL.GetAttribute (ContextAttribute.SHARE_WITH_CURRENT_CONTEXT, out test);
			SDL.GetWindowWMInfo (hWnd, out sdlInfo);

			hCairoGLCtx = SDL.GL.CreateContext (hWnd);


			dev = new Cairo.GLXDevice (sdlInfo.Info.X11.Display, hCairoGLCtx);
		}

//		void interfaceThread()
//		{
//			while (CrowInterface.ClientRectangle.Size.Width == 0)
//				Thread.Sleep (5);
//
//			while (true) {
//				CrowInterface.Update ();
//				Thread.Sleep (1);
//			}
//		}

		#region graphic context
		public virtual void initGL(){
			#if DEBUG
			Console.WriteLine("\n\n*************************************");
			Console.WriteLine("GL version: " + GL.GetString (StringName.Version));
			Console.WriteLine("GL vendor: " + GL.GetString (StringName.Vendor));
			Console.WriteLine("GLSL version: " + GL.GetString (StringName.ShadingLanguageVersion));
			Console.WriteLine("*************************************\n");
			#endif

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			projection = OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);

			createContext ();
		}
		/// <summary>Create the texture for the interface redering</summary>
		public virtual void createContext()
		{
			if (GL.IsTexture (texID))
				GL.DeleteTexture (texID);
			GL.GenTextures (1, out texID);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				CrowInterface.ClientRectangle.Width, CrowInterface.ClientRectangle.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			surf = new Cairo.GLSurface (dev, Cairo.Content.ColorAlpha, (uint)texID,
				CrowInterface.ClientRectangle.Width, CrowInterface.ClientRectangle.Height);

			CrowInterface.bmp = surf;
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}

		void openGLDraw(){
			//save GL states
			bool blend, depthTest, cullFace;
			GL.GetBoolean (GetPName.Blend, out blend);
			GL.GetBoolean (GetPName.DepthTest, out depthTest);
			GL.GetBoolean (GetPName.CullFace, out cullFace);
			GL.Enable (EnableCap.Texture2D);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Disable (EnableCap.DepthTest);
			GL.Disable (EnableCap.CullFace);
			GL.Viewport (0, 0, CrowInterface.ClientRectangle.Width, CrowInterface.ClientRectangle.Height);

			#if MEASURE_TIME
			glDrawMeasure.StartCycle();
			#endif


			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
//			if (Monitor.TryEnter(CrowInterface.RenderMutex)) {
//				if (CrowInterface.IsDirty) {
//					byte[] data = null;
//					if (CrowInterface.dirtyBmp != null)
//						data = (CrowInterface.dirtyBmp as Cairo.ImageSurface).Data;
//					GL.TexSubImage2D (TextureTarget.Texture2D, 0,
//						CrowInterface.DirtyRect.Left, CrowInterface.DirtyRect.Top,
//						CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height,
//						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);
//					CrowInterface.IsDirty = false;
//				}
//				Monitor.Exit (CrowInterface.RenderMutex);
//			}
//

			Matrix4 proj = Matrix4.CreateOrthographicOffCenter (-1f, 1f, -1f, 1f, 1.0f, -1.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix (ref proj);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			GL.BindTexture (TextureTarget.Texture2D, texID);

			GL.Begin(PrimitiveType.Quads);
			GL.TexCoord2(0, 1);	GL.Vertex2(-1, -1);
			GL.TexCoord2(0, 0);	GL.Vertex2(-1, 1);
			GL.TexCoord2(1, 0);	GL.Vertex2(1, 1);
			GL.TexCoord2(1, 1);	GL.Vertex2(1, -1);
			GL.End();

			GL.BindTexture(TextureTarget.Texture2D, 0);

			#if MEASURE_TIME
			glDrawMeasure.StopCycle();
			#endif
			//restore GL states
			if (!blend)
				GL.Disable (EnableCap.Blend);
			if (depthTest)
				GL.Enable (EnableCap.DepthTest);
			if (cullFace)
				GL.Enable (EnableCap.CullFace);
		}
		#endregion

		public void Quit (object sender, EventArgs e)
		{
			dev.Dispose ();
			SDL.GL.MakeCurrent (this.WindowInfo.Handle, IntPtr.Zero);
			SDL.GL.DeleteContext (hCairoGLCtx);
			this.MakeCurrent ();

			this.Exit ();
		}

		void CrowInterface_MouseCursorChanged (object sender, MouseCursorChangedEventArgs e)
		{
			this.Cursor = new MouseCursor(
				(int)e.NewCursor.Xhot,
				(int)e.NewCursor.Yhot,
				(int)e.NewCursor.Width,
				(int)e.NewCursor.Height,
				e.NewCursor.data);
		}

		#region Events are raised only if mouse isn't in a graphic object
		public event EventHandler<OpenTK.Input.MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseClick;
		public event EventHandler<OpenTK.Input.MouseMoveEventArgs> MouseMove;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> KeyboardKeyDown;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> KeyboardKeyUp;
		#endregion


		public GraphicObject AddWidget (GraphicObject g){
			CrowInterface.AddWidget (g);
			return g;
		}
		public void DeleteWidget (GraphicObject g){
			CrowInterface.DeleteWidget (g);
		}
		public GraphicObject Load (string path){			
			return CrowInterface.LoadInterface (path);
		}
		public GraphicObject FindByName (string nameToFind){
			return CrowInterface.FindByName (nameToFind);
		}

		/// <summary>Override this method for your OpenGL rendering calls</summary>
		public virtual void OnRender(FrameEventArgs e)
		{
		}
		/// <summary>Override this method to customize clear method between frames</summary>
		public virtual void GLClear()
		{
			GL.Clear (ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
		}

		#region Game win overrides
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			this.KeyPress += new EventHandler<OpenTK.KeyPressEventArgs>(OpenTKGameWindow_KeyPress);
			Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Keyboard.KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyUp);
			Mouse.WheelChanged += new EventHandler<OpenTK.Input.MouseWheelEventArgs>(GL_Mouse_WheelChanged);
			Mouse.ButtonDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(GL_Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(GL_Mouse_ButtonUp);
			Mouse.Move += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(GL_Mouse_Move);

			initCairo ();
			initCrow ();
			initGL ();
		}
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			fps = (int)RenderFrequency;

			#if MEASURE_TIME
			if (frameCpt > 500) {
				resetFps ();
				frameCpt = 0;
//				#if DEBUG
//				GC.Collect();
//				GC.WaitForPendingFinalizers();
//				NotifyValueChanged("memory", GC.GetTotalMemory (false).ToString());
//				#endif
			}
			#endif

			frameCpt++;

			SDL.GL.MakeCurrent (this.WindowInfo.Handle, hCairoGLCtx);
			CrowInterface.Update ();
			surf.Flush ();
			surf.SwapBuffers ();
			this.MakeCurrent ();
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GLClear ();

			base.OnRenderFrame(e);

			OnRender (e);
			openGLDraw ();

			SwapBuffers ();
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize (e);

			lock (CrowInterface.UpdateMutex) {
				CrowInterface.ClientRectangle = this.ClientRectangle;
				createContext ();
				CrowInterface.ProcessResize ();
			}
		}
		#endregion

		#region Mouse and Keyboard Handling
		void update_mouseButtonStates(ref MouseState e, OpenTK.Input.MouseState otk_e){
			for (int i = 0; i < MouseState.MaxButtons; i++) {
				if (otk_e.IsButtonDown ((OpenTK.Input.MouseButton)i))
					e.EnableBit (i);
			}
		}
		protected virtual void GL_Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
        {
			if (!CrowInterface.ProcessMouseMove (otk_e.X, otk_e.Y))
				MouseMove.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			if (!CrowInterface.ProcessMouseButtonUp ((int)otk_e.Button))
				MouseButtonUp.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			if (!CrowInterface.ProcessMouseButtonDown ((int)otk_e.Button))
				MouseButtonDown.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (!CrowInterface.ProcessMouseWheelChanged (otk_e.DeltaPrecise))
				MouseWheelChanged.Raise (sender, otk_e);
        }
		protected virtual void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{			
			if (!CrowInterface.ProcessKeyDown((int)otk_e.Key))
				KeyboardKeyDown.Raise (this, otk_e);
        }
		protected virtual void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (!CrowInterface.ProcessKeyUp((int)otk_e.Key))
				KeyboardKeyUp.Raise (this, otk_e);
		}
		protected virtual void OpenTKGameWindow_KeyPress (object sender, OpenTK.KeyPressEventArgs e)
		{
			CrowInterface.ProcessKeyPress (e.KeyChar);				
			//TODO:create keyboardkeypress evt
		}
        #endregion
    }
}
