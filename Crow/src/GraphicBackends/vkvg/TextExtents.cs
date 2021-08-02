// Copyright (c) 2018-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Runtime.InteropServices;

namespace Crow.Drawing
{
	[StructLayout (LayoutKind.Sequential)]
	public struct TextExtents
	{
		float xbearing;
		float ybearing;
		float width;
		float height;
		float xadvance;
		float yadvance;
		
		public float XBearing {
			get { return xbearing; }
			set { xbearing = value; }
		}
		
		public float YBearing {
			get { return ybearing; }
			set { ybearing = value; }
		}
		
		public float Width {
			get { return width; }
			set { width = value; }
		}
		
		public float Height {
			get { return height; }
			set { height = value; }
		}
		
		public float XAdvance {
			get { return xadvance; }
			set { xadvance = value; }
		}
		
		public float YAdvance {
			get { return yadvance; }
			set { yadvance = value; }
		}

		public override bool Equals (object obj)
		{
			if (obj is TextExtents)
				return this == (TextExtents)obj;
			return false;
		}

		public override int GetHashCode ()
		{
			return (int)XBearing ^ (int)YBearing ^ (int)Width ^ (int)Height ^ (int)XAdvance ^ (int)YAdvance;
		}

		public static bool operator == (TextExtents extents, TextExtents other)
		{
			return extents.XBearing == other.XBearing && extents.YBearing == other.YBearing && extents.Width == other.Width && extents.Height == other.Height && extents.XAdvance == other.XAdvance && extents.YAdvance == other.YAdvance;
		}

		public static bool operator != (TextExtents extents, TextExtents other)
		{
			return !(extents == other);
		}
	}
}
