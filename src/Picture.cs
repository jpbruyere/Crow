//
// Picture.cs
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
using System.IO;
using Cairo;

namespace Crow
{
	public abstract class Picture : Fill
	{
		public string Path;
		public Size Dimensions;
		public bool KeepProportions = false;
		public bool Scaled = true;

		public Picture ()
		{
		}
		public Picture (string path)
		{
			LoadImage (path);
		}

		#region Image Loading
		public void LoadImage (string path)
		{
			loadFromStream (Interface.GetStreamFromPath (path));

			Path = path;
		}
			
		protected abstract void loadFromStream(Stream stream);
		#endregion

		public abstract void Paint(Context ctx, Rectangle rect, string subPart = "");


		public static implicit operator Picture(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			
			Picture _pic = null;

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture ();
			else 
				_pic = new BmpPicture ();

			_pic.LoadImage (path);			

			return _pic;
		}
		public static implicit operator string(Picture _pic)
		{
			return _pic == null ? null : _pic.Path;
		}

		public static object Parse(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			
			Picture _pic = null;

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture ();
			else 
				_pic = new BmpPicture ();

			_pic.LoadImage (path);			

			return _pic;
		}
		public override string ToString ()
		{
			return Path;
		}
	}
}

