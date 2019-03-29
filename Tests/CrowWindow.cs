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
				if (frameCpt % 3 == 0) {
					ValueChanged.Raise (this, new ValueChangeEventArgs ("fps", _fps));
					#if MEASURE_TIME
					foreach (PerformanceMeasure m in ifaceControl[0].PerfMeasures)
						m.NotifyChanges ();
					#endif
				}
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

		// TODO:We should be able to set the current interface programmaticaly
		/// <summary>
		/// Gets the currently focused interface, focus could have been given by creation of new iface controler and
		/// not only by the mouse
		/// </summary>
		public Interface CurrentInterface {
			get {
				checkDefaultIFace ();
				return ifaceControl [focusedIdx].CrowInterface;
			}
		}
			
		void addInterfaceControler(InterfaceControler ifaceControler)
		{
			ifaceControler.CrowInterface.Quit += Quit;
			ifaceControler.CrowInterface.MouseCursorChanged += CrowInterface_MouseCursorChanged;

			ifaceControl.Add (ifaceControler);
			focusedIdx = ifaceControl.Count - 1;
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
		public event EventHandler<OpenTK.Input.MouseWheelEventArgs> CrowMouseWheel;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> CrowMouseUp;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> CrowMouseDown;
		public event EventHandler<OpenTK.Input.MouseButtonEventArgs> CrowMouseClick;
		public event EventHandler<OpenTK.Input.MouseMoveEventArgs> CrowMouseMove;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> CrowKeyDown;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> CrowKeyUp;

		#endregion

		public ProjectiveIFaceControler Add3DInterface(int width, int height, Matrix4 ifaceModelMat){
			ProjectiveIFaceControler tmp = new ProjectiveIFaceControler (new Rectangle (0, 0, width, height), ifaceModelMat);
			addInterfaceControler (tmp);
			return tmp;
		}
		public GraphicObject AddWidget (string path)
		{			
			GraphicObject tmp = Load (path);
			return tmp;
		}
		public GraphicObject AddWidget (GraphicObject g, int interfaceIdx = 0){
			checkDefaultIFace ();
			ifaceControl [interfaceIdx].CrowInterface.AddWidget (g);
			return g;
		}

		public void DeleteWidget (GraphicObject g, int interfaceIdx = 0){
			ifaceControl [interfaceIdx].CrowInterface.DeleteWidget (g);
		}
		/// <summary>
		/// check if a default interface exists, create one if not
		/// </summary>
		void checkDefaultIFace (){
			if (ifaceControl.Count == 0) {//create default orthogonal interface
				addInterfaceControler (new InterfaceControler (
					new Rectangle (0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height)));
				focusedIdx = 0;
			}
		}
		/// <summary>
		/// Load the content of the IML file pointed by path and add it to the current interface
		/// graphic tree.
		/// </summary>
		/// <param name="path">the path of the IML file to load</param>
		/// <param name="interfaceIdx">interface index to bind to, a default one is created if none exists</param>
		public GraphicObject Load (string path, int interfaceIdx = 0){
			checkDefaultIFace();
			return ifaceControl [interfaceIdx].CrowInterface.AddWidget (path);
		}
		/// <summary>
		/// Load the content of the IML string passed as first argument and add it to the current interface
		/// graphic tree.
		/// </summary>
		/// <param name="path">a valid IML string</param>
		/// <param name="interfaceIdx">interface index to bind to, a default one is created if none exists</param>
		public void LoadIMLFragment (string imlFragment, int interfaceIdx = 0){
			checkDefaultIFace();
			ifaceControl [interfaceIdx].CrowInterface.LoadIMLFragment (imlFragment);
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
			KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
			KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyUp);

			MouseWheel += new EventHandler<OpenTK.Input.MouseWheelEventArgs>(GL_Mouse_WheelChanged);
			MouseDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(GL_Mouse_ButtonDown);
			MouseUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(GL_Mouse_ButtonUp);
			MouseMove += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(GL_Mouse_Move);

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
		protected virtual void GL_Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs otk_e)
        {
			if (activeIdx == -2) {				
				for (int i = 0; i < ifaceControl.Count; i++) {
					if (ifaceControl [i].ProcessMouseMove (otk_e.X, otk_e.Y)) {
						focusedIdx = i;
						return;
					}
				}
			}
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseMove (otk_e.X, otk_e.Y))
					return;
			}
			CrowMouseMove.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
        {
			activeIdx = -2;
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseButtonUp ((int)otk_e.Button))
					return;
			}
			CrowMouseUp.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs otk_e)
		{
			activeIdx = focusedIdx;
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseButtonDown ((int)otk_e.Button))
					return;
			}
			CrowMouseDown.Raise (sender, otk_e);
        }
		protected virtual void GL_Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs otk_e)
        {
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessMouseWheelChanged (otk_e.DeltaPrecise))
					return;
			}
			CrowMouseWheel.Raise (sender, otk_e);
        }

		protected virtual void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessKeyDown((int)otk_e.Key))
					return;
			}
			CrowKeyDown.Raise (this, otk_e);
        }
		protected virtual void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs otk_e)
		{
			if (focusedIdx >= 0) {
				if (ifaceControl [focusedIdx].ProcessKeyUp((int)otk_e.Key))
					return;
			}
			CrowKeyUp.Raise (this, otk_e);
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
