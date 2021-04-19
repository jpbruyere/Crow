// Copyright (c) 2013-2019  Jean-Philippe Bruyère jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;


namespace Crow
{
	/// <summary>
	/// Base widget to display an image. Accepts bitmaps and SVGs.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class Image : Scalable
	{
		Picture _pic;
		string _svgSub;

		double opacity;

		#region Public properties
		/// <summary>
		/// If false, original size will be kept in any case.
		/// </summary>
		[DefaultValue(true)]
		public override bool Scaled {
			get => base.Scaled;
			set {
				if (scaled == value)
					return;
				scaled = value;
				NotifyValueChangedAuto (scaled);
				if (_pic == null)
					return;
				_pic.Scaled = scaled;
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// If image is scaled, proportions will be preserved.
		/// </summary>
		[DefaultValue(true)]
		public override bool KeepProportions {
			get { return keepProps; }
			set {
				if (keepProps == value)
					return;
				keepProps = value;
				NotifyValueChangedAuto (keepProps);
				if (_pic == null)
					return;
				_pic.KeepProportions = keepProps;
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// Image file path, may be on disk or embedded. Accepts bitmaps or SVG drawings.
		/// </summary>        
		public string Path {
			get { return _pic == null ? "" : _pic.Path; }
			set {
				if (value == Path)
					return;
				try {
					if (string.IsNullOrEmpty(value))
						Picture = null;
					else {
						//lock(IFace.LayoutMutex){
							LoadImage (value);
						//}
					}
				} catch (Exception ex) {
					Debug.WriteLine (ex.Message);
					_pic = null;
				}
				NotifyValueChangedAuto (Path);
			}
		}
		/// <summary>
		/// Used only for svg images, repaint only node named referenced in SvgSub.
		/// If null, all the svg is rendered
		/// </summary>		
		public string SvgSub {
			get { return _svgSub; }
			set {
				if (_svgSub == value)
					return;
				_svgSub = value;
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// Object holding the image data once loaded, may be used directely to pupulate this control without 
		/// specifying a path.
		/// </summary>		
		public Picture Picture {
			get { return _pic; }
			set {
				if (_pic == value)
					return;
				_pic = value;
				NotifyValueChangedAuto (_pic);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// Opacity parameter for the image
		/// </summary>
		// TODO:could be moved in GraphicObject
		[DefaultValue(1.0)]
		public virtual double Opacity {
			get { return opacity; }
			set {
				if (opacity == value)
					return;
				opacity = value;
				NotifyValueChangedAuto (opacity);
				RegisterForRedraw ();
			}
		}
		#endregion

		#region CTOR
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.Image"/> class.
		/// </summary>
		protected Image () : base(){}
		/// <summary>
		/// Initializes a new instance of the <see cref="Crow.Image"/> class from code
		/// </summary>
		/// <param name="iface">interface to bound to</param>
		public Image (Interface iface) : base(iface)
		{
		}
		#endregion

		#region Image Loading
		public void LoadImage (string path)
		{
			Picture pic;
			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture))
				pic = new SvgPicture (path);
			else
				pic = new BmpPicture (path);


			pic.Scaled = scaled;
			pic.KeepProportions = keepProps;

			Picture = pic;
		}
		#endregion

		#region GraphicObject overrides
		public override int measureRawSize (LayoutingType lt)
		{
			if (_pic == null)
				return 2 * Margin;
				//_pic = "#Crow.Images.Icons.IconAlerte.svg";
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

			_pic.Paint (IFace, gr, ClientRectangle, _svgSub);

			if (Opacity<1.0) {
				gr.SetSource (0.0, 0.0, 0.0, 1.0-Opacity);
				gr.Operator = Operator.DestOut;
				gr.Rectangle (ClientRectangle);
				gr.Fill ();
				gr.Operator = Operator.Over;
			}
		}
		#endregion
	}
}
