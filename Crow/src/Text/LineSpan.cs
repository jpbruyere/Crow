// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow.Text
{
	public struct LineSpan : IEquatable<LineSpan>
	{
		public readonly CharLocation Start;
		public readonly CharLocation End;
		public LineSpan (CharLocation start, CharLocation end) {
			Start = start;
			End = end;
		}
		public bool IsEmpty => Start == End;

		public bool Equals(LineSpan other)
			=> Start == other.Start && End == other.End;
		public override bool Equals(object obj)
			=> obj is LineSpan ts ? Equals(ts) : false;

		public override int GetHashCode()
			=> HashCode.Combine(Start, End);
		public static bool operator ==(LineSpan left, LineSpan right)
			=> left.Equals (right);
		public static bool operator !=(LineSpan left, LineSpan right)
			=> !left.Equals (right);
		public override string ToString() => $"{Start},{End}";
	}
}
