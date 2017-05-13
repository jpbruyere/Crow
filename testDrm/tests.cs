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
using VT = Crow.Linux.VT;
using Crow.Linux.DRI;

namespace testDrm
{
	static class Tests
	{
		static void signal_handler (Signal s){
			Console.WriteLine ("signal catched: " + s.ToString());
		}
		static void Main ()
		{
			using (GPUControler gpu = new GPUControler ()) {
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

	}
}

