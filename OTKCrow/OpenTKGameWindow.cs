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

		void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		public string update = "";
		public string drawing = "";
		public string layouting = "";
		#endregion

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
			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();
//			interfaceThread ();
		}
		public OpenTKGameWindow(int _width, int _height, int colors, int depth, int stencil, int samples, string _title="Crow")
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.GetDisplay(DisplayIndex.Second),
				3,3,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
			Thread t = new Thread (interfaceThread);
			t.IsBackground = true;
			t.Start ();
		}
		#endregion

		void interfaceThread()
		{
			CrowInterface = new Interface ();
			CrowInterface.Quit += Quit;
			CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			while (true) {
				CrowInterface.Update ();
				Thread.Sleep (5);
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
		int texID;
		QuadVAO uiQuad;
		Crow.Shader shader;
		int[] viewport = new int[4];

		void createContext()
		{
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

			shader.Texture = texID;
			#endregion

			#region Update ui quad
			if (uiQuad != null)
				uiQuad.Dispose ();
			uiQuad = new QuadVAO (0, 0, ClientRectangle.Width, ClientRectangle.Height, 0, 1, 1, -1);

			shader.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter
				(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
			#endregion

			//TODO:add maybe clientrectangle to clipping here
		}
		void OpenGLDraw()
		{
			GL.GetInteger (GetPName.Viewport, viewport);
			GL.Viewport (0, 0, ClientRectangle.Width, ClientRectangle.Height);

			shader.Enable ();
			lock (CrowInterface.RenderMutex) {
				if (CrowInterface.IsDirty) {
					GL.TexSubImage2D (TextureTarget.Texture2D, 0,
						CrowInterface.DirtyRect.Left, CrowInterface.DirtyRect.Top,
						CrowInterface.DirtyRect.Width, CrowInterface.DirtyRect.Height,
						OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, CrowInterface.dirtyBmp);
					CrowInterface.IsDirty = false;
				}
			}

			uiQuad.Render (PrimitiveType.TriangleStrip);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			shader.Disable ();
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);
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

			shader = new Crow.TexturedShader ();
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			fps = (int)RenderFrequency;


			if (frameCpt > 50) {
				resetFps ();
				frameCpt = 0;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				NotifyValueChanged("memory", GC.GetTotalMemory (false).ToString());
			}
			frameCpt++;
			//CrowInterface.Update ();
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
				this.ClientRectangle.X,
				this.ClientRectangle.Y,
				this.ClientRectangle.Width,
				this.ClientRectangle.Height));
			createContext ();
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
