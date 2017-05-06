//
// HelloWorld.cs
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
using Cairo;
using System.Diagnostics;

namespace Tests2
{
	static class Test
	{
		const int H = 600;
		const int W = 800;
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("Single Surface Instance");
			Stopwatch sw = Stopwatch.StartNew ();
			Surface surf = new ImageSurface (Format.Argb32, W, H);
			for (int i = 0; i < 1000; i++) {
				using (Context ctx = new Context (surf)) {
					clearSurf (ctx);
					ctx.Rectangle (50, 50, 50, 50);
					ctx.SetSourceRGB (1, 0, 0);
					ctx.Fill ();
				}
			}
			sw.Stop ();
			Console.WriteLine ("elapse: {0} ticks, {1} ms", sw.ElapsedTicks, sw.ElapsedMilliseconds);

			Console.WriteLine ("Multiple Surface Instances");
			sw = Stopwatch.StartNew ();
			for (int i = 0; i < 1000; i++) {
				byte[] bmp = new byte[W * H * 4];
				using (Surface s = new ImageSurface (bmp, Format.Argb32, W, H, W * 4)){
					using (Context ctx = new Context (s)) {
						//clearSurf (ctx);
						ctx.Rectangle (50, 50, 50, 50);
						ctx.SetSourceRGB (1, 0, 0);
						ctx.Fill ();
					}
				}
			}
			sw.Stop ();
			Console.WriteLine ("elapse: {0} ticks, {1} ms", sw.ElapsedTicks, sw.ElapsedMilliseconds);

		}

		static void clearSurf(Context ctx){
			ctx.Operator = Operator.Clear;
			ctx.SetSourceRGB (1, 1, 1);
			ctx.Rectangle (0, 0, W, H);
			ctx.Fill ();
			ctx.Operator = Operator.Over;
		}
	}
}