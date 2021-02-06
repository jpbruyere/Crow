using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Crow.Text
{
    public class LineCollection : IList<TextLine>
    {
        TextLine[] lines;
        int length;

        #region CTOR
        public LineCollection (int capacity) {
            lines = new TextLine[capacity];
            length = 0;
        }
        public LineCollection (TextLine[] _lines, int capacity = -1) {
            if (capacity >= _lines.Length) {
                lines = new TextLine[capacity];
                _lines.AsSpan ().CopyTo (lines);
            } else
                lines =_lines;
            
            length = _lines.Length;
        }
        
        public LineCollection (string _text, int capacity = 4) : this (capacity) {
            Update (_text.AsSpan ());
        }
        #endregion

        public void Update (ReadOnlySpan<char> _text) {
            length = 0;
            int start = 0, i = 0;
            while (i < _text.Length) {
                char c = _text[i];
                if (c == '\r') {
                    if (++i < _text.Length) {
                        if (_text[i] == '\n')
                            Add (new TextLine (start, i - 1, ++i));
                        else
                            Add (new TextLine (start, i - 1, i));
                    } else
                        Add (new TextLine (start, i - 1, i));
                    start = i;
                } else if (c == '\n') {
                    if (++i < _text.Length) {
                        if (_text[i] == '\r')
                            Add (new TextLine (start, i - 1, ++i));
                        else
                            Add (new TextLine (start, i - 1, i));
                    } else
                        Add (new TextLine (start, i - 1, i));
                    start = i;

                } else if (c == '\u0085' || c == '\u2028' || c == '\u2029')
                    Add (new TextLine (start, i - 1, i));
                else
                    i++;
            }

            if (start < i)
                Add (new TextLine (start, _text.Length, _text.Length));
            else
                Add (new TextLine (_text.Length, _text.Length, _text.Length));
        }

        public void Update (TextChange change) {
            CharLocation locStart = GetLocation (change.Start);
            int charsDiff = change.ChangedText.Length - change.Length;
            int lineEnd = locStart.Line;
            while (lineEnd < length - 1 && change.End >= lines[lineEnd + 1].Start)
                lineEnd++;
            int columnEnd = change.End - lines[lineEnd].Start;
            int lineEndLineBreakLength = lines[lineEnd].LineBreakLength;

            LineCollection newLines = new LineCollection (change.ChangedText);
            int linesDiff = newLines.length - 1 - (lineEnd - locStart.Line);
            TextLine endTl = lines[lineEnd];

            if (linesDiff < 0)
                RemoveAt (locStart.Line + 1, -linesDiff);
            else if (linesDiff > 0) {
                for (int i = 0; i < linesDiff; i++)
                    Insert (locStart.Line + 1, default);
            }

            int remainingColumns = endTl.Length - columnEnd;
            lineEnd += linesDiff;
            lines[lineEnd].SetLength (0);
            lines[locStart.Line].SetLength (locStart.Column + newLines[0].Length);
            lines[lineEnd].Length += remainingColumns;
            if (newLines.Count > 1) {
                lines[lineEnd].Length += newLines[newLines.Count - 1].Length;                
                lines[locStart.Line].LengthIncludingLineBreak = lines[locStart.Line].Length + newLines[0].LineBreakLength;
            }
            lines[lineEnd].LengthIncludingLineBreak = lines[lineEnd].Length + endTl.LineBreakLength;

            for (int i = 1; i < newLines.Count - 1; i++) {
                int l = locStart.Line + i;
                lines[l] = newLines[i];
                lines[l].Start = lines[l - 1].EndIncludingLineBreak;
            }
            if (lineEnd > 0)
                lines[lineEnd].Start = lines[lineEnd - 1].EndIncludingLineBreak;

            //shift start for remaining lines
            for (int i = lineEnd + 1; i < length; i++)
                lines[i].Start += charsDiff;            
        }
        public int GetAbsolutePosition (CharLocation loc) => lines[loc.Line].Start + loc.Column;        
        public CharLocation GetLocation (int absolutePosition) {
            TextLine tl = new TextLine (absolutePosition);
            int result = lines.AsSpan (0, length).BinarySearch (tl);            
            if (result < 0) {
                result = ~result;
                return result == 0 ?
                    new CharLocation (0, absolutePosition) :
                    new CharLocation (result - 1, absolutePosition - lines[result - 1].Start);
            }
            return new CharLocation (result, absolutePosition - lines[result].Start);
        }
        public void UpdateLineLengthInPixel (int index, int lengthInPixel) {
            lines[index].LengthInPixel = lengthInPixel;
        }
        public int Count => length;
        public bool IsReadOnly => false;
        public bool IsEmpty => length == 0;

        public TextLine this[int index] { get => lines[index]; set => lines[index] = value; }

        public void Add (TextLine item) {
            if (lines.Length < length + 1) {
                TextLine[] tmp = new TextLine[length * 2];
                lines.AsSpan ().CopyTo (tmp);
                lines = tmp;
            }
            lines[length] = item;
            length++;
        }

        public void Clear () {
            length = 0;
        }

        public bool Contains (TextLine item) => Array.IndexOf<TextLine> (lines, item) >= 0;

        public void CopyTo (TextLine[] array, int arrayIndex) {
            lines.AsSpan (0, length).CopyTo (array.AsSpan (arrayIndex));
        }

        public bool Remove (TextLine item) {
            int idx = Array.IndexOf<TextLine> (lines, item);
            if (idx < 0)
                return false;
            if (idx + 1 < length)
                lines.AsSpan (idx + 1, length - idx - 1).CopyTo (lines.AsSpan (idx));
            length--;
            return true;
        }
        public IEnumerator<TextLine> GetEnumerator () => new Enumerator (this);
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

        public int IndexOf (TextLine item) => Array.IndexOf (lines, item);

        public void Insert (int index, TextLine item) {
            if (lines.Length < length + 1) {
                TextLine[] tmp = new TextLine[length * 2];
                lines.AsSpan (0, index).CopyTo (tmp);
                lines.AsSpan (index).CopyTo (tmp.AsSpan (index + 1));
                lines = tmp;
            }else
                lines.AsSpan (index, length - index).CopyTo (lines.AsSpan (index + 1));            
            lines[index] = item;
            length++;
        }

        public void RemoveAt (int index) {
            if (index + 1 < length)
                lines.AsSpan (index + 1, length - index - 1).CopyTo (lines.AsSpan (index));
            length--;
        }
        public void RemoveAt (int index, int count) {
            if (index + count < length)
                lines.AsSpan (index + count, length - index - count).CopyTo (lines.AsSpan (index));
            length -= count;
        }

        public class Enumerator : IEnumerator<TextLine>
        {
            TextLine[] lines;
            int length, position = -1;
            public Enumerator (LineCollection coll) {
                lines = coll.lines;
                length = coll.length;
            }
            public TextLine Current => lines[position];
            object IEnumerator.Current => Current;
            public void Dispose () { }
            public bool MoveNext () => ++position < length;            
            public void Reset () {
                position = -1;
            }
        }
    }
}
