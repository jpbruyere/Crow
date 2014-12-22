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
        string _imgPath;

        [System.Xml.Serialization.XmlIgnore]
		public System.Drawing.Bitmap Bitmap {
			set {
				loadImage (value);                
			}
		}
		
        [System.Xml.Serialization.XmlAttributeAttribute("Path")]        
		public string ImagePath {
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
			_imgPath = ImagePath;
            loadImage(_imgPath);
        }

		public Image (string ImagePath)
            : base()
		{
			_imgPath = ImagePath;
            loadImage(_imgPath);
		}

		public Image (System.Drawing.Bitmap _bitmap)
            : base()
		{
			Bitmap = _bitmap;
		}

		protected override Size measureRawSize ()
		{
			if (image == null)
				loadImage (@"Images/Icons/icon_alert.gif");				

			return imgSize + Margin*2;
		}

		//load image via System.Drawing.Bitmap, cairo load png only
		public void loadImage (string path)
		{
            if (!File.Exists(path))
                return;
            
			loadImage (new System.Drawing.Bitmap (path));
            _imgPath = path;
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
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);
			float ratio = 1f;
			float widthRatio = (float)ClientRectangle.Width / imgSize.Width;
			float heightRatio = (float)ClientRectangle.Height / imgSize.Height;

			ratio = Math.Min (widthRatio, heightRatio);
			Rectangle rImg = ClientRectangle;

			gr.Scale (widthRatio, heightRatio);
			using (ImageSurface imgSurf = new ImageSurface(image, Format.Argb32, 
						imgSize.Width, imgSize.Height, 4 * imgSize.Width)) {
				gr.SetSourceSurface (imgSurf, (int)(rImg.X / widthRatio), (int)(rImg.Y / heightRatio));
				gr.Paint ();
			}
		}
	}
}
