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
using vkvg;
using System.Collections.Generic;

namespace Crow
{
	/// <summary>
	/// store data and dimensions for resource sharing
	/// </summary>
	internal class sharedPicture {
		//TODO: restructure this whith clever conceptual classes
		public object Data;
		public Size Dims;
		public sharedPicture (object _data, Size _dims){
			Data = _data;
			Dims = _dims;
		}
	}
	/// <summary>
	/// virtual class for loading and drawing picture in the interface
	/// 
	/// Every loaded resources are stored in a dictonary with their path as key and shared
	/// among interface elements
	/// </summary>
	public abstract class Picture : Fill
	{
		/// <summary>
		/// share a single store for picture resources among usage in different controls
		/// </summary>
		internal static Dictionary<string, sharedPicture> sharedResources = new Dictionary<string, sharedPicture>();

		/// <summary>
		/// path of the picture
		/// </summary>
		public string Path;
		/// <summary>
		/// unscaled dimensions fetched on loading
		/// </summary>
		public Size Dimensions;
		/// <summary>
		/// if true and image has to be scalled, it will be scaled in both direction
		/// equaly
		/// </summary>
		public bool KeepProportions = false;
		/// <summary>
		/// allow or not the picture to be scalled on request by the painter
		/// </summary>
		public bool Scaled = true;

		#region CTOR
		/// <summary>
		/// Initializes a new instance of Picture.
		/// </summary>
		public Picture ()
		{
		}
		/// <summary>
		/// Initializes a new instance of Picture by loading the image pointed by the path argument
		/// </summary>
		/// <param name="path">image path, may be embedded</param>
		public Picture (string path)
		{
			Path = path;
		}
		#endregion

		#region Image Loading
		/// <summary>
		/// load the image for rendering from the stream given as argument
		/// </summary>
		/// <param name="stream">picture stream</param>
		//public abstract void Load(Interface iface, string path);
		#endregion

		/// <summary>
		/// abstract method to paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">used for svg only</param>
		public abstract void Paint(Context ctx, Rectangle rect, string subPart = "");

		#region Operators
		public static implicit operator Picture(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			
			Picture _pic = null;

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture (path);
			else 
				_pic = new BmpPicture (path);

			return _pic;
		}
		public static implicit operator string(Picture _pic)
		{
			return _pic == null ? null : _pic.Path;
		}
		#endregion

		public static object Parse(string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			
			Picture _pic = null;

			if (path.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture)) 
				_pic = new SvgPicture (path);
			else 
				_pic = new BmpPicture (path);

			return _pic;
		}
		public override string ToString ()
		{
			return Path;
		}
	}
}

