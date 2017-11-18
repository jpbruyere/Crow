//
// Image.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
		bool scaled, keepProps;
		double opacity;

		#region Public properties
		[XmlAttributeAttribute][DefaultValue(true)]
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
		[XmlAttributeAttribute][DefaultValue(true)]
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
        [XmlAttributeAttribute]
		public string Path {
			get { return _pic == null ? "" : _pic.Path; }
			set {
				if (value == Path)
					return;
				try {
					if (string.IsNullOrEmpty(value))
						Picture = null;
					else {
						lock(CurrentInterface.LayoutMutex){
							LoadImage (value);
						}
					}
				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
					_pic = null;
				}
				NotifyValueChanged ("Path", Path);
			}
		}
		[XmlAttributeAttribute]
		public string SvgSub {
			get { return _svgSub; }
			set {
				if (_svgSub == value)
					return;
				_svgSub = value;
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute]
		public Picture Picture {
			get { return _pic; }
			set {
				if (_pic == value)
					return;
				_pic = value;
				NotifyValueChanged ("Picture", _pic);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(1.0)]
		public virtual double Opacity {
			get { return opacity; }
			set {
				if (opacity == value)
					return;
				opacity = value;
				NotifyValueChanged ("Faded", opacity);
				RegisterForRedraw ();
			}
		}
		#endregion

		#region CTOR
		public Image () : base()
		{
		}
		#endregion

		#region Image Loading
		public void LoadImage (string path)
		{
			Picture pic;
			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture))
				pic = new SvgPicture ();
			else
				pic = new BmpPicture ();

			pic.LoadImage (path);
			pic.Scaled = scaled;
			pic.KeepProportions = keepProps;

			Picture = pic;
		}
		#endregion

		#region GraphicObject overrides
		protected override int measureRawSize (LayoutingType lt)
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

			_pic.Paint (gr, ClientRectangle, _svgSub);

			if (Opacity<1.0) {
				gr.SetSourceRGBA (0.0, 0.0, 0.0, 1.0-Opacity);
				gr.Operator = Operator.DestOut;
				gr.Rectangle (ClientRectangle);
				gr.Fill ();
				gr.Operator = Operator.Over;
			}
		}
		#endregion
	}
}
