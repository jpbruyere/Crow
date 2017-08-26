//
// BasicTests.cs
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using Crow.SDL2;

using Native;

using XCBConnection = System.IntPtr;
using XCBSetup = System.IntPtr;
using XCBScreen = System.IntPtr;
using XCBWindow = System.UInt32;
using XCBColorMap = System.UInt32;
using XCBVisualId = System.UInt32;


namespace Tests
{
	class test_xcb
	{
//		static IntPtr findVisual(XCBConnection conn, XCBVisualId visualId){			
//			Xcb.GenericIterator iter = Xcb.SetupRootsIterator (Xcb.GetSetup (conn));
//			for (; iter.rem > 0; Xcb.ScreenNext(ref iter)) {
//				
//				Xcb.Screen screen = Marshal.PtrToStructure<Xcb.Screen> (iter.data);
//				Xcb.GenericIterator depthIter = Xcb.ScreenAllowedDepthsIterator (ref screen);
//				for (; depthIter.rem > 0; Xcb.DepthNext (ref depthIter)) {
//					Xcb.Depth depth = Marshal.PtrToStructure<Xcb.Depth> (depthIter.data);
//					Console.WriteLine ("Depth = {0}\n\tVisuals: ", depth.depth);
//					Xcb.GenericIterator visualIter = Xcb.DepthVisualsIterator (ref depth);
//					for (; visualIter.rem > 0; Xcb.VisualtypeNext (ref visualIter)) {
//						Xcb.VisualType visual = Marshal.PtrToStructure<Xcb.VisualType> (visualIter.data);
//						Console.WriteLine ("{0}, ", visual.visual_id);
//						if (visualId == visual.visual_id)
//							return visualIter.data;
//					}
//				}
//			}
//			return IntPtr.Zero;
//		}

		[STAThread]
		static void Main ()
		{
			XCBConnection conn = Xcb.Connect ();

			Xcb.Result res = Xcb.ConnectionHasError (conn);

			if (res != Xcb.Result.SUCCESS) 
				Console.WriteLine ("error");

			IntPtr screenPtr = Xcb.SetupRootsIterator (Xcb.GetSetup (conn)).data;
			XCBWindow win = Xcb.GenerateId (conn);

			Xcb.Screen scr = Marshal.PtrToStructure<Xcb.Screen> (screenPtr);

			int[] mask = {1,(int)(Xcb.EventMask.EXPOSURE|Xcb.EventMask.KEY_PRESS|Xcb.EventMask.BUTTON_PRESS)};

			int maskSize =sizeof(int)*mask.Length;

			//IntPtr pMask = Marshal.AllocHGlobal(mask.Length * sizeof(uint));
			IntPtr pMask = Marshal.AllocHGlobal(sizeof(int)*mask.Length);
			Marshal.Copy(mask, 0, pMask, mask.Length);

			uint result = Xcb.CreateWindow (conn, 0, win, scr.root, 10, 10, 200, 200, 1, (ushort)Xcb.WindowClass.INPUT_OUTPUT, scr.root_visual,
				Xcb.Cw.OVERRIDE_REDIRECT | Xcb.Cw.EVENT_MASK, pMask);
			
			Marshal.FreeHGlobal(pMask);

			result = Xcb.MapWindow (conn, win);

			IntPtr visual = Xcb.FindVisual (conn, scr.root_visual);

			Console.WriteLine ("visual={0:X}", visual);
			if (visual == IntPtr.Zero)
				throw new Exception ("visual not found");

			Cairo.XcbSurface surf = new Cairo.XcbSurface (conn, win, visual, 200, 200);
			if (surf.Status != Cairo.Status.Success)
				Console.WriteLine ("surf.status: " + surf.Status);

			bool quit = false;

			Xcb.Flush (conn);

			while (!quit) {
				IntPtr pEvt = Xcb.WaitForEvent (conn);
				Xcb.GenericEvent e = Marshal.PtrToStructure<Xcb.GenericEvent> (pEvt);

				switch (e.response_type) {
				case Xcb.EventType.EXPOSE:
					using (Cairo.Context ctx = new Cairo.Context (surf)) {
						ctx.SetSourceRGBA (0.0, 0.0, 1.0, 1.0);
						ctx.Rectangle (10, 10, 100, 100);
						ctx.Fill ();
					}
					surf.Flush ();
					break;
				case Xcb.EventType.BUTTON_PRESS:
					quit = true;
					break;
				default:
					break;
				}
				Xcb.Flush (conn);
			}

			surf.Finish ();
			surf.Dispose ();

			Xcb.Disconnect (conn);
		}
	}
}
