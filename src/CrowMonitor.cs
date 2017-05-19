//
// CrowMonitor.cs
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
using System.Diagnostics;

namespace Crow
{
	public class NamedMutex {
		public string Name;
		public NamedMutex (string name){
			Name = name;
		}
	}
	public static class CrowMonitor
	{
		public static Stopwatch timer = Stopwatch.StartNew();

		public static bool TryEnter (NamedMutex mutex, string ctxName = "?"){
			Console.WriteLine("{3}:TRY LCK:{0} => {1} ({2})", Thread.CurrentThread.Name, mutex.Name, ctxName, timer.ElapsedTicks);
			bool locking = Monitor.TryEnter (mutex);
			if (locking)
				Console.WriteLine("{2}:\tLOCKED :{0} => {1} mutex", Thread.CurrentThread.Name, mutex.Name, timer.ElapsedTicks);
			return locking;

		}
		public static void Enter (NamedMutex mutex, string ctxName = "?") {
			Console.WriteLine("{3}:WAIT   :{0} => {1} ({2})", Thread.CurrentThread.Name, mutex.Name, ctxName, timer.ElapsedTicks);
			Monitor.Enter (mutex);
			Console.WriteLine("{3}:\tLOCKED :{0} => {1} mutex ({2})", Thread.CurrentThread.Name, mutex.Name, ctxName, timer.ElapsedTicks);
		}
		public static void Exit (NamedMutex mutex) {
			Monitor.Exit (mutex);
			Console.WriteLine("{2}:\tRELEASE:{0} => {1} mutex", Thread.CurrentThread.Name, mutex.Name, timer.ElapsedTicks);
		}
	}
}

