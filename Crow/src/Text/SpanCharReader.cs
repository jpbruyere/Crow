// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
    public ref struct SpanCharReader
    {
        int curPos;
        ReadOnlySpan<char> buffer;

        public SpanCharReader (string text) {
            buffer = text.AsSpan ();
            curPos = 0;
        }

        public int CurrentPosition => curPos;

        public void Seek (int position) => curPos = position;

        public Char Peak => buffer[curPos];
        public Char Read () => buffer[curPos++];
		public bool TryRead (out char c) {
			if (EndOfSpan) {
				c = default;
				return false;
			}
			c = Read();
			return true;
		}
		public bool TryRead (char c) => EndOfSpan ? false : Read() == c;
					
		public ReadOnlySpan<char> Read (int length) => buffer.Slice (curPos += length, length);
		public void Advance (int increment = 1) => curPos += increment;
		public bool TryAdvance (int increment = 1) {			
			curPos += increment;
			return curPos < buffer.Length;
		}
		
		public bool TryReadUntil (ReadOnlySpan<char> str, StringComparison comparison = StringComparison.Ordinal) {
			int startPos = curPos;
			while (curPos < buffer.Length - str.Length) {
				if (buffer[curPos] == str[0] && buffer.Slice(curPos + 1, str.Length - 1).Equals(str.Slice (1), comparison))
					return true;
				curPos++;			
			}
			return false;
		}
		public bool TryReadUntil (char c) {
			int startPos = curPos;
			while (curPos < buffer.Length && buffer[curPos] != c)
				curPos++;			
			return curPos < buffer.Length;
		}
		public bool TryRead (int length, out ReadOnlySpan<char> str) {
			if (length < buffer.Length) {
				str = buffer.Slice (curPos += length, length);
				return true;
			}
			str = default;
			return false;
		}
		
		/// <summary>
		/// Try read expected string and advance reader position in any case
		/// </summary>
		/// <param name="expectedString">expected string</param>
		/// <param name="comparison">comparison type</param>
		/// <returns>true if expected string is found</returns>
		public bool TryRead (ReadOnlySpan<char> expectedString, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
			if (buffer.Length < curPos + expectedString.Length) {
				curPos = buffer.Length;
				return false;
			}
			bool res = buffer.Slice(curPos, expectedString.Length).Equals (expectedString, comparison);
			curPos += expectedString.Length;
			return res;
		}
		public bool TryPeak (ReadOnlySpan<char> expectedString, StringComparison comparison = StringComparison.Ordinal) =>
			 (buffer.Length < curPos + expectedString.Length)? false :
						buffer.Slice(curPos, expectedString.Length).Equals (expectedString, comparison);			
			
        public ReadOnlySpan<char> Get (int fromPosition) => buffer.Slice (fromPosition, curPos - fromPosition);
        public bool EndOfSpan => curPos >= buffer.Length;
		public bool TryPeak (char c) => !EndOfSpan && Peak == c;
		public bool TryPeak (ref char c) {
			if (EndOfSpan)
				return false;
			c = buffer[curPos];
			return true;
		}
 		public bool IsNextCharIn (params char[] chars) {
			for (int i = 0; i < chars.Length; i++)
				if (chars[i] == buffer[curPos])
					return true;
			return false;
		}
    }
}
