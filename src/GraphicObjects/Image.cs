using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace go
{
	public class Image : GraphicObject
	{
		Picture _pic;
		string _svgSub;

        [XmlAttributeAttribute("Path")]        
		public string Path {
			get { return _pic == null ? null : _pic.Path; }
			set {	
				try {
					LoadImage (value);
					_pic.KeepProportions = true;
				} catch (Exception ex) {
					Debug.WriteLine (ex.Message);
					_pic = null;
				}
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
		public Image (string _imgPath, Rectangle _bounds)
            : base(_bounds)
		{			
            LoadImage(_imgPath);
        }
		public Image (string _imgPath)
            : base()
		{			
            LoadImage(_imgPath);
		}
//		public Image (System.Drawing.Bitmap _bitmap)
//            : base()
//		{
//			_pic = new BmpPicture ();
//
//			LoadImage (_bitmap);
//		}
		#endregion

		#region Image Loading
		public void LoadImage (string path)
		{

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture ();
			else 
				_pic = new BmpPicture ();

			_pic.LoadImage (path);
		}
		#endregion

		#region GraphicObject overrides
		protected override Size measureRawSize ()
		{
//			if (_pic == null)
//				_pic = "#go.Images.Icons.IconAlerte.svg";

			return _pic.Dimensions + Margin * 2;
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (_pic == null)
				return;

			_pic.Paint (gr, ClientRectangle, _svgSub);
		}
		#endregion
	}
}
