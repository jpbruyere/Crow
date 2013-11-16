using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using OpenTK.Graphics.OpenGL;
using Cairo;
using System.IO;

namespace go
{
	public class Image : GraphicObject
	{

		byte[] image;
		Size imgSize;

		public System.Drawing.Bitmap imgBitmap {
			set {
				loadImage (value);                
			}
		}

		string _imgPath;
        
		public string imgPath {
			get { return _imgPath; }
			set {
				_imgPath = value;
				loadImage (_imgPath);
			}
		}

		public Image () : base()
		{
		}

		public Image (string ImagePath, Rectangle _bounds)
            : base(_bounds)
		{
			imgPath = ImagePath;
		}

		public Image (string ImagePath)
            : base()
		{
			imgPath = ImagePath;
		}

		public Image (System.Drawing.Bitmap _bitmap)
            : base()
		{
			imgBitmap = _bitmap;
		}

		public override Size measureRawSize ()
		{
			if (image == null)
				loadImage (directories.rootDir + @"Images/Icons/icon_alert.gif");				

			return new Size (imgSize.Width + borderWidth + margin, imgSize.Height + borderWidth + margin);
		}

		//load image via System.Drawing.Bitmap, cairo load png only
		public void loadImage (string path)
		{
			if (File.Exists (path))
				loadImage (new System.Drawing.Bitmap (path));
		}

		public void loadImage (System.Drawing.Bitmap bitmap)
		{
			if (bitmap == null)
				return;

			System.Drawing.Imaging.BitmapData data = bitmap.LockBits
                (new System.Drawing.Rectangle (0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			imgSize = new Size (bitmap.Width, bitmap.Height);

			int stride = data.Stride;
			int bitmapSize = Math.Abs (data.Stride) * bitmap.Height;

			image = new byte[bitmapSize];
			System.Runtime.InteropServices.Marshal.Copy (data.Scan0, image, 0, bitmapSize);

			bitmap.UnlockBits (data);
			//bitmap.Dispose();            
		}

		internal override void updateGraphic ()
		{            
			//int maxUV = 0;
			float ratio = 1f;

            
			//Image.WriteToPng(directories.rootDir + @"test.png");
			//maxUV = Math.Max(Image.Width, Image.Height);

			float widthRatio = (float)clientBounds.Width / imgSize.Width;
			float heightRatio = (float)clientBounds.Height / imgSize.Height;

			ratio = Math.Min (widthRatio, heightRatio);

			int stride = 4 * renderBounds.Width;
            
			//init  bmp with widget background and border
			base.updateGraphic ();

			using (ImageSurface draw =
                new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride)) {
				using (Context gr = new Context(draw)) {
					//Rectangle r = new Rectangle(0, 0, renderBounds.Width, renderBounds.Height);
					gr.Antialias = Antialias.Subpixel;

					Rectangle rImg = clientBounds.Clone;

					gr.Scale (widthRatio, heightRatio);
					using (ImageSurface imgSurf = new ImageSurface(image, Format.Argb32, imgSize.Width, imgSize.Height, 4 * imgSize.Width)) {
						gr.SetSourceSurface (imgSurf, (int)(rImg.X / widthRatio), (int)(rImg.Y / heightRatio));

						gr.Paint ();
					}
					draw.Flush ();
				}
				//draw.WriteToPng(directories.rootDir + @"test.png");
			}

			//Image.Dispose();

			//registerForRedraw();
		}



	}
}
