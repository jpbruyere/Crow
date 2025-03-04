// Copyright (c) 2021-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using System.Collections.Generic;
using Crow.Text;
using Crow;

namespace CrowEditBase
{
	public class TextBuffer {
		static int bufferExpension = 100;
		int length;
		Memory<char> buffer;
		ReadOnlyMemory<char> origBuffer;
		LineCollection lines;
		public bool mixedLineBreak = false;
		public string lineBreak = null;

		internal LineCollection GetLineListCopy() => new LineCollection(lines.ToArray());
		public Span<char> Span => buffer.Span.Slice(0, length);
		public ReadOnlySpan<char> ReadOnlySpan => buffer.Span.Slice(0, length);
		public bool IsEmpty => length == 0;
		public bool IsDirty => !origBuffer.Span.Equals(Span, StringComparison.Ordinal);
		public int LinesCount => lines.Count;
		public int Length => length;
		public ReadOnlyMemory<char> ReadOnlyCopy {
			get {
				return ReadOnlySpan.ToArray();
			}
		} 
		public void ResetDirtyState () {
			origBuffer = Span.ToArray();
		}
		public TextBuffer(ReadOnlySpan<char> origText) {
			length = origText.Length;
			buffer = new char[length + bufferExpension];
			origText.CopyTo(buffer.Span);
			lines = new LineCollection (10);
			if (IsEmpty)
				lines.Add (new TextLine (0, 0, 0));
			else
				lines.Update (Span);
			ResetDirtyState ();
		}
		public void Update (TextChange change) {
			ReadOnlySpan<char> orig = buffer.Span;
			char[] newBuff = null;
			Span<char> tmp;
			if (buffer.Length < length + change.CharDiff) {
				newBuff = new char[length + change.CharDiff + bufferExpension];
				tmp = newBuff;
				orig.Slice(0, change.Start).CopyTo(tmp);
				if (change.CharDiff == 0)
					orig.Slice(change.End, length - change.End).CopyTo(tmp.Slice(change.End));
			} else
				tmp = buffer.Span;
			
			if (change.CharDiff != 0)
				orig.Slice(change.End, length - change.End).CopyTo(tmp.Slice(change.End2));
			if (!string.IsNullOrEmpty (change.ChangedText))
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			
			if (newBuff != null)
				buffer = newBuff;
			length += change.CharDiff;

			lines.Update (change);
		}
		public string GetLineBreak () {
			if (string.IsNullOrEmpty (lineBreak)) {
				mixedLineBreak = false;

				if (lines.Count == 0 || lines[0].LineBreakLength == 0)
					lineBreak = Environment.NewLine;
				else {
					lineBreak = ReadOnlySpan.GetLineBreak (lines[0]).ToString ();
					for (int i = 1; i < lines.Count; i++) {
						if (!ReadOnlySpan.GetLineBreak (lines[i]).SequenceEqual (lineBreak)) {
							mixedLineBreak = true;
							break;
						}
					}
				}
			}
			return lineBreak;
		}
		public CharLocation GetLocation (int absolutePosition) => lines.GetLocation (absolutePosition);
		public TextLine GetLine (int index) => lines[index];
		public void SetLine (int index, TextLine line) => lines[index] = line;
		public ReadOnlySpan<char> GetText (TextLine line) => GetText(line.Span);
		public ReadOnlySpan<char> GetText (TextSpan textSpan) => buffer.Span.Slice(textSpan.Start, textSpan.Length);
		public int GetAbsolutePosition (CharLocation loc) => lines.GetAbsolutePosition (loc);
		public CharLocation EndLocation => new CharLocation (lines.Count - 1, lines[lines.Count - 1].Length);
        public override string ToString() => ReadOnlySpan.ToString();
    }
}