// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Runtime.InteropServices;

namespace Crow.VkvgBackend
{
	[StructLayout (LayoutKind.Sequential)]
	internal struct TextExtents : IEquatable<TextExtents>
	{
		float xBearing;
		float yBearing;
		float width;
		float height;
		float xAdvance;
		float yAdvance;

		public float XBearing {
			get => xBearing;
			set { xBearing = value; }
		}

		public float YBearing {
			get => yBearing;
			set { yBearing = value; }
		}

		public float Width {
			get => width;
			set { width = value; }
		}

		public float Height {
			get => height;
			set { height = value; }
		}

		public float XAdvance {
			get => xAdvance;
			set { xAdvance = value; }
		}

		public float YAdvance {
			get => yAdvance;
			set { yAdvance = value; }
		}

		public override int GetHashCode () =>
			HashCode.Combine (xBearing, yBearing, width, height, xAdvance, yAdvance);
		public override bool Equals (object obj) => obj is TextExtents te ? Equals (te) : false;

		public bool Equals(TextExtents other) =>
			xBearing == other.xBearing && yBearing == other.yBearing && width == other.width && height == other.height &&
			xAdvance == other.xAdvance && yAdvance == other.yAdvance;
		public static bool operator == (TextExtents extents, TextExtents other) => extents.Equals (other);
		public static bool operator != (TextExtents extents, TextExtents other )=> !extents.Equals (other);
	}
}
