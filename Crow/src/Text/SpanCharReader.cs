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

		/// <summary>
		/// Retrieve a span of that buffer from provided starting position to the current reader position.
		/// </summary>
		/// <param name="fromPosition"></param>
		/// <returns></returns>
        public ReadOnlySpan<char> Get (int fromPosition) => buffer.Slice (fromPosition, curPos - fromPosition);
        /// <summary>
        /// Current reader position is further the end of the buffer.
        /// </summary>
		public bool EndOfSpan => curPos >= buffer.Length;
		public bool TryPeak (char c) => !EndOfSpan && Peak == c;
		/// <summary>
		/// Try peak one char, return false if end of span, true otherwise.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool TryPeak (ref char c) {
			if (EndOfSpan)
				return false;
			c = buffer[curPos];
			return true;
		}
		/// <summary>
		/// test if next char is one of the provided one as parameter
		/// </summary>
		/// <param name="chars"></param>
		/// <returns></returns>
 		public bool IsNextCharIn (params char[] chars) {
			for (int i = 0; i < chars.Length; i++)
				if (chars[i] == buffer[curPos])
					return true;
			return false;
		}
		/// <summary>
		/// increment reader position just before the next end of line
		/// </summary>
		public void AdvanceUntilEol () {
			while(!EndOfSpan) {
				switch (Peak) {
					case '\x85':
					case '\x2028':
					case '\xA':
						return;
					case '\xD':
						int nextPos = curPos + 1;
						if (nextPos == buffer.Length || buffer[nextPos] == '\xA' || buffer[nextPos] == '\x85')
							return;
						break;
				}
				Advance ();
			}
		}
		/// <summary>
		/// Next char or pair of chars is end of line.
		/// </summary>
		/// <returns></returns>
		public bool Eol () {
			return Peak == '\x85' || Peak == '\x2028' || Peak == '\xA' || curPos + 1 == buffer.Length ||
				(Peak == '\xD' && (buffer [curPos + 1]  == '\xA' || buffer [curPos + 1]  == '\x85'));

		}
		/// <summary>
		/// next char sequence has already been tested as eol, advance 1 or two char depending on eol format.
		/// </summary>
		public void ReadEol () {
			if (Read () == '\xD')
				Advance();
		}
    }
}
