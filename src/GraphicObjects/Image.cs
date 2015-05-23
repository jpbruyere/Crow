using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.ComponentModel;

namespace go
{
	public class Image : GraphicObject
	{
		byte[] image;
		Rsvg.Handle hSVG;
		Size imgSize;
        string _imgPath;
		string _svgSub;
		
        [XmlAttributeAttribute("Path")]        
		public string ImagePath {
			get { return _imgPath; }
			set {
				_imgPath = value;
				LoadImage (_imgPath);
			}
		}

		[XmlAttributeAttribute()][DefaultValue(null)]
		public string SvgSub {
			get { return _svgSub; }
			set {
				_svgSub = value;
				registerForGraphicUpdate ();
			}
		}
			
		#region CTOR
		public Image () : base()
		{
		}
		public Image (string ImagePath, Rectangle _bounds)
            : base(_bounds)
		{
			_imgPath = ImagePath;
            LoadImage(_imgPath);
        }
		public Image (string ImagePath)
            : base()
		{
			_imgPath = ImagePath;
            LoadImage(_imgPath);
		}
		public Image (System.Drawing.Bitmap _bitmap)
            : base()
		{
			LoadImage (_bitmap);
		}
		#endregion

		#region Image Loading
		void loadFromRessource(string resId)
		{
			Stream stream = null;

			//first, search for ressource in main executable assembly
			stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(resId);
			if (stream == null)//try to find ressource in golib assembly				
				stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resId);
			if (stream == null)
				return;
			
			using (MemoryStream ms = new MemoryStream ()) {
				stream.CopyTo (ms);

				if (resId.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) {
					hSVG = new Rsvg.Handle (ms.ToArray ());
					imgSize = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
				} else
					LoadImage (new System.Drawing.Bitmap (ms));					
			}
		}
		void loadFromFile(string path)
		{
			if (!File.Exists(path))
				return;

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) {
				hSVG = new Rsvg.Handle (path);								 
				imgSize = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
			}else
				LoadImage (new System.Drawing.Bitmap (path));

		}
		public void LoadImage (string path)
		{
			hSVG = null;
			image = null;

			if (path.StartsWith ("#"))
				loadFromRessource (path.Substring (1));
			else
				loadFromFile (path);

			_imgPath = path;
		}
		//load image via System.Drawing.Bitmap, cairo load png only
		public void LoadImage (System.Drawing.Bitmap bitmap)
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
		}
		#endregion

		#region GraphicObject overrides
		protected override Size measureRawSize ()
		{
			if (image == null && hSVG == null) {
				loadFromRessource ("go.Images.Icons.IconAlerte.svg");
			}

			return imgSize + Margin * 2;
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
				if (string.IsNullOrEmpty (_svgSub))
					hSVG.RenderCairo (gr);
				else
					hSVG.RenderCairoSub (gr, "#" + _svgSub);
			}
			gr.Restore ();
		}
		#endregion
	}
}
