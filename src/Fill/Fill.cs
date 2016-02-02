using System;
using System.Collections.Generic;

namespace Crow
{
	public abstract class Fill
	{
		public abstract void SetAsSource (Cairo.Context ctx, Rectangle bounds = default(Rectangle));
		public static object Parse (string s){
			if (string.IsNullOrEmpty (s))
				return null;
			if (s.Substring (1).StartsWith ("gradient"))
				return (Gradient)Gradient.Parse (s);
			if (s.EndsWith (".svg", true, System.Globalization.CultureInfo.InvariantCulture))
				return SvgPicture.Parse (s);
			if (s.EndsWith (".png", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".jpg", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".jpeg", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".bmp", true, System.Globalization.CultureInfo.InvariantCulture) ||
			    s.EndsWith (".gif", true, System.Globalization.CultureInfo.InvariantCulture))
				return BmpPicture.Parse (s);
			
			return (SolidColor)SolidColor.Parse (s);
		}

		public static implicit operator Fill(Color c){
			return new SolidColor (c);
		}

	}
}

