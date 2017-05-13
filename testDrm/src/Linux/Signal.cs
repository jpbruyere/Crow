//
// Signals.cs
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
using System.Runtime.InteropServices;

namespace Linux
{
	public enum Signal : int {
		SIGHUP		 = 1,
		SIGINT		 = 2,
		SIGQUIT		 = 3,
		SIGILL		 = 4,
		SIGTRAP		 = 5,
		SIGABRT		 = 6,
		SIGIOT		 = 6,
		SIGBUS		 = 7,
		SIGFPE		 = 8,
		SIGKILL		 = 9,
		SIGUSR1		= 10,
		SIGSEGV		= 11,
		SIGUSR2		= 12,
		SIGPIPE		= 13,
		SIGALRM		= 14,
		SIGTERM		= 15,
		SIGSTKFLT	= 16,
		SIGCHLD		= 17,
		SIGCONT		= 18,
		SIGSTOP		= 19,
		SIGTSTP		= 20,
		SIGTTIN		= 21,
		SIGTTOU		= 22,
		SIGURG		= 23,
		SIGXCPU		= 24,
		SIGXFSZ		= 25,
		SIGVTALRM	= 26,
		SIGPROF		= 27,
		SIGWINCH	= 28,
		/// <summary>same as SIGPOLL</summary>
		SIGIO		= 29,			
		SIGPWR		= 30,
		SIGSYS		= 31,
		SIGUNUSED	= 31
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SignalHandler(Signal signal);

	public static class Kernel
	{
		[DllImport("libc")]
		public static extern int signal(Signal s, SignalHandler handler);
	}
}

