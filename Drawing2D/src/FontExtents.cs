//
// from Mono.Cairo.FontExtents.cs
//
// Authors: Duncan Mak (duncan@ximian.com)
//          Hisham Mardam Bey (hisham.mardambey@gmail.com)
//
// (C) Ximian, Inc. 2003
//
// This is a simplistic binding of the Cairo API to C#. All functions
// in cairo.h are transcribed into their C# equivelants
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2018-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.InteropServices;

namespace Drawing2D
{
	[StructLayout (LayoutKind.Sequential)]
	public struct FontExtents : IEquatable<FontExtents>
	{
		double ascent;
		double descent;
		double height;
		double maxXAdvance;
		double maxYAdvance;

		public double Ascent {
			get => ascent;
			set { ascent = value; }
		}

		public double Descent {
			get => descent;
			set { descent = value; }
		}

		public double Height {
			get => height;
			set { height = value; }
		}

		public double MaxXAdvance {
			get => maxXAdvance;
			set { maxXAdvance = value; }
		}

		public double MaxYAdvance {
			get => maxYAdvance;
			set { maxYAdvance = value; }
		}

		public FontExtents (double ascent, double descent, double height, double maxXAdvance, double maxYAdvance)
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
