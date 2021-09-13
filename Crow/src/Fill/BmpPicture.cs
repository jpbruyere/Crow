// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Runtime.InteropServices;

using Crow.Drawing;

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
				load (stream);
				//loadBitmap (new System.Drawing.Bitmap (stream));
				iFace.sharedPictures[Path] = new sharedPicture (image, Dimensions);
			}
		}
		void load (Stream stream) {
#if STB_SHARP
			StbImageSharp.ImageResult stbi = StbImageSharp.ImageResult.FromStream (stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
			image = new byte[stbi.Data.Length];
	#if VKVG
			Array.Copy (stbi.Data, image, stbi.Data.Length);
	#else
			//rgba to argb for cairo.
			for (int i = 0; i < stbi.Data.Length; i += 4) {
				image[i] = stbi.Data[i + 2];
				image[i + 1] = stbi.Data[i + 1];
				image[i + 2] = stbi.Data[i];
				image[i + 3] = stbi.Data[i + 3];
			}
	#endif
			Dimensions = new Size (stbi.Width, stbi.Height);
#else
				using (StbImage stbi = new StbImage (stream)) {
					image = new byte [stbi.Size];
	#if VKVG
					Marshal.Copy (stbi.Handle, image, 0, stbi.Size);
	#else
					for (int i = 0; i < stbi.Size; i+=4) {
						//rgba to argb for cairo. ???? looks like bgra to me.
						image [i] = Marshal.ReadByte (stbi.Handle, i + 2);
						image [i + 1] = Marshal.ReadByte (stbi.Handle, i + 1);
						image [i + 2] = Marshal.ReadByte (stbi.Handle, i);
						image [i + 3] = Marshal.ReadByte (stbi.Handle, i + 3);
					}
	#endif
					Dimensions = new Size (stbi.Width, stbi.Height);
				}
#endif
		}
		internal static sharedPicture CreateSharedPicture (Stream stream) {
			BmpPicture pic = new BmpPicture ();
			pic.load (stream);
			return new sharedPicture (pic.image, pic.Dimensions);
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
		public override void SetAsSource (Interface iFace, Context ctx, Rectangle bounds = default(Rectangle))
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

#if VKVG
			using (Surface tmp = new Surface (iFace.vkvgDevice, bounds.Width, bounds.Height)) {
#else
			using (Surface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
#endif
				using (Context gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);
#if VKVG
					using (Surface imgSurf = new Surface (iFace.vkvgDevice, image, Dimensions.Width, Dimensions.Height)) 
#else
					using (Surface imgSurf = new ImageSurface (image, Format.Argb32, Dimensions.Width, Dimensions.Height, 4 * Dimensions.Width)) 
#endif
					{
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
		public override void Paint (Interface iFace, Context gr, Rectangle rect, string subPart = "")
		{
			if (image == null)
				load (iFace);

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

			//gr.Save ();

			Matrix m = gr.Matrix;

			gr.Translate (rect.Left,rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate ((rect.Width/widthRatio - Dimensions.Width)/2, (rect.Height/heightRatio - Dimensions.Height)/2);

#if VKVG
			using (Surface imgSurf = new Surface (iFace.vkvgDevice, image, Dimensions.Width, Dimensions.Height)) 
#else
			using (Surface imgSurf = new ImageSurface (image, Format.Argb32, Dimensions.Width, Dimensions.Height, 4 * Dimensions.Width)) 
#endif			
			{				
				gr.SetSource (imgSurf, 0,0);
				gr.Paint ();
			}

			gr.Matrix = m;
			//gr.Restore ();
		}
	}
}

