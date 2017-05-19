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
using System.Threading;

namespace testMonitor
{
	
	public class testMonitor
	{
		static object _obj = new object();
		static object mutex1
		{
			get
			{
				System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1);
				System.Diagnostics.Debug.WriteLine(String.Format("Lock acquired by: {0} on thread {1}", frame.GetMethod().Name, System.Threading.Thread.CurrentThread.ManagedThreadId));
				return _obj;
			}
		}

		[STAThread]
		static void Main ()
		{
			
			Thread t1 = new Thread (thread1);
			t1.Start ();
			Thread t2 = new Thread (thread2);
			t2.Start ();

			while (true) {
				continue;
				Monitor.Enter (mutex1);
				//Console.WriteLine ("Main thread entered mutex1 lock");
				Thread.Sleep (1000);
//				Console.WriteLine ("Main Thread wait state mutex1 lock");
//				Monitor.Wait (Mutex1);
//				Console.WriteLine ("Main Thread wait finished mutex1 lock");
				Monitor.Exit (mutex1);
				Thread.Sleep (1);
				//Console.WriteLine ("Thread 1 state: {0}", t1.ThreadState.ToString ());
			}


		}

		static void thread1(){
			while (true) {
				Monitor.Enter (mutex1);
				Thread.Sleep (1000);
				Monitor.Exit (mutex1);
				Thread.Sleep (1);
			}
		}
		static void thread2(){
			while (true) {
				Monitor.Enter (mutex1);
				Thread.Sleep (1000);
				Monitor.Exit (mutex1);
				Thread.Sleep (1);
			}
		}
	}
}