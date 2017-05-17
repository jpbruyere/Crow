//
// tests.cs
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
using Crow.Linux;
using System.Threading;
using Linux;
using System.Runtime.InteropServices;
using System.Text;

//using static Crow.Linux.VT.VTControler;
using VT = Linux.VT;
using DRI = Linux.DRI;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Crow;



namespace testDrm
{
	public class TestApp : Application {

		public bool Running = true;

		public TestApp () : base () {
			
		}
		public override void Run ()
		{
			Load ("#testDrm.ui.menu.crow").DataSource = this;
			Load (@"/mnt/data2/devel/crow/Tests/Interfaces/Divers/0.crow");

			while(Running){
				try {
					base.Run ();
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString());
				}
			}
		}
		void onQuitClick(object send, Crow.MouseButtonEventArgs e)
		{
			Running = false;
		}
	}
	static class Tests
	{
		static void signal_handler (Signal s){
			Console.WriteLine ("signal catched: " + s.ToString());
		}
		static void genEglConstCase (){
			Dictionary<int,string> dic = new Dictionary<int, string> ();

			//parseEglConsts ("/home/jp/tmp/EGL/eglplatform.h", ref dic);
			parseEglConsts ("/home/jp/tmp/EGL/egl.h", ref dic);

			IOrderedEnumerable<KeyValuePair<int,string>> result = dic.OrderBy (p => p.Key);

			foreach (KeyValuePair<int,string> kv in result) {
				Console.WriteLine ("case {0}:\n\treturn \"{1}\";", kv.Key, kv.Value);
			}
		}
		static void parseEglConsts (string path, ref Dictionary<int,string> dic){
			using (Stream s = new FileStream (path, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s)) {
					while (!sr.EndOfStream) {
						string l = sr.ReadLine ();
						if (!l.StartsWith ("#define"))
							continue;
						l = l.Substring (8);
						string[] ll = l.Split (new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						string[] cn = ll [0].Split ('_');
						try {
							string constName = "";

							for (int i = 1; i < cn.Length; i++) {
								cn [i] = cn [i].ToLowerInvariant ();
								constName += char.ToUpperInvariant (cn [i] [0]) + cn [i].Substring (1);							 
							}

							int value = 0;
							if (ll [1].StartsWith ("0x")) {
								if (int.TryParse (ll [1].Substring (2), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.CurrentCulture, out value)) {
									if (dic.ContainsKey (value))
										dic [value] += "|" + ll [0].Substring (4);
									else
										dic [value] = ll [0].Substring (4);
									continue;
								} else
									Console.WriteLine ("parsing error: " + l);
							} else
								Console.WriteLine ("not hex value: " + l);
							//Console.WriteLine ("{0}\t= {1},", constName, ll [1]);
						} catch (Exception ex) {
							Console.WriteLine ("failed: " + l);
						}
					}
				}
			}
		}
		static void Main ()
		{
			try {
				using (TestApp crowApp = new TestApp ()) {
					crowApp.Run ();
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
		}
		static void testApp () {
			int previousVT = -1, appVT = -1;

			if (Kernel.signal (Signal.SIGUSR1, switch_request_handle) < 0)
				throw new Exception ("signal handler registation failed");			
			if (Kernel.signal (Signal.SIGINT, sigint_handler) < 0)
				throw new Exception ("SIGINT handler registation failed");

			using (VT.VTControler master = new VT.VTControler ()) {
				previousVT = master.CurrentVT;
				appVT = master.FirstAvailableVT;
				master.SwitchTo (appVT);
				try {
					master.KDMode = VT.KDMode.GRAPHICS;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}
			}
			try {
				using (TestApp crowApp = new TestApp ()) {
					crowApp.Run ();
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}

			using (VT.VTControler master = new VT.VTControler ()) {
				//				try {
				//					master.KDMode = VT.KDMode.TEXT;
				//				} catch (Exception ex) {
				//					Console.WriteLine (ex.ToString ());	
				//				}
				master.SwitchTo (previousVT);
			}
		}

		static void testKMS(){
			int previousVT = -1, appVT = -1;

			if (Kernel.signal (Signal.SIGUSR1, switch_request_handle) < 0)
				throw new Exception ("signal handler registation failed");			
			if (Kernel.signal (Signal.SIGINT, sigint_handler) < 0)
				throw new Exception ("SIGINT handler registation failed");

			using (VT.VTControler master = new VT.VTControler ()) {
				previousVT = master.CurrentVT;
				appVT = master.FirstAvailableVT;
				master.SwitchTo (appVT);
				try {
					master.KDMode = VT.KDMode.GRAPHICS;
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}
			}
			try {
				using (DRI.GPUControler gpu = new DRI.GPUControler ()) {
					
//					int i = 0;
//					while(running && i < 1000){
//						try {
//							gpu.Update ();
//							i++;
//						} catch (Exception ex) {
//							Console.WriteLine (ex.ToString());
//						}
//
//					}
						

				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}

			using (VT.VTControler master = new VT.VTControler ()) {
				//				try {
				//					master.KDMode = VT.KDMode.TEXT;
				//				} catch (Exception ex) {
				//					Console.WriteLine (ex.ToString ());	
				//				}
				master.SwitchTo (previousVT);
			}
		}

		static void signalTest (){
			if (Kernel.signal (Signal.SIGINT, signal_handler) < 0)
				throw new Exception ("signal handler registation failed");
			Console.WriteLine ("Handler registered for {0}", Signal.SIGINT);
			while (true)
				Thread.Sleep (1);			
		}
//		static void pty_tests(){
//			int ret = 0;
//			int fd = -1;
//			fd = Libc.posix_openpt (OpenFlags.ReadWrite);
//			if (fd < 0)
//				return;
//			string newPts = Crow.Linux.VT.TTY.GetFreePtsPath (fd);
//			Console.WriteLine (newPts);
//
//			Crow.Linux.VT.TTY.unlockpt (fd);
//			int fdPts = -1;
//			fdPts = Libc.open(newPts, OpenFlags.ReadWrite);
//			if (fdPts < 0)
//				return;
//			
//			Libc.close (fdPts);
//			Libc.close (fd);
//
//			Console.WriteLine ("terminated succeffully");
//		}

		static void tty_switch2(){
			int previousVT = -1, appVT = -1;
			using(VT.VTControler master = new VT.VTControler()){
				VT.vt_mode m = master.VTMode;

				Console.WriteLine ("Startup:");
				Console.WriteLine ("\tVT{0}\t- KDMode: {1}", master.CurrentVT, master.KDMode);
				Console.WriteLine ("\t\t- VTMode= {0}", m.mode);
				Console.WriteLine ("\t\t- RELSIG= {0}", ((Signal)m.relsig).ToString());

				previousVT = master.CurrentVT;
				appVT = master.FirstAvailableVT;


				master.SwitchTo (appVT);

				m = master.VTMode;

				try {
					master.KDMode = VT.KDMode.GRAPHICS;
					//m.mode = VT.SwitchMode.AUTO;
					//master.VTMode = m;

				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());	
				}

				Console.WriteLine ("Switch:");
				Console.WriteLine ("\tVT{0}\t- KDMode: {1}", master.CurrentVT, master.KDMode);
				Console.WriteLine ("\t\t- VTMode= {0}", m.mode);
				Console.WriteLine ("\t\t- RELSIG= {0}", m.relsig.ToString());

				if (Kernel.signal (Signal.SIGUSR1, switch_request_handle) < 0)
					throw new Exception ("signal handler registation failed");
				Console.WriteLine ("Handler registered for switching tty");
				if (Kernel.signal (Signal.SIGINT, sigint_handler) < 0)
					throw new Exception ("SIGINT handler registation failed");
				Console.WriteLine ("SIGINT Handler registered");

				while (running) {					
					Thread.Sleep (500);
					Console.Write (".");
				}


				master.SwitchTo (previousVT);

				Console.WriteLine ("Back to master:");
				Console.WriteLine ("\tVT{0}\t- KDMode: {1}", master.CurrentVT, master.KDMode);
				Console.WriteLine ("\t\t- VTMode= {0}", master.VTMode.mode);

			}

//			using (VTControler vt = new VTControler ("/dev/tty" + appVT)) {
//				vt.CurrentMode = VT.Mode.GRAPHICS;
//			}


			Console.WriteLine ("terminated succeffully");
			//vtc = new VTControler ("/dev/tty" + appVT);
			//vtc.CurrentMode = VT.Mode.GRAPHICS;
		}
		static bool running = true;
		static void switch_request_handle (Signal s){
			Console.WriteLine ("switch request catched: " + s.ToString());
			using (VT.VTControler master = new VT.VTControler ()) {
				Libc.write (master.fd, Encoding.ASCII.GetBytes ("this is a test string"));
				master.AcknoledgeSwitchRequest ();
			}			
		}
		static void sigint_handler (Signal s){
			Console.WriteLine ("SIGINT catched");
			running = false;
		}
		static void dumpDrmResources(){
			string gpu_path = "/dev/dri/card0";
			int fd_gpu = Libc.open(gpu_path, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd_gpu < 0)
				throw new NotSupportedException("[DRI] Failed to open gpu");

			using (DRI.Resources resources = new DRI.Resources (fd_gpu)) {
				foreach (DRI.Connector e in resources.Connectors) {					
					Console.WriteLine (e.ToString ());
				}
				foreach (DRI.Encoder e in resources.Encoders)
					Console.WriteLine (e.ToString ());
				foreach (DRI.Crtc e in resources.Crtcs)
					Console.WriteLine (e.ToString ());
			}
			
			Libc.close (fd_gpu);	
		}
//		static void dumpDrm(){
//			using (DRI.GPUControler gpu = new DRI.GPUControler ()) {
//
//
//				if (gpu.PlanesIds.Length > 0){
//					DRI.drmPlane plane = gpu.GetPlane (gpu.PlanesIds [0]);
//				}
//
//				Console.WriteLine ("DRI Resources:");
//				Console.WriteLine ("\t- Connectors\t({0})", gpu.ConnectorIds.Length);
//				for (int i = 0; i < gpu.ConnectorIds.Length; i++) {
//					DRI.Connector mc = gpu.GetConnector (gpu.ConnectorIds [i]);
//					Console.WriteLine ("\t\t{0}. {1,-11} ({2})",i, mc.Type, mc.State);
//					//DRI.Encoder e = mc.CurrentEncoder;
//					//DRI.Crtc c = gpu.GetCrtc (e.crtc_id);
//					//DRI.FrameBuffer fb = gpu.GetFB (c.buffer_id);
//
//				}
//				Console.WriteLine ("\t- Encoderds\t({0})", gpu.EncoderIds.Length);
//				Console.WriteLine ("\t- Crtcs\t\t({0})", gpu.CrtcIds.Length);
//				Console.WriteLine ("\t- FrameBuffers\t({0})", gpu.FbIds.Length);
//				Console.WriteLine ("\t- Planes\t({0})", gpu.PlanesIds.Length);
//				//				foreach (Crow.Linux.DRI.ModeConnector c in gpu.Connectors) {
//				//					Console.WriteLine ("connector id: {0}\tType: {1}", c.connector_id, c.connector_type);
//				//				}
//			}			
//		}
	}
}

