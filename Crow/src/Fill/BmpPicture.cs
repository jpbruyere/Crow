// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Runtime.InteropServices;


using Drawing2D;

namespace Crow
{

	/// <summary>
	/// Derived from FILL for loading and drawing bitmaps in the interface
	/// </summary>
	public class BmpPicture : Picture {
		byte[] image = null;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of BmpPicture.
		/// </summary>
		public BmpPicture () { }
		/// <summary>
		/// Initializes a new instance of BmpPicture by loading the image pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public BmpPicture (string path) : base (path) { }
		#endregion
		/// <summary>
		/// load the image for rendering from the path given as argument
		/// </summary>
		public override void load (Interface iFace) {
			if (iFace.sharedPictures.ContainsKey (Path)) {
				sharedPicture sp = iFace.sharedPictures[Path];
				image = (byte[])sp.Data;
				Dimensions = sp.Dims;
				return;
			}
			using (Stream stream = iFace.GetStreamFromPath (Path)) {
				image = iFace.Device.LoadBitmap (stream, out Size dimensions);
				Dimensions = dimensions;
				iFace.sharedPictures[Path] = new sharedPicture (image, Dimensions);
			}
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
		public override bool IsLoaded => image != null;
		public override void SetAsSource (Interface iFace, IContext ctx, Rectangle bounds = default(Rectangle))
		{
			if (image == null)
				load (iFace);

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

			using (ISurface tmp = iFace.Device.CreateSurface (bounds.Width, bounds.Height)) {
				using (IContext gr = iFace.Device.CreateContext (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					using (ISurface imgSurf = iFace.Device.CreateSurface (bounds.Width, bounds.Height)) {
						gr.SetSource (imgSurf, 0,0);
						gr.Paint ();
					}
				}
				ctx.SetSource (tmp);
			}
		}
		#endregion

		/// <summary>
		/// paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">used for svg only</param>
		public override void Paint (Interface iFace, IContext gr, Rectangle rect, string subPart = "")
		{
			if (image == null)
				load (iFace);

			double widthRatio = 1;
			double heightRatio = 1;

			if (Scaled){
				widthRatio = (double)rect.Width / Dimensions.Width;
				heightRatio = (double)rect.Height / Dimensions.Height;
			}

			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}

			gr.SaveTransformations ();

			gr.Translate (rect.Left,rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate ((rect.Width/widthRatio - Dimensions.Width)/2, (rect.Height/heightRatio - Dimensions.Height)/2);

			using (ISurface imgSurf = iFace.Device.CreateSurface (image, Dimensions.Width, Dimensions.Height)) {
				gr.SetSource (imgSurf, 0,0);
				gr.Paint ();
			}

			gr.RestoreTransformations ();
		}
	}
}

