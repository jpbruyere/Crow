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
	class GOLIBTest_Window : OpenTKGameWindow , IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		#endregion

		#region FPS
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

				if (ValueChanged != null)
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
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

		public GOLIBTest_Window ()
			: base(800, 600,"test")
		{}


		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/testWindow.goml");
			LoadInterface("Interfaces/testWindow.goml");
//			LoadInterface("Interfaces/testWindow.goml", out g);
//			LoadInterface("Interfaces/testWindow.goml", out g);
//			LoadInterface("Interfaces/testWindow.goml", out g);
//			LoadInterface("Interfaces/testWindow.goml", out g);
//			LoadInterface("Interfaces/testWindow.goml", out g);
			CursorVisible = true;
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

		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			base.OnKeyDown (e);
			this.Quit ();
		}

		void butQuitPress (object sender, MouseButtonEventArgs e)
		{
			DeleteWidget (sender as GraphicObject);
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_Window win = new GOLIBTest_Window( )) {
				win.Run (30.0);
			}
		}
	}
}