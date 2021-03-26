// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow.Text
{
    public static class Extensions
    {
		public static ReadOnlySpan<char> GetLine (this string str, TextLine ls) {
			if (ls.Start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (ls.Start, ls.Length);
		}
		public static ReadOnlySpan<char> GetLine (this string str, TextLine ls, int offset) {
			int start = ls.Start + offset;
			if (start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (start, ls.Length);

		}
		public static ReadOnlySpan<char> GetLineIncludingLineBreak (this string str, TextLine ls) {
			if (ls.Start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (ls.Start, ls.LengthIncludingLineBreak);
		}
		public static ReadOnlySpan<char> GetLineBreak (this string str, TextLine ls) {
			if (ls.LineBreakLength == 0)
				return "".AsSpan ();
			return str.AsSpan ().Slice (ls.End, ls.LineBreakLength);
		}
		public static ReadOnlySpan<char> GetLineIncludingLineBreak (this string str, TextLine ls, int offset) {
			int start = ls.Start + offset;
			if (start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (start, ls.LengthIncludingLineBreak);
		}

		public static ReadOnlySpan<char> ToCharSpan (this LineBreakKind lineBreak) {
			switch (lineBreak) {
			case LineBreakKind.Unix:
				return "\n".AsSpan ();
			case LineBreakKind.Windows:
				return "\r\n".AsSpan ();
			case LineBreakKind.Other:
				return "\r".AsSpan ();
			default:
				return "\r\n".AsSpan ();
			}
		}

	}
}
