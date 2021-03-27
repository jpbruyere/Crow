// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
	public struct TextSpan : IEquatable<TextSpan>
	{
		public readonly int Start;
		public readonly int End;
		public TextSpan (int start, int end) {
			Start = start;
			End = end;
		}

		public bool IsEmpty => Start == End;
		public int Length => End - Start;

		public bool Equals(TextSpan other)
			=> Start == other.Start && End == other.End;
		public override bool Equals(object obj)
			=> obj is TextSpan ts ? Equals(ts) : false;

		public override int GetHashCode()
			=> HashCode.Combine(Start, End);
		public static bool operator ==(TextSpan left, TextSpan right)
			=> left.Equals (right);		
		public static bool operator !=(TextSpan left, TextSpan right)
			=> !left.Equals (right);
		public override string ToString() => $"{Start},{End}";
	}
}
