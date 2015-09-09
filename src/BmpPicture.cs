//
//  BmpPicture.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.IO;
using Cairo;

namespace go
{
	public class BmpPicture : Picture
	{
		byte[] image;

		public BmpPicture ()
		{}
		public BmpPicture (string path) : base(path)
		{}
		protected override void loadFromStream (Stream stream)
		{
			using (MemoryStream ms = new MemoryStream ()) {
				stream.CopyTo (ms);
				loadBitmap (new System.Drawing.Bitmap (ms));	
			}
		}

		//load image via System.Drawing.Bitmap, cairo load png only
		void loadBitmap (System.Drawing.Bitmap bitmap)
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
		}

		public override void Paint (Cairo.Context gr, Rectangle rect, string subPart = "")
		{
			float widthRatio = (float)rect.Width / Dimensions.Width;
			float heightRatio = (float)rect.Height / Dimensions.Height;
			float ratio = Math.Min (widthRatio, heightRatio);

//			if (KeepProportions)
//				widthRatio = heightRatio = ratio;

			Rectangle rImg = rect;
			gr.Save ();

			if (KeepProportions) {
				gr.Translate ((rect.Width - (float)Dimensions.Width * ratio)/2f, 
					(rect.Height - (float)Dimensions.Height * ratio)/2f);
				gr.Scale (ratio, ratio);
					
			}else
				gr.Scale (widthRatio, heightRatio);
			
			using (ImageSurface imgSurf = new ImageSurface (image, Format.Argb32, 
				Dimensions.Width, Dimensions.Height, 4 * Dimensions.Width)) {
				gr.SetSourceSurface (imgSurf, (int)(rImg.X / widthRatio), (int)(rImg.Y / heightRatio));
				gr.Paint ();
			}
			gr.Restore ();
		}
	}
}

