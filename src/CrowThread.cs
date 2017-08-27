//
// CrowThread.cs
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

namespace Crow
{
	/// <summary>
	/// Thread monitored by current interface with Finished event when state==Stopped
	/// </summary>
	public class CrowThread {
		public bool cancelRequested = false;
		Thread thread;
		public event EventHandler Finished;
		public GraphicObject Host;
		public CrowThread (GraphicObject host, ThreadStart start){
			thread = new Thread (start);
			thread.IsBackground = true;
			Host = host;
			lock (Host.currentInterface.CrowThreads)
				Host.currentInterface.CrowThreads.Add (this);
		}
		public void CheckState(){
			if (thread.ThreadState != ThreadState.Stopped)
				return;
			Finished.Raise (Host, null);
			lock (Host.currentInterface.CrowThreads)
				Host.currentInterface.CrowThreads.Remove (this);
		}
		public void Start() { thread.Start();}
		public void Cancel(){
			if (thread.IsAlive){
				cancelRequested = true;
				//cancelLoading = true;
				thread.Join ();
				//cancelLoading = false;
			}
			lock (Host.currentInterface.CrowThreads)
				Host.currentInterface.CrowThreads.Remove (this);
		}
	}
}

