using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Crow.Text
{
	public enum LineBreakKind
    {
		Undefined,
		Unix,
		Windows,
		Other
    }
	/// <summary>
	/// represent a single line span with end of line handling.
	/// </summary>
	[DebuggerDisplay ("{Start}, {Length}, {LengthInPixel}")]
	public struct TextLine : IComparable<TextLine>
	{
		/// <summary>
		/// Line start absolute character position.
		/// </summary>
		public int Start;
		/// <summary>
		/// Line's character count not including linebreak if any.
		/// </summary>
		public int Length;
		/// <summary>
		/// Total line's Character count including linebreak characters if any.
		/// </summary>
		public int LengthIncludingLineBreak;
		/// <summary>
		/// Cached line's length in pixel. If not yet computed by renderer, value is '-1'
		/// </summary>
		public int LengthInPixel;
		/// <summary>
		/// Absolute end character position just before linebreak if any.
		/// </summary>
		public int End => Start + Length;
		/// <summary>
		/// Absolute line's end position after linebreak if any.
		/// </summary>
		public int EndIncludingLineBreak => Start + LengthIncludingLineBreak;
		/// <summary>
		/// Character count of the linebreak, 0 if no linebreak.
		/// </summary>
		public int LineBreakLength => LengthIncludingLineBreak - Length;
		/// <summary>
		/// True line has a linebreak, false otherwise.
		/// </summary>
		public bool HasLineBreak => LineBreakLength > 0;
		/// <summary>
		/// Create a new TextLine span using the absolute start and end character positions.
		/// </summary>
		public TextLine (int start, int end, int endIncludingLineBreak) {
			Start = start;
			Length = end - start;
			LengthIncludingLineBreak = endIncludingLineBreak - start;
			LengthInPixel = -1;
		}
		/// <summary>
		/// Create an empty line span without linebreak starting at absolute charater position given in argument.
		/// </summary>
		/// <param name="start"></param>
		public TextLine (int start) {
			Start = start;
			Length = 0;
			LengthIncludingLineBreak = 0;
			LengthInPixel = -1;
		}
		/// <summary>
		/// Set a new line's length and reset computed length in pixel to '-1'.
		/// </summary>
		/// <param name="newLength"></param>
		public void SetLength (int newLength) {
			LengthInPixel = -1;
			Length = newLength;
        }
		/// <summary>
		/// Create a new TextLine span with a start offset, length in pixel is reseted.
		/// </summary>
		/// <param name="start"></param>
		/// <returns></returns>
		public TextLine WithStartOffset (int start) => new TextLine (Start + start, End, EndIncludingLineBreak);		
		public int CompareTo (TextLine other) => Start - other.Start;
    }
}
