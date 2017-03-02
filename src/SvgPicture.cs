//
// SvgPicture.cs
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
using System.IO;
using Cairo;

namespace Crow
{
	public class SvgPicture : Picture
	{
		Rsvg.Handle hSVG;

		public SvgPicture ()
		{}
		public SvgPicture (string path) : base(path)
		{}
		protected override void loadFromStream (Stream stream)
		{
			using (MemoryStream ms = new MemoryStream ()) {
				stream.CopyTo (ms);

				hSVG = new Rsvg.Handle (ms.ToArray ());
				Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
			}
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Context ctx, Rectangle bounds = default(Rectangle))
		{
			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled){
				widthRatio = (float)bounds.Width / Dimensions.Width;
				heightRatio = (float)bounds.Height / Dimensions.Height;
			}

			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}

			using (ImageSurface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
				using (Cairo.Context gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					hSVG.RenderCairo (gr);
				}
				ctx.SetSource (tmp);
			}	
		}
		#endregion
			
		public override void Paint (Cairo.Context gr, Rectangle rect, string subPart = "")
		{
			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled) {
				widthRatio = (float)rect.Width / Dimensions.Width;
				heightRatio = (float)rect.Height / Dimensions.Height;
			}
			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}
				
			gr.Save ();

			gr.Translate (rect.Left,rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate (((float)rect.Width/widthRatio - Dimensions.Width)/2f, ((float)rect.Height/heightRatio - Dimensions.Height)/2f);

			if (string.IsNullOrEmpty (subPart))
				hSVG.RenderCairo (gr);
			else
				hSVG.RenderCairoSub (gr, "#" + subPart);
			
			gr.Restore ();			
		}
	}
}

