//
// Main.cs
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
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Cairo;
using Crow.Linux;
using Crow;
using System.Reflection;
using System.Linq;
using System.Threading;

namespace testDrm
{
	
	public class TestApp : Application, IValueChange 
	{
		[STAThread]
		static void Main ()
		{
//			int pid = Mono.Unix.Native.Syscall.getpid ();
//			uint gid = Mono.Unix.Native.Syscall.getgid ();
//			int sid =  Mono.Unix.Native.Syscall.getsid (0);
//			int newsid = Mono.Unix.Native.Syscall.setsid ();
//
//			Console.WriteLine ("pid:{0} gid:{1} sid:{2}", pid, gid, sid);
//			Console.WriteLine ("pid:{0} gid:{1} sid:{2}",
//				Mono.Unix.Native.Syscall.getpid (),
//				Mono.Unix.Native.Syscall.getgid (),
//				Mono.Unix.Native.Syscall.getsid (0));			

			System.Threading.Thread.CurrentThread.Name = "Main";

			try {
				using (TestApp crowApp = new TestApp ())					
					crowApp.Run ();
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
			Console.WriteLine ("terminating");
		}

		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion


		public Command CMDViewPerf, CMDViewCfg, CMDViewTest0, CMDOpen;

		public TestApp () : base () {
			CrowInterface.KeyboardKeyDown += CrowInterface_KeyboardKeyDown;

			CMDViewPerf = new Command(new Action(() => Load ("#testDrm.ui.perfMeasures.crow").DataSource = this)) { Caption = "Performances"};
			CMDViewCfg = new Command(new Action(() => Load ("#testDrm.ui.2.crow").DataSource = this)) { Caption = "Configuration"};
			CMDViewTest0 = new Command(new Action(() => Load ("#testDrm.ui.0.crow").DataSource = this)) { Caption = "Test view 0"};
			CMDOpen = new Command(new Action(() => { 
				lock (CrowInterface.UpdateMutex) CrowInterface.AddWidget(new FileDialog());})) { Caption = "Open"};

			Load ("#testDrm.ui.menu.crow").DataSource = this;

			initTests ();
		}

		void CrowInterface_KeyboardKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			if (e.Alt){
				if (e.Key >= Key.F1 && e.Key <= Key.F8){
					int ttyNum = 0;

					if (int.TryParse (e.Key.ToString ().Substring (1), out ttyNum))
						Console.WriteLine ("tty switch {0}", ttyNum);
					this.CurrentState = RunState.DesactivateRequest;
				}
			}
		}

		public int frameTime = 0;
		public int frameMin = int.MaxValue;
		public int frameMax = 0;
		#region Test values for Binding
//		public int intValue = 500;
//		DirectoryInfo curDir = new DirectoryInfo (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
//		public FileSystemInfo[] CurDirectory {
//			get { return curDir.GetFileSystemInfos (); }
//		}
//		public int IntValue {
//			get {
//				return intValue;
//			}
//			set {
//				intValue = value;
//				NotifyValueChanged ("IntValue", intValue);
//			}
//		}
//		void onSpinnerValueChange(object sender, ValueChangeEventArgs e){
//			if (e.MemberName != "Value")
//				return;
//			intValue = Convert.ToInt32(e.NewValue);
//		}
//		void change_alignment(object sender, EventArgs e){
//			RadioButton rb = sender as RadioButton;
//			if (rb == null)
//				return;
//			NotifyValueChanged ("alignment", Enum.Parse(typeof(Alignment), rb.Caption));
//		}
//		public IList<String> List2 = new List<string>(new string[]
//			{
//				"string1",
//				"string2",
//				"string3",
//			}
//		);
//		public IList<String> TestList2 {
//			set{
//				List2 = value;
//				NotifyValueChanged ("TestList2", testList);
//			}
//			get { return List2; }
//		}
		IList<Crow.Color> testList = Crow.Color.ColorDic;
		public IList<Crow.Color> TestList {
			set{
				testList = value;
				NotifyValueChanged ("TestList", testList);
			}
			get { return testList; }
		}
//		string curSources = "";
//		public string CurSources {
//			get { return curSources; }
//			set {
//				if (value == curSources)
//					return;
//				curSources = value;
//				NotifyValueChanged ("CurSources", curSources);
//			}
//		}
//		bool boolVal = true;
//		public bool BoolVal {
//			get { return boolVal; }
//			set {
//				if (boolVal == value)
//					return;
//				boolVal = value;
//				NotifyValueChanged ("BoolVal", boolVal);
//			}
//		}

		#endregion

		public int InterfaceSleep {
			get { return ifaceSleep; }
			set {
				if (ifaceSleep == value)
					return;
				ifaceSleep = value;
				NotifyValueChanged ("InterfaceSleep", ifaceSleep);
			}
		}
		public int FlipPollSleep {
			get { return gpu.FlipPollingSleep; }
			set {
				if (gpu.FlipPollingSleep == value)
					return;
				gpu.FlipPollingSleep = value;
				NotifyValueChanged ("FlipPollSleep", gpu.FlipPollingSleep);
			}
		}
		public int UpdateSleep {
			get { return updateSleep; }
			set {
				if (updateSleep == value)
					return;
				updateSleep = value;
				NotifyValueChanged ("UpdateSleep", updateSleep);
			}
		}

		EGL.Surface pbuff;
		void initTests(){



//			pbuff = EGL.Surface.CreatePBuffer(gpu.eglctx,800,600);
//			Console.WriteLine ("pbuff created successfully");
//			Thread.Sleep (100);
//			pbuff.Dispose ();
		}
		void testegldraw()
		{
			int width = 19;
			int height = 19;
			using (Cairo.ImageSurface img = new Cairo.ImageSurface (Cairo.Format.Argb32, gpu.Width, gpu.Height)) {
				glDrawMeasure.StartCycle ();
				using (Cairo.Context ctx = new Cairo.Context (img)) {
					for (int x = 100; x < 800; x += 20) {
						for (int y = 100; y < 800; y += 20) {
							ctx.Rectangle (x, y, width, height);
							ctx.SetSourceRGBA (x / 800.0, y / 800.0, 0.5, 0.5);
							ctx.Fill ();
						}
					}
				}
				glDrawMeasure.StopCycle ();
				using (Cairo.Context ctx = new Cairo.Context (cairoSurf)) {
					ctx.SetSourceSurface (img, 0, 0);
					ctx.Paint ();
				}

			}

		}

		protected override void uiDraw ()
		{			
			Stopwatch frame = Stopwatch.StartNew ();

			testegldraw ();

			base.uiDraw ();
					
			frame.Stop();
			frameTime = (int)frame.ElapsedTicks;
			NotifyValueChanged("frameTime", frameTime);
			if (frameTime > frameMax){
				frameMax = frameTime;
				NotifyValueChanged("frameMax", frameMax);	
			}
			if (frameTime < frameMin){
				frameMin = frameTime;
				NotifyValueChanged("frameMin", frameMin);	
			}
			#if MEASURE_TIME
			foreach (PerformanceMeasure m in PerfMeasures)
				m.NotifyChanges();
			#endif
		}

		void onQuitClick(object send, Crow.MouseButtonEventArgs e)
		{
			Running = false;
		}
	}
}

