//
//  Picture.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.IO;
using Cairo;

namespace Crow
{
	public abstract class Picture
	{
		public string Path;
		public Size Dimensions;
		public bool KeepProportions = false;
		public bool Scale = true;

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

