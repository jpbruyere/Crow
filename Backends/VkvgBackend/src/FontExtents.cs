// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace Crow.VkvgBackend
{
	[StructLayout (LayoutKind.Sequential)]
	internal struct FontExtents : IEquatable<FontExtents>
	{
		float ascent;
		float descent;
		float height;
		float maxXAdvance;
		float maxYAdvance;

		public float Ascent {
			get => ascent;
			set { ascent = value; }
		}

		public float Descent {
			get => descent;
			set { descent = value; }
		}

		public float Height {
			get => height;
			set { height = value; }
		}

		public float MaxXAdvance {
			get => maxXAdvance;
			set { maxXAdvance = value; }
		}

		public float MaxYAdvance {
			get => maxYAdvance;
			set { maxYAdvance = value; }
		}

		public FontExtents (float ascent, float descent, float height, float maxXAdvance, float maxYAdvance)
		{
			this.ascent = ascent;
			this.descent = descent;
			this.height = height;
			this.maxXAdvance = maxXAdvance;
			this.maxYAdvance = maxYAdvance;
		}

		public override int GetHashCode () => HashCode.Combine (ascent, descent, height, maxXAdvance, maxYAdvance);
		public override bool Equals (object obj) => obj is FontExtents fe ? Equals (fe) : false;

		public bool Equals(FontExtents other) =>
			ascent == other.ascent && descent == other.descent && height == other.height &&
			maxXAdvance == other.maxXAdvance && maxYAdvance == other.maxYAdvance;

		public static bool operator == (FontExtents extents, FontExtents other) => extents.Equals (other);
		public static bool operator != (FontExtents extents, FontExtents other) => !extents.Equals (other);
	}
}
