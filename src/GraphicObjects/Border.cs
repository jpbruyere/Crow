//
// Border.cs
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
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;

namespace Crow
{
	public class Border : Container
	{
		#region CTOR
		public Border () : base(){}
		#endregion

		#region private fields
		int _borderWidth;
		#endregion

		#region public properties
		[XmlAttributeAttribute()][DefaultValue(1)]
		public virtual int BorderWidth {
			get { return _borderWidth; }
			set {
				_borderWidth = value;
				RegisterForGraphicUpdate ();
			}
		}
		#endregion

		#region GraphicObject override
		[XmlIgnore]public override Rectangle ClientRectangle {
			get {
				Rectangle cb = base.ClientRectangle;
				cb.Inflate (- BorderWidth);
				return cb;
			}
		}

		protected override int measureRawSize (LayoutingType lt)
		{
			int tmp = base.measureRawSize (lt);
			return tmp < 0 ? tmp : tmp + 2 * BorderWidth;
		}
		unsafe protected override void onDraw (Cairo.Context gr)
		{
			Rectangle rBack = new Rectangle (nativeHnd->Slot.Size);

			//rBack.Inflate (-Margin);
//			if (BorderWidth > 0) 
//				rBack.Inflate (-BorderWidth / 2);			

			Background.SetAsSource (gr, rBack);
			CairoHelpers.CairoRectangle(gr, rBack, CornerRadius);
			gr.Fill ();

			if (BorderWidth > 0) {				
				Foreground.SetAsSource (gr, rBack);
				CairoHelpers.CairoRectangle(gr, rBack, CornerRadius, BorderWidth);
			}

			gr.Save ();
			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle,Math.Max(0.0, CornerRadius-Margin));
				gr.Clip ();
			}

			if (child != null)
				child.Paint (ref gr);
			gr.Restore ();
		}		
		#endregion
	}
}

