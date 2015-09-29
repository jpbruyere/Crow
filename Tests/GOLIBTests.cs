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
using System.Collections.Generic;


namespace test
{
	class GOLIBTests : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation

		public event EventHandler<ValueChangeEventArgs> ValueChanged;

		#endregion

		public GOLIBTests ()
			: base(600, 400,"test: press spacebar to toogle test files")
		{
			VSync = VSyncMode.Off;
		}

		int frameCpt = 0;
		int idx = 0;
		string[] testFiles = {
			"test4.goml",
			"testContainer.goml",
			"testBorder.goml",
			"testLabel.goml",
			"testCheckbox.goml",
			"fps.goml",
			"testRadioButton.goml",
			"testSpinner.goml",
			"testPopper.goml",
			"testExpandable.goml",
			"testGroupBox.goml",
			"testCombobox.goml",
			"testWindow.goml",
			"testMsgBox.goml",
			"testGrid.goml",

			"testMeter.goml",
		};

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

				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms"));
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
		#endregion

		public List<String> TestList = new List<string>( new string[] 
			{
				"string 1",
				"string 2",
				"string 3"
			});	

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			this.AddWidget(new test4());
			//LoadInterface("Interfaces/" + testFiles[idx]);
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			if (frameCpt % 8 == 0)
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
			if (e.Key == Key.Escape) {
				this.Quit ();
			}
			ClearInterface ();
			idx++;
			if (idx == testFiles.Length)
				idx = 0;
			this.Title = testFiles [idx];
			LoadInterface("Interfaces/" + testFiles[idx]);

		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTests win = new GOLIBTests( )) {
				win.Run (30.0);
			}
		}
	}
}