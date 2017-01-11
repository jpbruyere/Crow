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
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Crow
{
	public class CrowWindow3D : GameWindow, IValueChange
    {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)				
				ValueChanged.Invoke(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public Interface CrowInterface;

		#region FPS
		int frameCpt = 0;
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
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}
				if (frameCpt % 3 == 0)
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				#if MEASURE_TIME
				foreach (PerformanceMeasure m in PerfMeasures)
					m.NotifyChanges();
				#endif
			}
		}

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		public string update = "";
		public string drawing = "";
		public string layouting = "";
		public string clipping = "";
		#if MEASURE_TIME
		public PerformanceMeasure glDrawMeasure = new PerformanceMeasure("OpenGL Draw", 10);
		#endif

		#endregion

		#region ctor
		public CrowWindow3D(int _width = 800, int _height = 600, string _title="Crow",
			int colors = 32, int depth = 24, int stencil = 0, int samples = 1,
			int major=3, int minor=3)
			: this(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.Default,
				major,minor,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
		}
		public CrowWindow3D (int width, int height, OpenTK.Graphics.GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags)
			: base(width,height,mode,title,options,device,major,minor,flags)
		{
			CrowInterface = new Interface ();

			#if MEASURE_TIME
			PerfMeasures = new List<PerformanceMeasure> (
				new PerformanceMeasure[] {
					this.CrowInterface.updateMeasure,
					this.CrowInterface.layoutingMeasure,
					this.CrowInterface.clippingMeasure,
					this.CrowInterface.drawingMeasure,
					this.glDrawMeasure
				}
			);
			#endif

			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();
		}

		#endregion

		#if MEASURE_TIME
		public List<PerformanceMeasure> PerfMeasures;
		#endif

		void interfaceThread()
		{
			CrowInterface.Quit += Quit;
			CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;
			while (CrowInterface.ClientRectangle.Size.Width == 0)
				Thread.Sleep (5);
			
			while (true) {
				CrowInterface.Update ();
				//Thread.Sleep (1);
			}
		}

		public void Quit (object sender, EventArgs e)
		{
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

		#region Events
		//those events are raised only if mouse isn't in a graphic object
		public event EventHandler<OpenTK.Input.MouseWheelEventArgs> MouseWheelChanged;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonUp;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseButtonDown;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> MouseClick;
		public event EventHandler<OpenTK.Input.MouseMoveEventArgs> MouseMove;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> KeyboardKeyDown;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> KeyboardKeyUp;

		#endregion

		#region graphic context
		public int texID;
		public Shader shader;
		public vaoMesh quad;
		public Matrix4 projection;

		void initGL(){
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			shader = new Shader ();
			quad = new Crow.vaoMesh (0, 0, 0, 1, 1, 1, -1);
		}
		/// <summary>Create the texture for the interface redering</summary>
		void createContext()
		{
			if (GL.IsTexture(texID))
				GL.DeleteTexture (texID);
			GL.GenTextures(1, out texID);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texID);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				ClientRectangle.Width, ClientRectangle.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.bmp);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		/// <summary>Rendering of the interface</summary>
		void OpenGLDraw()
		{
			#if MEASURE_TIME
			glDrawMeasure.StartCycle();
			#endif
			bool blend, depthTest;
			GL.GetBoolean (GetPName.Blend, out blend);
			GL.GetBoolean (GetPName.DepthTest, out depthTest);
			GL.Enable (EnableCap.Blend);
			GL.Disable (EnableCap.DepthTest);

			shader.Enable ();
			shader.SetMVP (modelview * projection);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			if (Monitor.TryEnter(CrowInterface.RenderMutex)) {
				if (CrowInterface.IsDirty) {
					GL.TexSubImage2D (TextureTarget.Texture2D, 0,
						CrowInterface.DirtyRect.Left, CrowInterface.DirtyRect.Top,
						CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height,
						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.dirtyBmp);
					CrowInterface.IsDirty = false;
				}
				Monitor.Exit (CrowInterface.RenderMutex);
			}
			quad.Render (BeginMode.TriangleStrip);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			if (!blend)
				GL.Disable (EnableCap.Blend);
			if (depthTest)
				GL.Enable (EnableCap.DepthTest);
			#if MEASURE_TIME
			glDrawMeasure.StopCycle();
			#endif
		}
		#endregion

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
			Mouse.WheelChanged += new EventHandler<OpenTK.Input.MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.ButtonDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(Mouse_Move);

			#if DEBUG
			Console.WriteLine("\n\n*************************************");
			Console.WriteLine("GL version: " + GL.GetString (StringName.Version));
			Console.WriteLine("GL vendor: " + GL.GetString (StringName.Vendor));
			Console.WriteLine("GLSL version: " + GL.GetString (StringName.ShadingLanguageVersion));
			Console.WriteLine("*************************************\n");
			#endif

			initGL ();
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			fps = (int)RenderFrequency;


			if (frameCpt > 500) {
				resetFps ();
				frameCpt = 0;
//				#if DEBUG
//				GC.Collect();
//				GC.WaitForPendingFinalizers();
//				NotifyValueChanged("memory", GC.GetTotalMemory (false).ToString());
//				#endif
			}
			frameCpt++;
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GLClear ();

			base.OnRenderFrame(e);

			OnRender (e);
			OpenGLDraw ();

			SwapBuffers ();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize (e);
			CrowInterface.ProcessResize(
				new Rectangle(
				0,
				0,
				this.ClientRectangle.Width,
				this.ClientRectangle.Height));
			createContext ();
			UpdateViewMatrix ();
		}
		#endregion
		int[] viewport = new int[4];
		#region Mouse Handling
		void update_mouseButtonStates(ref MouseState e, OpenTK.Input.MouseState otk_e){
			for (int i = 0; i < MouseState.MaxButtons; i++) {
				if (otk_e.IsButtonDown ((OpenTK.Input.MouseButton)i))
					e.EnableBit (i);
			}
		}
		public static Matrix4 modelview;
		public static Matrix4 orthoMat//full screen quad rendering
			= OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);
		Vector3 vEyeTarget = new Vector3(0f, 0f, 0f);
		Vector3 vEye;
		Vector3 vLookInit = Vector3.Normalize(new Vector3(0.0f, 0.0f, 1.0f));
		Vector3 vLook;  // Camera vLook Vector
		float zFar = 300.0f;
		float zNear = 0.001f;
		float fovY = (float)Math.PI / 4;
		float eyeDist = 1.2f;
		float MoveSpeed = 0.02f;
		float RotationSpeed = 0.005f;
		float ZoomSpeed = 0.22f;
		float viewZangle, viewXangle;

		public Vector4 vLight = new Vector4 (0.5f, 0.5f, -1f, 0f);

		Point mousePosition;

		void updateMousePosition(OpenTK.Input.MouseMoveEventArgs otk_e)
		{			
			Vector3 vMouse = UnProject(ref projection, ref modelview, viewport, new Vector2 (otk_e.X, otk_e.Y)).Xyz;
			Vector3 vMouseRay = Vector3.Normalize(vMouse - vEye);
			float a = vEye.Z / vMouseRay.Z;
			vMouse = vEye - vMouseRay * a;
			mousePosition = new Point ((int)Math.Truncate ((vMouse.X + 0.5f) * viewport [2]),
				viewport [3] - (int)Math.Truncate ((vMouse.Y + 0.5f) * viewport [3]));			
		}

		void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
        {
			updateMousePosition (otk_e);

			if (mousePosition.X.IsInBetween (0, ClientRectangle.Width) & mousePosition.Y.IsInBetween (0, ClientRectangle.Height)
				&!(Keyboard[OpenTK.Input.Key.ShiftLeft])) {
				if (CrowInterface.ProcessMouseMove (mousePosition.X, mousePosition.Y))
					return;
			}
			if (otk_e.XDelta != 0 || otk_e.YDelta != 0)
			{
				if (otk_e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					viewZangle -= (float)otk_e.XDelta * RotationSpeed;
					viewXangle -= (float)otk_e.YDelta * RotationSpeed;
//					if (viewXangle < - 0.75f)
//						viewXangle = -0.75f;
//					else if (viewXangle > MathHelper.PiOver4)
//						viewXangle = MathHelper.PiOver4;
					UpdateViewMatrix ();
				}else if (otk_e.Mouse.LeftButton == OpenTK.Input.ButtonState.Pressed) {
					return;
				}else if (otk_e.Mouse.RightButton == OpenTK.Input.ButtonState.Pressed) {
					Vector2 v2Look = vLook.Xz.Normalized ();
					Vector2 disp = v2Look.PerpendicularLeft * otk_e.XDelta * MoveSpeed +
						v2Look * otk_e.YDelta * MoveSpeed;
					vEyeTarget += new Vector3 (disp.X, disp.Y, 0);
					UpdateViewMatrix();
				}					
				//System.Diagnostics.Debug.WriteLine ("vMouse={0} newPos={1}", vMouse, newPos);
			}
			MouseMove.Raise (sender, otk_e);
        }

		void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			if (mousePosition.X.IsInBetween (0, ClientRectangle.Width) & mousePosition.Y.IsInBetween (0, ClientRectangle.Height)
				&!(Keyboard[OpenTK.Input.Key.ShiftLeft])) {
				if (CrowInterface.ProcessMouseButtonUp ((int)otk_e.Button))
					return;
			}
			MouseButtonUp.Raise (sender, otk_e);
        }
		void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			if (mousePosition.X.IsInBetween (0, ClientRectangle.Width) & mousePosition.Y.IsInBetween (0, ClientRectangle.Height)
				&!(Keyboard[OpenTK.Input.Key.ShiftLeft])) {
				if (CrowInterface.ProcessMouseButtonDown ((int)otk_e.Button))
					return;
			}
			MouseButtonDown.Raise (sender, otk_e);
        }
		void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (mousePosition.X.IsInBetween (0, ClientRectangle.Width) & mousePosition.Y.IsInBetween (0, ClientRectangle.Height)
				&!(Keyboard[OpenTK.Input.Key.ShiftLeft])) {
				if (CrowInterface.ProcessMouseWheelChanged (otk_e.DeltaPrecise))
					return;
			}
			float speed = ZoomSpeed;
			if (Keyboard[OpenTK.Input.Key.ControlLeft])
				speed *= 20.0f;

			eyeDist -= otk_e.Delta * speed;
			if (eyeDist < zNear)
				eyeDist = zNear;
			else if (eyeDist > zFar)
				eyeDist = zFar;
			UpdateViewMatrix ();
			
			MouseWheelChanged.Raise (sender, otk_e);
        }
		#endregion

		#region keyboard Handling
		void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (!CrowInterface.ProcessKeyDown((int)otk_e.Key))
				KeyboardKeyDown.Raise (this, otk_e);
        }
		void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (!CrowInterface.ProcessKeyUp((int)otk_e.Key))
				KeyboardKeyUp.Raise (this, otk_e);
		}
		void OpenTKGameWindow_KeyPress (object sender, OpenTK.KeyPressEventArgs e)
		{
			CrowInterface.ProcessKeyPress (e.KeyChar);
		}
        #endregion

		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			vLook = vLookInit.Transform(
				Matrix4.CreateRotationX (viewXangle)*
				Matrix4.CreateRotationY (viewZangle));
			vLook.Normalize();
			vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitY);
			GL.GetInteger(GetPName.Viewport, viewport);
		}
		static Vector4 UnProject(ref Matrix4 projection, ref Matrix4 view, int[] viewport, Vector2 mouse)
		{
			Vector4 vec;

			vec.X = 2.0f * mouse.X / (float)viewport[2] - 1;
			vec.Y = -(2.0f * mouse.Y / (float)viewport[3] - 1);
			vec.Z = 0f;
			vec.W = 1.0f;

			Matrix4 viewInv = Matrix4.Invert(view);
			Matrix4 projInv = Matrix4.Invert(projection);

			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);

			if (vec.W > float.Epsilon || vec.W < float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return vec;
		}
    }

	public static class Extensions {
		public static Vector4 ToVector4(this Color c){
			float[] f = c.floatArray;
			return new Vector4 (f [0], f [1], f [2], f [3]);
		}
		public static Vector3 Transform(this Vector3 v, Matrix4 m){
			return Vector4.Transform(new Vector4(v, 1), m).Xyz;			
		}
		public static bool IsInBetween(this int v, int min, int max){
			return v >= min & v <= max;
		}
	}
}
