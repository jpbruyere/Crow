//
//  CrowThread.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Threading;

namespace Crow
{
	/// <summary>
	/// Thread monitored by current interface with Finished event when state==Stopped
	/// </summary>
	public class CrowThread {
		Thread thread;
		public event EventHandler Finished;
		public GraphicObject Host;
		public CrowThread (GraphicObject host, ThreadStart start){
			thread = new Thread (start);
			thread.IsBackground = true;
			Host = host;
			lock (Host.CurrentInterface.CrowThreads)
				Host.CurrentInterface.CrowThreads.Add (this);
		}
		public void CheckState(){
			if (thread.ThreadState != ThreadState.Stopped)
				return;
			Finished.Raise (Host, null);
			lock (Host.CurrentInterface.CrowThreads)
				Host.CurrentInterface.CrowThreads.Remove (this);
		}
		public void Start() { thread.Start();}
		public void Cancel(){
			if (thread.IsAlive){
				//cancelLoading = true;
				thread.Join ();
				//cancelLoading = false;
			}
			lock (Host.CurrentInterface.CrowThreads)
				Host.CurrentInterface.CrowThreads.Remove (this);
		}
	}
}

