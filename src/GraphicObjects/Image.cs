using System;
using Cairo;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;


namespace Crow
{
	public class Image : GraphicObject
	{
		Picture _pic;
		string _svgSub;
		bool scaled;
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool Scaled {
			get { return scaled; }
			set {
				if (scaled == value)
					return;
				scaled = value; 
				NotifyValueChanged ("Scaled", scaled);
				if (_pic == null)
					return;
				_pic.Scaled = scaled;
				RegisterForGraphicUpdate ();
			}
		} 
		bool keepProps;
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool KeepProportions {
			get { return keepProps; }
			set {
				if (keepProps == value)
					return;
				keepProps = value; 
				NotifyValueChanged ("KeepProportions", keepProps);
				if (_pic == null)
					return;
				_pic.KeepProportions = keepProps;
				RegisterForGraphicUpdate ();
			}
		} 
        [XmlAttributeAttribute("Path")]        
		public string Path {
			get { return _pic == null ? null : _pic.Path; }
			set {	
				try {
					if (string.IsNullOrEmpty(value)){
						_pic = null;
						return;
					}
					lock(CurrentInterface.LayoutMutex){
						LoadImage (value);
						_pic.Scaled = scaled;
						_pic.KeepProportions = keepProps;
					}
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
				RegisterForGraphicUpdate ();
			}
		}
			
		#region CTOR
		public Image () : base()
		{
		}
		#endregion

		#region Image Loading
		public void LoadImage (string path)
		{

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture ();
			else 
				_pic = new BmpPicture ();

			_pic.LoadImage (path);
			RegisterForGraphicUpdate ();
		}
		#endregion

		#region GraphicObject overrides
		protected override int measureRawSize (LayoutingType lt)
		{
			if (_pic == null)
				_pic = "#Crow.Images.Icons.IconAlerte.svg";
			//TODO:take scalling in account
			if (lt == LayoutingType.Width)
				return _pic.Dimensions.Width + 2 * Margin;
			else
				return _pic.Dimensions.Height + 2 * Margin;			
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
