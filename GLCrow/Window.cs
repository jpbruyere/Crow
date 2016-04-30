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
using System.Runtime.InteropServices;
using System.Threading;
using Crow;
using Pencil.Gaming;
using Pencil.Gaming.Graphics;
using System.Diagnostics;
      
namespace GLC
{
	public class Window: IValueChange, IDisposable
    {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)				
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		GlfwWindowPtr winPtr;

		public Interface CrowInterface;
		public Rectangle ClientRectangle;

		public string Title {
			set {
				Glfw.SetWindowTitle(winPtr, value);
			}
		}

		#region FPS & GPU info
		int frameCpt = 0;
		float elapsed = 0f, frameElapsed = 0f;

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
				} 
				if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}

				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				#if MEASURE_TIME
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.CrowInterface.clippingTime.ElapsedTicks.ToString () + " ticks"));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("layouting",
					this.CrowInterface.layoutTime.ElapsedTicks.ToString () + " ticks"));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("drawing",
					this.CrowInterface.drawingTime.ElapsedTicks.ToString () + " ticks"));
				#endif
			}
		}

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;
		float _frameTime;
		float frameUpdateTime;

		public float frameTime 
		{
			get { return _frameTime; }
			set {				
				if (_frameTime == value)
					return;

				_frameTime = value;

				if (_frameTime > frameMax) {
					frameMax = _frameTime;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("frameMax", frameMax));
				} else if (_frameTime < frameMin) {
					frameMin = _frameTime;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("frameMin", frameMin));
				}

				ValueChanged.Raise(this, new ValueChangeEventArgs ("frameTime", frameTime));			
			}
		}
			
		public float frameMax = 0f, frameMin = float.MaxValue;

		void resetFpsAndFrameTime ()
		{
			frameMax = 0f;
			frameMin = float.MaxValue;
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		public string update = "";
		public string drawing = "";
		public string layouting = "";
		public int gpuFreeMem, gpuTotalMem, gpuDedicatedMem;

		public void onUpdateGPUMemInfo (object sender, MouseButtonEventArgs e)
		{
			
			//GL.GetInteger ((GetPName NvxGpuMemoryInfo.GpuMemoryInfoCurrentAvailableVidmemNvx, out gpuFreeMem);
			//GL.GetInteger ((GetPName)NvxGpuMemoryInfo.GpuMemoryInfoTotalAvailableMemoryNvx, out gpuTotalMem);
			//GL.GetInteger ((GetPName)NvxGpuMemoryInfo.GpuMemoryInfoDedicatedVidmemNvx, out gpuDedicatedMem);
			//NotifyValueChanged ("gpuFreeMem", gpuFreeMem / 1024);
			//NotifyValueChanged ("gpuTotalMem", gpuTotalMem / 1024);
			//NotifyValueChanged ("gpuDedicatedMem", gpuDedicatedMem / 1024);
		}
		public void onResetTimes (object sender, MouseButtonEventArgs e)
		{
			resetFpsAndFrameTime ();
		}
		#endregion

		#region ctor
		public Window(int _width, int _height, int colors, int depth, int stencil, int samples, string _title="Crow")			
		{
			ClientRectangle.Width = _width;
			ClientRectangle.Height = _height;


			CrowInterface = new Interface ();
			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();

			initGlfw ();

			Title = _title;

			OnResize (winPtr, _width, _height);

			initCrow ();

			OnLoad ();
		}

		void onGlfwError (GlfwError code, string desc)
		{
			Debug.WriteLine (desc);
		}

		#endregion

		void initGlfw(){
			if (!Glfw.Init ()) {
				Console.Error.WriteLine ("ERROR: Could not initialize GLFW, shutting down.");
				Environment.Exit (1);
			}
			Debug.WriteLine("GLFW: " + Glfw.GetVersionString ());
			// Create GLFW window.

			Glfw.WindowHint(WindowHint.Samples, 1);
			Glfw.WindowHint(WindowHint.ContextVersionMajor, 3 );
			Glfw.WindowHint(WindowHint.ContextVersionMinor, 3);
			Glfw.WindowHint (WindowHint.OpenGLProfile, (int)OpenGLProfile.Core);

			Glfw.SetErrorCallback(onGlfwError);

			winPtr = Glfw.CreateWindow (ClientRectangle.Width, ClientRectangle.Height,
				"", GlfwMonitorPtr.Null, GlfwWindowPtr.Null);

			Glfw.SetKeyCallback (winPtr, OnKeyEvent);
			Glfw.SetCursorPosCallback (winPtr, OnMouseMove);
			Glfw.SetMouseButtonCallback (winPtr, OnMouseButton);
			Glfw.SetFramebufferSizeCallback (winPtr, OnResize);
			Glfw.SetScrollCallback (winPtr, OnScroll);

			// Enable the OpenGL context for the current window
			Glfw.MakeContextCurrent (winPtr);

			Glfw.SwapInterval (0);

			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
		}


		void updateFrameTimes(){
			frameTime = (float)Glfw.GetTime ();

			Glfw.SetTime (0.0);

			elapsed += _frameTime;
			frameElapsed += _frameTime;

			if (frameElapsed >= frameUpdateTime) {				
				frameElapsed -= frameUpdateTime;
				OnUpdateFrame ();
			}
			frameCpt++;
		}

		#region Crow
		#region graphic context
		protected int texID;
		Tetra.Shader shader;
		public static GGL.vaoMesh quad;

		int[] pbos = new int[2];
		bool evenCycle = false;
		Rectangle dirtyR;

		void createContext()
		{
			if (GL.IsTexture(texID))
				GL.DeleteTexture (texID);
			GL.GenTextures(1, out texID);
			GL.BindTexture(TextureTarget.Texture2D, texID);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				ClientRectangle.Width, ClientRectangle.Height, 0,
				PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.bmp);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			initPBOs ();
		}
		void initPBOs(){
			if (GL.IsBuffer (pbos [0]))
				GL.DeleteBuffers (2, pbos);
			GL.GenBuffers (2, pbos);
			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, pbos [0]);
			GL.BufferData (BufferTarget.PixelUnpackBuffer, (IntPtr)(ClientRectangle.Width * ClientRectangle.Height*4),
				IntPtr.Zero, BufferUsageHint.StreamDraw);
			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, pbos [1]);
			GL.BufferData (BufferTarget.PixelUnpackBuffer, (IntPtr)(ClientRectangle.Width * ClientRectangle.Height*4),
				IntPtr.Zero, BufferUsageHint.StreamDraw);
			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, 0);
		}

		void updatePBOs()
		{
			int pboMapped, pboDraw;
			if (evenCycle) {
				pboMapped = pbos [0];
				pboDraw = pbos [1];
			} else {
				pboMapped = pbos [1];
				pboDraw = pbos [0];
			}

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, pboDraw);

			GL.TexSubImage2D (TextureTarget.Texture2D, 0,
				dirtyR.Left, dirtyR.Top,
				dirtyR.Width, dirtyR.Height,
				PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, pboMapped);
			IntPtr ptrTexData = GL.MapBuffer (BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
			if (ptrTexData != IntPtr.Zero) {
				dirtyR = CrowInterface.DirtyRect;
				Marshal.Copy (CrowInterface.dirtyBmp, 0, ptrTexData, CrowInterface.dirtyBmp.Length);
				GL.UnmapBuffer (BufferTarget.PixelUnpackBuffer);
			}

			GL.BindBuffer (BufferTarget.PixelUnpackBuffer, 0);
			evenCycle = !evenCycle;
		}
		#endregion
		void interfaceThread()
		{
			CrowInterface.Quit += Quit;
			CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			while (true) {
				CrowInterface.Update ();
				Thread.Sleep (1);
			}
		}
		void initCrow(){
			shader = new Tetra.Shader ();
			shader.Enable();
			shader.SetMVP(Pencil.Gaming.MathUtils.Matrix.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1));
			GL.UseProgram(0);
			quad = new GGL.vaoMesh (0, 0, 0, 1, 1, 1, -1);
		}
		void updateCrow(){
			if (!Monitor.TryEnter (CrowInterface.RenderMutex))
				return;			
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			if (CrowInterface.IsDirty) {
				GL.TexSubImage2D (TextureTarget.Texture2D, 0,
					CrowInterface.DirtyRect.Left, CrowInterface.DirtyRect.Top,
					CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height,
					PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.dirtyBmp);
				//TODO:use pbo
				//updatePBOs ();		
				CrowInterface.IsDirty = false;
			}
			Monitor.Exit (CrowInterface.RenderMutex);
		}
		void drawCrow(){
			//bool blend = GL.GetBoolean (GetPName.Blend);
			//			bool depthTest = GL.GetBoolean (GetPName.DepthTest);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			GL.Enable (EnableCap.Blend);
			GL.Disable (EnableCap.DepthTest);
			shader.Enable ();
			quad.Render (BeginMode.TriangleStrip);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			shader.Disable ();

			//			if (!blend)
			//				GL.Disable (EnableCap.Blend);
			//			if (depthTest)
			//				GL.Enable (EnableCap.DepthTest);

		}
		#endregion

		public void Quit (object sender, EventArgs e)
		{
			//this.Exit ();
		}
		void CrowInterface_MouseCursorChanged (object sender, MouseCursorChangedEventArgs e)
		{
			
			//this.Cursor = new MouseCursor(
			//	(int)e.NewCursor.Xhot,
			//	(int)e.NewCursor.Yhot,
			//	(int)e.NewCursor.Width,
			//	(int)e.NewCursor.Height,
			//	e.NewCursor.data);
		}



		public virtual void Run (int targetFrameRate) {
			frameUpdateTime = 1f / (float)targetFrameRate;

			Glfw.SetTime (0.0);
			while (!Glfw.WindowShouldClose (winPtr)) {				
				Glfw.PollEvents ();

				updateFrameTimes ();

				if (Glfw.GetKey (winPtr, Pencil.Gaming.Key.Escape)) {
					Glfw.SetWindowShouldClose (winPtr, true);
				}					

				GLClear ();

				OnRender ();

				drawCrow ();

				// Swap the front and back buffer, displaying the scene
				Glfw.SwapBuffers (winPtr);
			}
		}

		public virtual void GLClear()
		{
			GL.ClearColor (0.1f,0.1f,0.1f,0.1f);
			GL.Clear (ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
		}
		public virtual void OnLoad()
		{			
		}
		public virtual void OnRender ()
		{
		}


		protected virtual void OnUpdateFrame()
		{
			if (elapsed >= 1f) {
				fps = frameCpt;
				elapsed -= 1f;
				frameCpt = 0;
			}
			//resetFpsAndFrameTime ();

			updateCrow ();
		}
		protected virtual void OnResize (GlfwWindowPtr wnd, int width, int height)
		{
			ClientRectangle.Width = width;
			ClientRectangle.Height = height;

			CrowInterface.ProcessResize (
				new Rectangle (
				this.ClientRectangle.X,
				this.ClientRectangle.Y,
				this.ClientRectangle.Width,
				this.ClientRectangle.Height));
			createContext ();
			GL.Viewport (0, 0, ClientRectangle.Width, ClientRectangle.Height);
		}

		public virtual void Dispose ()
		{
			if (GL.IsTexture (texID))
				GL.DeleteTexture (texID);
			if (GL.IsBuffer (pbos [0]))
				GL.DeleteBuffers (2, pbos);
			
			// Finally, clean up Glfw, and close the window
			Glfw.Terminate ();
		}

		#region Mouse Handling
		protected virtual void OnMouseMove (GlfwWindowPtr wnd, double x, double y)
		{
			CrowInterface.ProcessMouseMove ((int)x, (int)y);
		}

		protected virtual void OnMouseButton (GlfwWindowPtr wnd, Pencil.Gaming.MouseButton btn, KeyAction action)
		{
			switch (action) {
			case KeyAction.Release:
				CrowInterface.ProcessMouseButtonUp ((int)btn);
				break;
			case KeyAction.Press:
				CrowInterface.ProcessMouseButtonDown ((int)btn);
				break;
			case KeyAction.Repeat:
				break;
			}
		}
		protected virtual void OnScroll (GlfwWindowPtr wnd, double xoffset, double yoffset)
		{
			CrowInterface.ProcessMouseWheelChanged ((float)yoffset);
		}

		//void update_mouseButtonStates(ref MouseState e, OpenTK.Input.MouseState otk_e){
		//	for (int i = 0; i < MouseState.MaxButtons; i++) {
		//		if (otk_e.IsButtonDown ((OpenTK.Input.MouseButton)i))
		//			e.EnableBit (i);
		//	}
		//}
		//void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
		//      {
		//	if (!CrowInterface.ProcessMouseMove (otk_e.X, otk_e.Y))
		//		MouseMove.Raise (sender, otk_e);
		//      }
		//void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		//      {
		//	if (!CrowInterface.ProcessMouseButtonUp ((int)otk_e.Button))
		//		MouseButtonUp.Raise (sender, otk_e);
		//      }
		//void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		//{
		//	if (!CrowInterface.ProcessMouseButtonDown ((int)otk_e.Button))
		//		MouseButtonDown.Raise (sender, otk_e);
		//      }
		//void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
		//      {
		//	if (!CrowInterface.ProcessMouseWheelChanged (otk_e.DeltaPrecise))
		//		MouseWheelChanged.Raise (sender, otk_e);
		//      }
		#endregion

		#region keyboard Handling

		protected virtual void OnKeyEvent (GlfwWindowPtr wnd,Pencil.Gaming.Key key,int scanCode,KeyAction action,Pencil.Gaming.KeyModifiers mods)
		{
			switch (action) {
			case KeyAction.Release:
				CrowInterface.ProcessKeyUp ((int)key);
				break;
			case KeyAction.Press:
				CrowInterface.ProcessKeyDown ((int)key);
				break;
			}
		}
		//void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		//{
		//	if (!CrowInterface.ProcessKeyDown((int)otk_e.Key))
		//		KeyboardKeyDown.Raise (this, otk_e);
		//      }
		//void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		//{
		//	if (!CrowInterface.ProcessKeyUp((int)otk_e.Key))
		//		KeyboardKeyUp.Raise (this, otk_e);
		//}
		//void OpenTKGameWindow_KeyPress (object sender, OpenTK.KeyPressEventArgs e)
		//{
		//	CrowInterface.ProcessKeyPress (e.KeyChar);
		//}
		#endregion
	}
}
