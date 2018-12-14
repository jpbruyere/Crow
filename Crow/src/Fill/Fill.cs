//
// Fill.cs
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
using Cairo;

namespace Crow
{
	/// <summary>
	/// base class for drawing content to paint on backend
	/// </summary>
	public abstract class Fill
	{
		/// <summary>
		/// set content of fill as source for drawing operations on the backend context
		/// </summary>
		/// <param name="ctx">backend context</param>
		/// <param name="bounds">paint operation bounding box, unused for SolidColor</param>
		public abstract void SetAsSource (Context ctx, Rectangle bounds = default(Rectangle));
		public static object Parse (string s){
			if (string.IsNullOrEmpty (s))
				return null;
			if (s.Substring (1).StartsWith ("gradient", StringComparison.Ordinal))
				return (Gradient)Gradient.Parse (s);
			if (s.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture))
				return Parse (s);
			if (s.EndsWith (".png", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".jpg", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".jpeg", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".bmp", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".gif", true, System.Globalization.CultureInfo.InvariantCulture))
				return Parse (s);
			
			return (SolidColor)SolidColor.Parse (s);
		}
		public static implicit operator Color(Fill c){
			SolidColor sc = c as SolidColor;
			return sc == null ? default(Color) : sc.color;
		}
		public static implicit operator Fill(Color c){
			return new SolidColor (c);
		}

	}
}

