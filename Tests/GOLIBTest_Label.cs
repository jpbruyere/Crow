#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;


namespace test
{
	class GOLIBTest_Label : OpenTKGameWindow , IValueChange
	{
		#region FPS
		int _fps = 0;

		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;
				
				int oldVal = _fps;
				_fps = value;

				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax, _fps));
				} else if (_fps < fpsMin) {
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin, _fps));
					fpsMin = _fps;
				}

				if (ValueChanged != null)
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", oldVal, _fps));

				//ValueChanged.Raise (this, new ValueChangeEventArgs ("fps", oldVal, _fps));
			}
		}
		string name = "testName";

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
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
		#endregion

		public GOLIBTest_Label ()
			: base(600, 400,"test")
		{}

		Label g;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/testLabel.goml", out g);

		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}

		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			if (frameCpt > 200) {
				resetFps ();
				frameCpt = 0;

			}
			frameCpt++;
		}

		#region IValueChange implementation

		public event EventHandler<ValueChangeEventArgs> ValueChanged;

		#endregion

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_Label win = new GOLIBTest_Label( )) {
				win.Run (30.0);
			}
		}
	}
}