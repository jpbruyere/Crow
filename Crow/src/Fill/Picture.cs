// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;

using System.Collections.Generic;
using Crow.Drawing;

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
		/// path of the picture
		/// </summary>
		public string Path;
		/// <summary>
		/// unscaled dimensions fetched on loading
		/// </summary>
		public Size Dimensions { get; protected set; }
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

		/// <summary>
		/// abstract method to paint the image in the rectangle given in arguments according
		/// to the Scale and keepProportion parameters.
		/// </summary>
		/// <param name="gr">drawing Backend context</param>
		/// <param name="rect">bounds of the target surface to paint</param>
		/// <param name="subPart">used for svg only</param>
		public abstract void Paint(Interface iFace, Context ctx, Rectangle rect, string subPart = "");
		public abstract bool IsLoaded { get; }
		public abstract void load (Interface iface);
		#region Operators
		public static implicit operator Picture(string path) => Parse (path) as Picture;
		public static implicit operator string(Picture _pic) => _pic == null ? null : _pic.Path;
		#endregion

		public static new Picture Parse(string path)
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

