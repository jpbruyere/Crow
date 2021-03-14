using System;
using System.Collections.Generic;
using System.Text;

namespace Crow.src.Text
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

        public Char Peak () => buffer[curPos];
        public Char Read () => buffer[curPos++];
        public ReadOnlySpan<char> Get (int fromPosition) => buffer.Slice (fromPosition, curPos - fromPosition);
        public bool EndOfSpan => curPos >= buffer.Length;
    }
}
