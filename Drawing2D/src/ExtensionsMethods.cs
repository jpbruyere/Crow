// Copyright (c) 2013-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Drawing2D;

namespace Crow
{
	public static class ExtensionsMethods
	{
		public static void Raise(this EventHandler handler, object sender, EventArgs e)
		{
			handler?.Invoke (sender, e);
		}
		public static void Raise<T>(this EventHandler<T> handler, object sender, T e)
		{
			handler?.Invoke (sender, e);
		}
		public static byte[] GetBytes(this string str)
		{
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}
		public static bool IsWhiteSpace (this char c)
		{
			return c == '\t' || char.IsWhiteSpace (c);
		}		
		public static bool IsWhiteSpaceOrNewLine (this char c)
		{
			return c == '\t' || c.IsAnyLineBreakCharacter() || char.IsWhiteSpace (c);
		}
		internal static bool IsAnyLineBreakCharacter (this char c)
			=> c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029';
		public static ReadOnlySpan<char> GetLine (this ReadOnlySpan<char> str, TextLine ls) {
			if (ls.Start >= str.Length)
				return "".AsSpan ();
			return str.Slice (ls.Start, ls.Length);
		}
		public static ReadOnlySpan<char> GetLine (this ReadOnlySpan<char> str, TextLine ls, int offset) {
			int start = ls.Start + offset;
			if (start >= str.Length)
				return "".AsSpan ();
			return str.Slice (start, ls.Length);
		}
		public static ReadOnlySpan<char> GetLineIncludingLineBreak (this string str, TextLine ls) {
			if (ls.Start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (ls.Start, ls.LengthIncludingLineBreak);
		}
		public static ReadOnlySpan<char> GetLineBreak (this ReadOnlySpan<char> str, TextLine ls) {
			if (ls.LineBreakLength == 0)
				return "".AsSpan ();
			return str.Slice (ls.End, ls.LineBreakLength);
		}
		public static ReadOnlySpan<char> GetLineIncludingLineBreak (this string str, TextLine ls, int offset) {
			int start = ls.Start + offset;
			if (start >= str.Length)
				return "".AsSpan ();
			return str.AsSpan ().Slice (start, ls.LengthIncludingLineBreak);
		}
		public static int CountLeadingWhiteSpaces (this ReadOnlySpan<char> str) {
			int i = 0;
			while (i < str.Length && str[i].IsWhiteSpace())
				i++;
			return i;
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

