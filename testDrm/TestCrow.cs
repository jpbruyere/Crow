//
// TestCrow.cs
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
using Crow;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace testDrm
{
	public class TestCrow
	{
		//const string lib = "/mnt/data2/devel/crow/libcrow/bin/Debug/libcrow.so";
		const string lib = "libcrow.so";

		[StructLayout(LayoutKind.Sequential)]
		public struct testStruct {
			public object instance;
			public int a;
			public int b;
		}

		public class testClass {
			public string alpha = "this is a test string";
			public int a = 10,b=20;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		unsafe extern static string gimme();
		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void registerICall ();

		static void Main(){
			registerICall ();
			Console.WriteLine (gimme());

			/*using (Interface iface = new Interface ()) {
				Console.WriteLine ("is dirty: {0}", iface.IsDirty);
				iface.ProcessResize (new Rectangle (0, 0, 1024, 768));
				iface.LoadInterface ("#testDrm.ui.go.crow");
				iface.Update ();
				iface.DumpTo ("/home/jp/test.png");
			}*/
		}
	}
}

