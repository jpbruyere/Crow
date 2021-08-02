// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Drawing;

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
		public abstract void SetAsSource (Interface iFace, Context ctx, Rectangle bounds = default(Rectangle));
		public static Fill Parse (string s){
			ReadOnlySpan<char> tmp = s.AsSpan ();
			if (tmp.Length == 0)
				return null;
			if (tmp.Length > 8 && tmp.Slice (1, 8).SequenceEqual ("gradient"))
				return (Fill)Gradient.Parse (s);			
			if (tmp.EndsWith (".svg", StringComparison.OrdinalIgnoreCase) ||
				tmp.EndsWith (".png", StringComparison.OrdinalIgnoreCase) ||
				tmp.EndsWith (".jpg", StringComparison.OrdinalIgnoreCase) ||
				tmp.EndsWith (".jpeg", StringComparison.OrdinalIgnoreCase) ||
				tmp.EndsWith (".bmp", StringComparison.OrdinalIgnoreCase) ||
				tmp.EndsWith (".gif", StringComparison.OrdinalIgnoreCase))
				return Picture.Parse (s);

			return new SolidColor((Color)Color.Parse (s));
		}
		public static implicit operator Color(Fill c) => c is SolidColor sc ? sc.color : default;
		public static implicit operator Fill (Color c) => new SolidColor (c);
		public static implicit operator Fill (Colors c) => new SolidColor (c);

	}
}

