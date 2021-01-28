using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Crow.Text
{
	[DebuggerDisplay ("{Start}, {Length}, {LengthInPixel}")]
	public struct TextLine : IComparable<TextLine>
	{
		public int Start;
		public int Length;
		public int LengthIncludingLineBreak;
		public int LengthInPixel;
		public int End => Start + Length;
		public int EndIncludingLineBreak => Start + LengthIncludingLineBreak;
		public int LineBreakLength => LengthIncludingLineBreak - Length;
		public bool HasLineBreak => LineBreakLength > 0;
		public TextLine (int start, int end, int endIncludingLineBreak) {
			Start = start;
			Length = end - start;
			LengthIncludingLineBreak = endIncludingLineBreak - start;
			LengthInPixel = -1;
		}
		public TextLine (int start) {
			Start = start;
			Length = 0;
			LengthIncludingLineBreak = 0;
			LengthInPixel = -1;
		}
		public TextLine WithStartOffset (int start) => new TextLine (Start + start, End, EndIncludingLineBreak);

		public int CompareTo (TextLine other) => Start - other.Start;
        /*public ReadOnlySpan<char> GetSubString (string str) {			
    if (Start >= str.Length)
        return "".AsSpan();
    return str.Length - Start < Length ?
        str.AsSpan().Slice (Start, Length) :
        str.AsSpan().Slice (Start);
}
public ReadOnlySpan<char> GetSubStringIncludingLineBreak (string str) {
    if (Start >= str.Length)
        return "".AsSpan ();
    return (str.Length - Start < LengthIncludingLineBreak) ?
        str.AsSpan ().Slice (Start, LengthIncludingLineBreak) :
        str.AsSpan ().Slice (Start);
}*/
    }
}
