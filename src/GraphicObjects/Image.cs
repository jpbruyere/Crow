using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.IO;
using System.Runtime.InteropServices;

namespace go
{
	public class Image : GraphicObject
	{

		byte[] image;
		Rsvg.Handle hSVG;
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
			if (image == null && hSVG == null)
				loadRessourceSvg ("go.image.icons.question_mark");				

			return imgSize + Margin*2;
		}
		void loadRessourceSvg(string resId)
		{
			Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resId);
			using (MemoryStream ms = new MemoryStream ()) {
				s.CopyTo (ms);
				hSVG = new Rsvg.Handle (ms.ToArray ());
				imgSize = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
				_imgPath = resId;
			}
		}
		//load image via System.Drawing.Bitmap, cairo load png only
		public void loadImage (string path)
		{
            if (!File.Exists(path))
                return;
            
			if (path.EndsWith (".svg", true,System.Globalization.CultureInfo.InvariantCulture)) {
				hSVG = new Rsvg.Handle (path);								 
				imgSize = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
			}else
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

			float widthRatio = (float)ClientRectangle.Width / imgSize.Width;
			float heightRatio = (float)ClientRectangle.Height / imgSize.Height;
			float ratio = Math.Min (widthRatio, heightRatio);

			Rectangle rImg = ClientRectangle;
			gr.Save ();
			gr.Scale (widthRatio, heightRatio);
			if (hSVG == null) {
				using (ImageSurface imgSurf = new ImageSurface (image, Format.Argb32, 
					                              imgSize.Width, imgSize.Height, 4 * imgSize.Width)) {
					gr.SetSourceSurface (imgSurf, (int)(rImg.X / widthRatio), (int)(rImg.Y / heightRatio));
					gr.Paint ();
				}
			} else {
				gr.Translate (rImg.X/widthRatio, rImg.Y/heightRatio);
				hSVG.RenderCairo (gr);
			}
			gr.Restore ();
		}
	}
}
