// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Runtime.InteropServices;

namespace Drawing2D
{
	[StructLayout (LayoutKind.Sequential)]
	public struct TextExtents : IEquatable<TextExtents>
	{
		double xBearing;
		double yBearing;
		double width;
		double height;
		double xAdvance;
		double yAdvance;
		public TextExtents (double xBearing, double yBearing, double width, double height, double xAdvance, double yAdvance) {
			this.xBearing = xBearing;
			this.yBearing = yBearing;
			this.width = width;
			this.height = height;
			this.xAdvance = xAdvance;
			this.yAdvance = yAdvance;
		}
		public double XBearing {
			get => xBearing;
			set { xBearing = value; }
		}

		public double YBearing {
			get => yBearing;
			set { yBearing = value; }
		}

		public double Width {
			get => width;
			set { width = value; }
		}

		public double Height {
			get => height;
			set { height = value; }
		}

		public double XAdvance {
			get => xAdvance;
			set { xAdvance = value; }
		}

		public double YAdvance {
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
