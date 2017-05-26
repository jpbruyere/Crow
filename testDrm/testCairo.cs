//
// testCairo.cs
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
using Cairo;
using System.Runtime.InteropServices;

namespace testDrm
{
	public class testCairo
	{

		static void Main(){
			test ();
		}
		static void test(){
			using (Surface s = new ImageSurface (Format.Argb32, 500,500)){
				using (Context ctx = new Context (s)) {
					ctx.Rectangle (100, 100, 200, 200);
					ctx.Rectangle (50, 50, 50, 50);
					ctx.Clip ();
					ctx.Rectangle (0, 0, 400, 400);
					ctx.SetSourceRGB (1, 0, 0);
					ctx.Fill ();
					//IntPtr rects = ctx.GetClipRectangles ();

					RectangleList rl = ctx.GetClipRectangles ();//(RectangleList)Marshal.PtrToStructure (rects, typeof(RectangleList));
					Console.WriteLine ("num rects: {0}", rl.NumRectangles);
				}
				s.WriteToPng ("/home/jp/test.png");
			}
		}

	}
}

