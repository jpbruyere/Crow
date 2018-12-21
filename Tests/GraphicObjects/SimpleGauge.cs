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

namespace Tutorials
{
	public class SimpleGauge : Widget
	{		
		public SimpleGauge () : base() {}
		public SimpleGauge (Interface iface): base (iface){}

		int level;

		[DefaultValue(0)]
		public int Level
		{
			get { return level; }
			set {
				if (level == value)
					return;
				level = value;
				NotifyValueChanged ("Level", level);
			}
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			int height = r.Height / 100 * level;
			r.Y += r.Height - height;
			r.Height = height;
			Foreground.SetAsSource (gr);
			gr.Rectangle (r);
			gr.Fill ();
		}
	}
}

