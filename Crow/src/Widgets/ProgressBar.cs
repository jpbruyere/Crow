//
// ProgressBar.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crow.Cairo;
using System.Diagnostics;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	
	public class ProgressBar : NumericControl
    {
		#region CTOR
		protected ProgressBar () : base(){}
		public ProgressBar(Interface iface) : base(iface){}
		#endregion

		protected override void loadTemplate (Widget template)
		{
			
		}

		#region GraphicObject overrides
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (Maximum == 0)
				return;

			Rectangle rBack = ClientRectangle;
			rBack.Width = (int)((double)rBack.Width / Maximum * Value);
			Foreground.SetAsSource (gr, rBack);

			CairoHelpers.CairoRectangle(gr,rBack,CornerRadius);
			gr.Fill();
		}
		#endregion
    }
}
