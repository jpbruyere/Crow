// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
    public class Text
    {
        char[] buffer;
		int length;
        TextLine[] lines;

		public Text (string text, int capacity = -1) {
			if (string.IsNullOrEmpty (text)) {
				buffer = new char[capacity > 0 ? capacity : 0];
				length = 0;
			} else {
				if (capacity >= text.Length) {
					buffer = new char[capacity];
					text.AsSpan ().CopyTo (buffer.AsSpan ());
				} else
					buffer = text.ToCharArray ();

				length = text.Length;
				updateLines ();
			}
        }
		public Text (int capacity) {
			buffer = new char[capacity];
			length = 0;
        }

		void updateLines () {
			if (length == 0) {
				lines = new TextLine[] { new TextLine (0, 0, 0) };
				return;
			}

			List<TextLine> _lines = new List<TextLine> ();
			int start = 0, i = 0;
			while (i < length) {
				char c = buffer[i];
				if (c == '\r') {
					if (++i < length) {
						if (buffer[i] == '\n')
							_lines.Add (new TextLine (start, i - 1, ++i));
						else
							_lines.Add (new TextLine (start, i - 1, i));
					} else
						_lines.Add (new TextLine (start, i - 1, i));
					start = i;
				} else if (c == '\n') {
					if (++i < length) {
						if (buffer[i] == '\r')
							_lines.Add (new TextLine (start, i - 1, ++i));
						else
							_lines.Add (new TextLine (start, i - 1, i));
					} else
						_lines.Add (new TextLine (start, i - 1, i));
					start = i;

				} else if (c == '\u0085' || c == '\u2028' || c == '\u2029')
					_lines.Add (new TextLine (start, i - 1, i));
				else
					i++;
			}

			if (start < i)
				_lines.Add (new TextLine (start, length, length));
			else
				_lines.Add (new TextLine (length, length, length));

			lines = _lines.ToArray ();
		}

		public void Append (ReadOnlySpan<char> str) {
			
			if (length + str.Length > buffer.Length) {
				char[] newbuff = new char[length + str.Length];
				Span<char> tmp = newbuff.AsSpan ();
				buffer.AsSpan ().CopyTo (tmp);
				str.CopyTo (tmp.Slice (length));
				buffer = newbuff;				
            } else {
				str.CopyTo (buffer.AsSpan ().Slice (length));
			}
			length = length + str.Length;
		}
		public void Insert (ReadOnlySpan<char> str, int start) {
			if (length + str.Length > buffer.Length) {
				char[] newbuff = new char[length + str.Length];
				Span<char> tmp = newbuff.AsSpan ();
				buffer.AsSpan ().Slice (0, start).CopyTo (tmp);
				tmp = tmp.Slice (start);
				str.CopyTo (tmp);
				tmp = tmp.Slice (str.Length);
				buffer.AsSpan ().Slice (start, length - start).CopyTo (tmp);				
				buffer = newbuff;
			} else {
				buffer.AsSpan ().Slice (start, length - start).CopyTo (buffer.AsSpan ().Slice (start + str.Length));
				str.CopyTo (buffer.AsSpan ().Slice (start, str.Length));
			}
			length = length + str.Length;
		}
		public void Remove (int start, int length) {
			Span<char> tmp = buffer.AsSpan ();
			int end = Math.Min (this.length, start + length);
			if (end < this.length)
				tmp.Slice (end, this.length - end).CopyTo (tmp.Slice (start, this.length - end));
			this.length = start + (this.length - end);
        }

		public void Update (TextChange change) {

        }

		public override string ToString () => new string (buffer, 0, length);
    }
}
