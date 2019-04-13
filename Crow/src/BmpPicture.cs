//
// BmpPicture.cs
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
	/// Derived from FILL for loading and drawing bitmaps in the interface
	/// </summary>
	public class BmpPicture : Picture
	{
		byte[] image = null;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of BmpPicture.
		/// </summary>
		public BmpPicture ()
		{}
		/// <summary>
		/// Initializes a new instance of BmpPicture by loading the image pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public BmpPicture (string path) : base(path)
		{}
		#endregion
		/// <summary>
		/// load the image for rendering from the path given as argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		void Load ()
		{			
			if (sharedResources.ContainsKey (Path)) {
				sharedPicture sp = sharedResources [Path];
				image = (byte[])sp.Data;
				Dimensions = sp.Dims;
				return;
			}
			using (Stream stream = Interface.StaticGetStreamFromPath (Path)) {				
				//loadBitmap (new System.Drawing.Bitmap (stream));	
			}
			sharedResources [Path] = new sharedPicture (image, Dimensions);
		}

		//load image via System.Drawing.Bitmap, cairo load png only
		/*void loadBitmap (System.Drawing.Bitmap bitmap)
		{
			if (bitmap == null)
				return;

			System.Drawing.Imaging.BitmapData data = bitmap.LockBits
				(new System.Drawing.Rectangle (0, 0, bitmap.Width, bitmap.Height),
					System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			Dimensions = new Size (bitmap.Width, bitmap.Height);

			int stride = data.Stride;
			int bitmapSize = Math.Abs (data.Stride) * bitmap.Height;

			image = new byte[bitmapSize];
			System.Runtime.InteropServices.Marshal.Copy (data.Scan0, image, 0, bitmapSize);

			bitmap.UnlockBits (data);           
		}*/

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

			//using (ImageSurface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
			//	using (Context gr = new Context (tmp)) {
			//		gr.Translate (bounds.Left, bounds.Top);
			//		gr.Scale (widthRatio, heightRatio);
			//		gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

			//		using (ImageSurface imgSurf = new ImageSurface (image, Format.Argb32, 
			//			Dimensions.Width, Dimensions.Height, 4 * Dimensions.Width)) {
			//			gr.SetSourceSurface (imgSurf, 0,0);
			//			gr.Paint ();
			//		}
			//	}
			//	ctx.SetSource (tmp);
			//}				
		}
		#endregion

		/// <summary>
		/// paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">used for svg only</param>
		public override void Paint (Context gr, Rectangle rect, string subPart = "")
		{
			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled){
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
			gr.Translate ((rect.Width/widthRatio - Dimensions.Width)/2, (rect.Height/heightRatio - Dimensions.Height)/2);
			
			//using (Surface imgSurf = new Surface (. image, Format.Argb32, 
			//	Dimensions.Width, Dimensions.Height, 4 * Dimensions.Width)) {
			//	gr.SetSourceSurface (imgSurf, 0,0);
			//	gr.Paint ();
			//}
			gr.Restore ();
		}
	}
}

