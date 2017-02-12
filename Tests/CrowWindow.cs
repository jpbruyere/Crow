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
	public class CrowWindow : GameWindow, IValueChange
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
				if (frameCpt % 3 == 0)
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				#if MEASURE_TIME
//				foreach (PerformanceMeasure m in PerfMeasures)
//					m.NotifyChanges();
				#endif
			}
		}

		#if MEASURE_TIME
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
		public CrowWindow(int _width = 800, int _height = 600, string _title="Crow",
			int colors = 32, int depth = 24, int stencil = 0, int samples = 1,
			int major=3, int minor=3)
			: this(_width, _height, new OpenTK.Graphics.GraphicsMode(colors, depth, stencil, samples),
				_title,GameWindowFlags.Default,DisplayDevice.Default,
				major,minor,OpenTK.Graphics.GraphicsContextFlags.Default)
		{
		}
		public CrowWindow (int width, int height, OpenTK.Graphics.GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags)
			: base(width,height,mode,title,options,device,major,minor,flags)
		{
		}

		#endregion

		protected Shader shader;
		public List<InterfaceControler> ifaceControl = new List<InterfaceControler>();
		int focusedIdx = -1, activeIdx = -2;

		void addInterfaceControler(InterfaceControler ifaceControler)
		{
			ifaceControler.CrowInterface.Quit += Quit;
			ifaceControler.CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			ifaceControl.Add (ifaceControler);
		}
		void openGLDraw(){
			//save GL states
			bool blend, depthTest, cullFace;
			GL.GetBoolean (GetPName.Blend, out blend);
			GL.GetBoolean (GetPName.DepthTest, out depthTest);
			GL.GetBoolean (GetPName.CullFace, out cullFace);
			GL.Enable (EnableCap.Blend);
			GL.Disable (EnableCap.DepthTest);
			GL.Disable (EnableCap.CullFace);

			#if MEASURE_TIME
			glDrawMeasure.StartCycle();
			#endif

			shader.Enable ();
			for (int i = 0; i < ifaceControl.Count; i++) {
				shader.SetMVP (ifaceControl [i].InterfaceMVP);
				ifaceControl [i].OpenGLDraw ();
			}

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

		public void Quit (object sender, EventArgs e)
		{
			foreach (InterfaceControler ic in ifaceControl) {
				ic.Dispose ();
			}
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

		public ProjectiveIFaceControler Add3DInterface(int width, int height, Matrix4 ifaceModelMat){
			ProjectiveIFaceControler tmp = new ProjectiveIFaceControler (new Rectangle (0, 0, width, height), ifaceModelMat);
			addInterfaceControler (tmp);
			return tmp;
		}
		public GraphicObject AddWidget (GraphicObject g, int interfaceIdx = 0){
			if (ifaceControl.Count == 0)//create default orthogonal interface
				addInterfaceControler (new InterfaceControler (
					new Rectangle (0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height)));
			ifaceControl [interfaceIdx].CrowInterface.AddWidget (g);
			return g;
		}
		public void DeleteWidget (GraphicObject g, int interfaceIdx = 0){
			ifaceControl [interfaceIdx].CrowInterface.DeleteWidget (g);
		}
		public GraphicObject Load (string path, int interfaceIdx = 0){
			if (ifaceControl.Count == 0)//create default orthogonal interface
				addInterfaceControler (new InterfaceControler (
							new Rectangle (0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height)));
			return ifaceControl [interfaceIdx].CrowInterface.LoadInterface (path);
		}
		public GraphicObject FindByName (string nameToFind){
			for (int i = 0; i < ifaceControl.Count; i++) {
				GraphicObject tmp = ifaceControl [i].CrowInterface.FindByName (nameToFind);
				if (tmp != null)
					return tmp;
			}
			return null;
		}
		public void ClearInterface (int interfaceIdx = 0){
			ifaceControl [interfaceIdx].CrowInterface.ClearInterface ();
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

			shader = new Shader ();
			shader.Enable ();

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
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
			for (int i = 0; i < ifaceControl.Count; i++) {
				ifaceControl[i].ProcessResize(
					new Rectangle(
						0,
						0,
						this.ClientRectangle.Width,
						this.ClientRectangle.Height));
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
		protected virtual void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
        {
			if (activeIdx == -2) {
				focusedIdx = -1;
				for (int i = 0; i < ifaceControl.Count; i++) {
					if (ifaceControl [i].ProcessMouseMove (otk_e.X, otk_e.Y)) {
						focusedIdx = i;
						return;
					}
				}
			} else if (focusedIdx >= 0) {
				ifaceControl [focusedIdx].ProcessMouseMove (otk_e.X, otk_e.Y);
				return;
			}
			if (focusedIdx < 0)
				MouseMove.Raise (sender, otk_e);
        }
		protected virtual void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			activeIdx = -2;
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseButtonUp ((int)otk_e.Button))
					return;
			}
			MouseButtonUp.Raise (sender, otk_e);
        }
		protected virtual void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			activeIdx = focusedIdx;
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseButtonDown ((int)otk_e.Button))
					return;
			}
			MouseButtonDown.Raise (sender, otk_e);
        }
		protected virtual void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseWheelChanged (otk_e.DeltaPrecise))
					return;
			}
			MouseWheelChanged.Raise (sender, otk_e);
        }

		protected virtual void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessKeyDown((int)otk_e.Key))
					return;
			}
			KeyboardKeyDown.Raise (this, otk_e);
        }
		protected virtual void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessKeyUp((int)otk_e.Key))
					return;
			}
			KeyboardKeyUp.Raise (this, otk_e);
		}
		protected virtual void OpenTKGameWindow_KeyPress (object sender, OpenTK.KeyPressEventArgs e)
		{
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessKeyPress (e.KeyChar))
					return;
			}
			//TODO:create keyboardkeypress evt
		}
        #endregion
    }
}
