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
using vkvg;

namespace Crow
{
	/// <summary>
	/// Derived from FILL for loading and drawing SVG images in the interface
	/// </summary>
	public class SvgPicture : Picture
	{
		IntPtr nsvgImage;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of SvgPicture.
		/// </summary>
		public SvgPicture ()
		{}
		/// <summary>
		/// Initializes a new instance of SvgPicture by loading the SVG file pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public SvgPicture (string path) : base(path)
		{
			Load ();
		}
		#endregion

		void Load ()
		{
			using (StreamReader sr = new StreamReader(Interface.CurrentInterface.GetStreamFromPath (Path))) {
				nsvgImage = Interface.CurrentInterface.dev.LoadSvgFragment (sr.ReadToEnd ());
			}

			int w, h;
			NativeMethods.nsvg_get_size (nsvgImage, out w, out h);
			Dimensions.Width = w;
			Dimensions.Height = h;
		}

		public void LoadSvgFragment (string fragment) {
			throw new NotImplementedException ();
			/*hSVG = new Rsvg.Handle (System.Text.Encoding.Unicode.GetBytes(fragment));
			Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);*/
		}

		#region implemented abstract members of Fill

		public override void SetAsSource (Context ctx, Rectangle bounds = default(Rectangle))
		{
			if (nsvgImage == IntPtr.Zero)
				return;
			throw new NotImplementedException ();
			/*float widthRatio = 1f;
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

			using (Surface tmp = new Surface (Interface.CurrentInterface.dev, bounds.Width, bounds.Height)) {
				using (Context gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width / widthRatio - Dimensions.Width) / 2, (bounds.Height / heightRatio - Dimensions.Height) / 2);
					gr.SetSourceSurface (svgSurface, 0, 0);
					gr.Paint ();
				}
				ctx.SetSource (tmp);
			}*/
		}
		#endregion

		/// <summary>
		/// paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">limit rendering to svg part named 'subPart'</param>
		public override void Paint (Context gr, Rectangle rect, string subPart = "")
		{
			if (nsvgImage == IntPtr.Zero)
				return;

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

			gr.Translate (rect.Left, rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate ((rect.Width / widthRatio - Dimensions.Width) / 2, (rect.Height / heightRatio - Dimensions.Height) / 2);

			gr.RenderSvg (nsvgImage, subPart);

			gr.Restore ();
		}
	}
}

