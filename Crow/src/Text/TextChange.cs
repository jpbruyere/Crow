// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
    public struct TextChange
    {
        public readonly int Start;
        public readonly int Length;
        public string ChangedText;

        public int End => Start + Length;
        public int End2 => End + CharDiff;

		public int CharDiff => string.IsNullOrEmpty (ChangedText) ? - Length : ChangedText.Length - Length;
        public bool IsEmpty => string.IsNullOrEmpty (ChangedText) && Length == 0;
        public TextChange (int position, int length, string changedText) {
            Start = position;
            Length = length;
            ChangedText = changedText;
        }
        public TextChange (int position, int length, ReadOnlySpan<char> changedText) {
            Start = position;
            Length = length;
            ChangedText = changedText.ToString();
        }
        public TextChange Inverse (ReadOnlySpan<char> src)
            => new TextChange (Start, string.IsNullOrEmpty (ChangedText) ? 0 : ChangedText.Length,
                Length == 0 ? "" : src.Slice (Start, Length).ToString ());
        public override string ToString() => $"{Start},{ChangedText}";
    }
}
