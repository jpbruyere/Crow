// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
		public Widget Host;
		public CrowThread (Widget host, ThreadStart start){
			thread = new Thread (start);
			thread.IsBackground = true;
			Host = host;
			lock (Host.IFace.CrowThreads)
				Host.IFace.CrowThreads.Add (this);
		}
		public void CheckState(){
			if (thread.ThreadState != ThreadState.Stopped)
				return;
			Finished.Raise (Host, null);
			lock (Host.IFace.CrowThreads)
				Host.IFace.CrowThreads.Remove (this);
		}
		public void Start() { thread.Start();}
		public void Cancel(){
			if (thread.IsAlive & !cancelRequested){
				cancelRequested = true;
				while (thread.IsAlive)
					Thread.Sleep (1);
				thread.Join ();
			}
			lock (Host.IFace.CrowThreads)
				Host.IFace.CrowThreads.Remove (this);
		}
	}
}

