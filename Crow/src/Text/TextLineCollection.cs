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
        #endregion

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
        public IEnumerator<TextLine> GetEnumerator () => new LineEnumerator (this);
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

        public int IndexOf (TextLine item) {
            throw new NotImplementedException ();
        }

        public void Insert (int index, TextLine item) {
            throw new NotImplementedException ();
        }

        public void RemoveAt (int index) {
            throw new NotImplementedException ();
        }

        public class LineEnumerator : IEnumerator<TextLine>
        {
            TextLine[] lines;
            int length, position = -1;
            public LineEnumerator (LineCollection coll) {
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
