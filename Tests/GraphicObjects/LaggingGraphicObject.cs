//
// SimpleGauge.cs
//
// Author:
//       jp <>
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
using System.ComponentModel;
using Cairo;
using System.Diagnostics;

namespace Crow
{
	public class LaggingGraphicObject : GraphicObject
	{
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Stopwatch t = Stopwatch.StartNew ();

			IntPtr ctxHnd = gr.Handle;

			for (int i = 0; i < 1000000; i++) {
				
				/*gr.SetSourceRGBA (1.0,0.0,0.0,1.0);
				gr.Rectangle (0, 0, 100, 100);
				gr.Stroke ();*/

				/*Cairo.NativeMethods.SetSourceRGBA (ctxHnd, 1.0,0.0,0.0,1.0);
				Cairo.NativeMethods.Rectangle (ctxHnd, 0, 0, 100, 100);
				Cairo.NativeMethods.Stroke (ctxHnd);*/


				//tests.MainClass.cairo_set_rgba_func (ctxHnd, 1.0,0.0,0.0,1.0);
				//tests.MainClass.cairo_rect_func (ctxHnd, 0, 0, 100, 100);
				//tests.MainClass.cairo_stroke_func (ctxHnd);

				tests.MainClass.cairo_rgba_internal (ctxHnd, 1.0,0.0,0.0,1.0);
				tests.MainClass.cairo_rect_internal (ctxHnd, 0, 0, 100, 100);
				tests.MainClass.cairo_stroke_internal (ctxHnd);
				//tests.MainClass.cairo_stroke (ctxHnd);
				//tests.MainClass.cairo_stroke_icall (ctxHnd);



			}


			t.Stop ();

			Console.WriteLine("elapsed ticks = {0}", t.ElapsedTicks);

			//System.Threading.Thread.Sleep (1000);

		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{


			RegisterForRedraw ();
		}
	}		
}

