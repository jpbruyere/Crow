using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
    public struct TextChange
    {
        public readonly int Start;
        public readonly int Length;
        public readonly string ChangedText;

        public int End => Start + Length;
        public TextChange (int position, int length, string changedText) {
            Start = position;
            Length = length;
            ChangedText = changedText;
        }
        public TextChange Inverse (string src)
            => new TextChange (Start, string.IsNullOrEmpty (ChangedText) ? 0 : ChangedText.Length,
                Length == 0 ? "" : src.AsSpan (Start, Length).ToString ());
    }
}
