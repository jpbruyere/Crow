#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using Crow;
using System.Threading;
using System.Collections.Generic;


namespace test
{
	class GOLIBTests : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));			
		}
		#endregion

		public GOLIBTests ()
			: base(800, 600,"test: press spacebar to toogle test files")
		{
			VSync = VSyncMode.Off;
		}

		int frameCpt = 0;
		int idx = 0;
		string[] testFiles = {
			"testBorder.goml",
			"testGroupBox.goml",
			"test2WayBinding.crow",
			"testButton.crow",
			"0.crow",
			"testSpinner.goml",
			"fps.goml",
			"testTextBox.crow",
			"testImage.crow",
			"test4.goml",
			"testScrollbar.goml",
			"2.crow",
			"test1.goml",
			"testWindow2.goml",
//			"testCombobox.goml",
			"testWindow3.goml",
			"testWindow.goml",
			"testExpandable.goml",
			"testCheckbox.goml",
			"testPopper.goml",
			"testLabel.goml",
			"testAll.goml",
//			"testSpinner.goml",
			"testRadioButton2.goml",
			"testContainer.goml",
			"testRadioButton.goml",
			"testMsgBox.goml",
			"testGrid.goml",
			"testMeter.goml",
			"test_Listbox.goml",
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
				ValueChanged.Raise (this, new ValueChangeEventArgs ("drawing",
					this.drawingTime.ElapsedMilliseconds.ToString () + " ms"));
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
		#endregion

		public int intValue = 25;

		public int IntValue {
			get {
				return intValue;
			}
			set {
				intValue = value;
				NotifyValueChanged ("IntValue", intValue);
			}
		}
		void onSpinnerValueChange(object sender, ValueChangeEventArgs e){
			if (e.MemberName != "Value")
				return;
			intValue = Convert.ToInt32(e.NewValue);
		}
		public List<String> TestList = new List<string>( new string[] 
			{
				"string 1",
				"string 2",
				"string 3"
			});	

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			//this.AddWidget(new test4());

			GraphicObject obj = LoadInterface("Interfaces/" + testFiles[idx]);
			obj.DataSource = this;
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			//if (frameCpt % 8 == 0)
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
			if (FocusedWidget is TextBox) {
				base.OnKeyDown (e);
				return;
			}
			if (e.Key == Key.Escape) {
				this.Quit ();
				return;
			} else if (e.Key == Key.L) {
				TestList.Add ("new string");
				NotifyValueChanged ("TestList", TestList);
				return;
			} else if (e.Key == Key.W) {
				GraphicObject w = LoadInterface("Interfaces/testWindow.goml");
				w.DataSource = this;
				return;
			}
			ClearInterface ();
			idx++;
			if (idx == testFiles.Length)
				idx = 0;
			this.Title = testFiles [idx];
			GraphicObject obj = LoadInterface("Interfaces/" + testFiles[idx]);
			obj.DataSource = this;

		}
		void onButClick(object send, MouseButtonEventArgs e)
		{
			Console.WriteLine ("button clicked:" + send.ToString());
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