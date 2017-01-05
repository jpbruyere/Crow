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
	public class OpenTKGameWindow : GameWindow, IValueChange
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
		#endregion

		#region ctor
		public OpenTKGameWindow(int _width = 800, int _height = 600, string _title="Crow",
			int colors = 32, int depth = 24, int stencil = 0, int samples = 1,
			int major=3, int minor=3)
			: this(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.Default,
				major,minor,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
		}
		public OpenTKGameWindow (int width, int height, OpenTK.Graphics.GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags)
			: base(width,height,mode,title,options,device,major,minor,flags)
		{
			CrowInterface = new Interface ();

			#if MEASURE_TIME
			PerfMeasures = new List<PerformanceMeasure> (
				new PerformanceMeasure[] {
					this.CrowInterface.updateMeasure,
					this.CrowInterface.layoutingMeasure,
					this.CrowInterface.clippingMeasure,
					this.CrowInterface.drawingMeasure
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
		public int texID, pboIdx, pboNextIdx;
		int[] pboHandles = new int[2];
		int pboSize;
		Rectangle pboRect;
		public Shader shader;
		public vaoMesh quad;
		public Matrix4 projection;

		void createContext()
		{
			if (GL.IsBuffer (pboHandles[0]))
				GL.DeleteBuffers (2, pboHandles);
			GL.GenBuffers (2, pboHandles);
			pboRect = ClientRectangle;
			pboSize= 4 * ClientRectangle.Width * ClientRectangle.Height;
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboHandles[0]);
			GL.BufferData(BufferTarget.PixelUnpackBuffer, pboSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboHandles[1]);
			GL.BufferData(BufferTarget.PixelUnpackBuffer, pboSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
			#region Create texture
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
			#endregion
		}
		void OpenGLDraw()
		{
			bool blend, depthTest;
			GL.GetBoolean (GetPName.Blend, out blend);
			GL.GetBoolean (GetPName.DepthTest, out depthTest);
			GL.Enable (EnableCap.Blend);
			GL.Disable (EnableCap.DepthTest);

			shader.Enable ();
			shader.SetMVP (projection);

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, texID);
			if (Monitor.TryEnter(CrowInterface.RenderMutex)) {				
				if (CrowInterface.IsDirty) {
					pboIdx = (pboIdx + 1) % 2;
					pboNextIdx = (pboIdx + 1) % 2;

					// bind the texture and PBO (texture is already binded
					GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboHandles[pboIdx]);

					// copy pixels from PBO to texture object
					// Use offset instead of pointer.
					GL.TexSubImage2D (TextureTarget.Texture2D, 0,
						pboRect.Left, pboRect.Top,
						pboRect.Width, pboRect.Height,
						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
					
					pboRect = CrowInterface.DirtyRect;
					pboSize = 4 * CrowInterface.DirtyRect.Width * CrowInterface.DirtyRect.Height;
					// bind PBO to update texture source
					GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboHandles[pboNextIdx]);

					// Note that glMapBufferARB() causes sync issue.
					// If GPU is working with this buffer, glMapBufferARB() will wait(stall)
					// until GPU to finish its job. To avoid waiting (idle), you can call
					// first glBufferDataARB() with NULL pointer before glMapBufferARB().
					// If you do that, the previous data in PBO will be discarded and
					// glMapBufferARB() returns a new allocated pointer immediately
					// even if GPU is still working with the previous data.
					GL.BufferData(BufferTarget.PixelUnpackBuffer, pboSize,IntPtr.Zero, BufferUsageHint.StreamDraw);

					// map the buffer object into client's memory
					IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer,BufferAccess.WriteOnly);
					if(ptr!=IntPtr.Zero)
					{
						// update data directly on the mapped buffer
						System.Runtime.InteropServices.Marshal.Copy(CrowInterface.dirtyBmp,0,ptr,pboSize);
						GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer); // release the mapped buffer
					}

					// it is good idea to release PBOs with ID 0 after use.
					// Once bound with 0, all pixel operations are back to normal ways.
					GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

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
		}
		#endregion

		/// <summary>
		/// Override this method for your OpenGL rendering calls
		/// </summary>
		public virtual void OnRender(FrameEventArgs e)
		{
		}
		/// <summary>
		/// Override this method to customize clear method between frames
		/// </summary>
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

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

			Console.WriteLine("\n\n*************************************");
			Console.WriteLine("GL version: " + GL.GetString (StringName.Version));
			Console.WriteLine("GL vendor: " + GL.GetString (StringName.Vendor));
			Console.WriteLine("GLSL version: " + GL.GetString (StringName.ShadingLanguageVersion));
			Console.WriteLine("*************************************\n");

			projection = OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);

			shader = new Shader ();
			quad = new Crow.vaoMesh (0, 0, 0, 1, 1, 1, -1);
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
			GL.Viewport (0, 0, ClientRectangle.Width, ClientRectangle.Height);
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
			if (!CrowInterface.ProcessMouseMove (otk_e.X, otk_e.Y))
				MouseMove.Raise (sender, otk_e);
        }
		void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			if (!CrowInterface.ProcessMouseButtonUp ((int)otk_e.Button))
				MouseButtonUp.Raise (sender, otk_e);
        }
		void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			if (!CrowInterface.ProcessMouseButtonDown ((int)otk_e.Button))
				MouseButtonDown.Raise (sender, otk_e);
        }
		void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (!CrowInterface.ProcessMouseWheelChanged (otk_e.DeltaPrecise))
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
    }
}
