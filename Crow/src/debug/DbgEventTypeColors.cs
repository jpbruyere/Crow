//
// DbgEventTypeColors.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using Crow.Cairo;
using System.Linq;

namespace Crow
{
	#if DEBUG_LOG
	public class DbgEventTypeColors : GraphicObject
	{
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			DbgEvtType[] types = DbgLogViewer.colors.Keys.ToArray ();
			Color[] colors = DbgLogViewer.colors.Values.ToArray ();

			Rectangle r = ClientRectangle;
			FontExtents fe = gr.FontExtents;

			double penY = fe.Height + r.Top;
			double penX = (double)r.Left;


			for (int i = 0; i < types.Length; i++) {
				string n = types [i].ToString();
				Color c = colors[i];
				Foreground.SetAsSource (gr);

				gr.MoveTo (penX + 25.0, penY - fe.Descent);
				gr.ShowText (n);

				Rectangle rc = new Rectangle((int)penX, (int)(penY - fe.Height), 20, (int)fe.Height);
				rc.Inflate (-2);
				gr.Rectangle (rc);
				gr.StrokePreserve ();

				gr.SetSourceColor (c);
				gr.Fill ();

				penY += fe.Height;

			}
		}
	}
	#endif
}

