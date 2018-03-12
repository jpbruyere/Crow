//
//  CodeTextBuffer.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Text;

namespace Crow.Coding
{
	/// <summary>
	/// Code buffer, lines are arranged in a List<string>, new line chars are removed during string.split on '\n...',
	/// </summary>
	public class TextBuffer
	{
		public ReaderWriterLockSlim editMutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		static Regex slb = new Regex ("\\n");//single char line break used internaly
		Regex reghexLineBrk = new Regex(@"\r\n|\r|\n|\\\n");//original text line break regex

		#region Events
		public event EventHandler<CodeBufferEventArgs> LineUpadateEvent;
		public event EventHandler<CodeBufferEventArgs> LineRemoveEvent;
		public event EventHandler<CodeBufferEventArgs> LineAdditionEvent;
		public event EventHandler BufferCleared;
		public event EventHandler SelectionChanged;
		public event EventHandler PositionChanged;
		#endregion

		StringBuilder buffer = new StringBuilder();
		string lineBreak = Interface.LineBreak;//detected linebreak kind in original source
		List<int> lineLength = new List<int>();//line length table
		/// <summary>
		/// real position in char arrays, tab = 1 char
		/// </summary>
		int _currentLine = 0;
		int _currentCol = 0;

		/// <summary>
		/// Gets the total line count.
		/// </summary>
		public int LineCount { get { return lineLength.Count;}}

		/// <summary>
		/// get a substring in the buffer
		/// </summary>
		/// <returns>a new string</returns>
		/// <param name="idx">absolute index in the buffer</param>
		/// <param name="length">length of the substring</param>
		public string GetSubString (int idx, int length){
			return buffer.ToString (idx, length);
		}
		/// <summary>
		/// Gets length of a line.
		/// </summary>
		/// <returns>length of line</returns>
		/// <param name="lineIdx">line nuber</param>
		public int GetLineLength (int lineIdx) {
			return lineLength [lineIdx];
		}
		/// <summary>
		/// get a single charactere in the buffer
		/// </summary>
		/// <returns>a single char</returns>
		/// <param name="idx">absolute index in the buffer</param>
		public char GetCharAt (int idx){
			return buffer [idx];
		}
		/// <summary>
		/// return full text with original line breaks
		/// </summary>
		/// <value>a string containing the full text</value>
		public string FullText {
			get { return buffer.Replace("\n", lineBreak).ToString (); }
			set {
				Load (value);
			}
		}
		/// <summary>
		/// Gets the buffer pointer of a line
		/// </summary>
		/// <returns>absolute index of the line in the buffer</returns>
		/// <param name="i">line number</param>
		public int GetBufferIndexOfLine (int i) {
			int ptr = 0;
			editMutex.EnterReadLock ();
			for (int j = 0; j < i; j++) {
				ptr += lineLength [j];
			}
			editMutex.ExitReadLock ();
			return ptr;
		}
		/// <summary>
		/// Gets the buffer pointer for the current position 
		/// </summary>
		/// <value>absolute index in the buffer of current position</value>
		public int BufferIndexOfCurrentPosition {
			get {return GetBufferIndexOfLine (_currentLine) + _currentCol;}
		}
		public int this[int i]
		{
			get {				
				int ptr = 0;
				editMutex.EnterReadLock ();
				for (int j = 0; j < i; j++) {
					ptr += lineLength [j];
				}
				editMutex.ExitReadLock ();
				return ptr; 
			}
		}
		/// <summary>
		/// remove line number i
		/// </summary>
		/// <param name="i">index of the line</param>
		public void RemoveLine(int i){
			editMutex.EnterWriteLock ();
			buffer.Remove (GetBufferIndexOfLine (i), lineLength [i]);
			lineLength.RemoveAt (i);
			editMutex.ExitWriteLock ();
			LineRemoveEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		/// <summary>
		/// insert string without linebreaks at position i in buff
		/// </summary>
		/// <param name="i">absolute index in the buffer</param>
		/// <param name="str">linebreak free string</param>
		public void InsertAt(int i, string str){
			editMutex.EnterWriteLock ();
			buffer.Insert (this [i], str);
			lineLength.Insert (i, str.Length);
			editMutex.ExitWriteLock ();
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		public void AddLine(string str){
			editMutex.EnterWriteLock ();
			if (lineLength.LastOrDefault() == 0) {
				buffer.Append (str);
				lineLength [lineLength.Count - 1] = str.Length;
				lineLength.Add (0);
			}
			editMutex.ExitWriteLock ();
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (lineLength.Count - 1));
		}
		public void AddRange (string[] items){
			int start = lineLength.Count;
			editMutex.EnterWriteLock ();
			for (int i = 0; i < items.Length; i++)
				AddLine (items [i]);
			editMutex.ExitWriteLock ();
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (start, items.Length));
		}
		public void Clear () {
			editMutex.EnterWriteLock ();
			lineLength.Clear ();
			buffer.Clear ();
			editMutex.ExitWriteLock ();
			BufferCleared.Raise (this, null);
		}
		public void UpdateLine(int i, string newContent){
			editMutex.EnterWriteLock ();
			int ptrL = this [i];
			buffer.Remove (ptrL, lineLength [i]);
			buffer.Insert (ptrL, newContent);
			lineLength [i] = newContent.Length;
			editMutex.ExitWriteLock ();
			LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		public void AppenedLine(int i, string newContent){
			editMutex.EnterWriteLock ();
			int ptr = this [i] + lineLength [i];
			if (i < LineCount - 1)
				ptr--;
			buffer.Insert(ptr, newContent);
			lineLength [i] += newContent.Length;
			editMutex.ExitWriteLock ();
			LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		public void InsertAt(string str)
		{
			if (!SelectionIsEmpty)
				this.Delete ();

			editMutex.EnterWriteLock ();

			string tmp = reghexLineBrk.Replace (str, "\n");//use single char line break in buffer
			int buffPtr = this [CurrentLine] + CurrentColumn;
			buffer.Insert (buffPtr, tmp);

			int lPtr = CurrentLine, strPtr = 0;
			int remainingLength = lineLength [lPtr] - CurrentColumn;
			lineLength [lPtr] = CurrentColumn;
			foreach (Match match in slb.Matches(tmp))
			{
				lineLength [lPtr] += match.Index + 1 - strPtr;
				lPtr++;
				lineLength.Insert (lPtr, 0);
				strPtr = match.Index + 1;
				//CurrentLine++;
			}
			remainingLength += tmp.Length - strPtr; 
			lineLength [lPtr] += remainingLength;

			CurrentLine = lPtr;
			if (strPtr == 0)
				CurrentColumn += tmp.Length;
			else
				CurrentColumn = tmp.Length - strPtr;

			editMutex.ExitWriteLock ();
			if (strPtr>0)
				LineAdditionEvent.Raise (this, null);
			else
				LineUpadateEvent.Raise (this, null);
		}
		/// <summary>
		/// Insert a line break.
		/// </summary>
		public void InsertLineBreak()
		{
			editMutex.EnterWriteLock ();
			buffer.Insert (this [CurrentLine] + CurrentColumn, '\n');
			int lgdiff = lineLength [CurrentLine] - CurrentColumn;
			lineLength.Insert (CurrentLine + 1, lineLength [CurrentLine] - CurrentColumn);
			lineLength [CurrentLine] = CurrentColumn + 1;
			editMutex.ExitWriteLock ();
			LineAdditionEvent.Raise (this, null);
			CurrentColumn = 0;
			CurrentLine++;
		}
		public void Delete()
		{
			editMutex.EnterWriteLock ();
			if (SelectionIsEmpty) {
				if (CurrentColumn == 0) {
					if (CurrentLine == 0) {
						editMutex.ExitWriteLock ();
						return;
					}

					buffer.Remove (this [CurrentLine] - 1, 1);

					int col = lineLength [CurrentLine - 1] - 1;
					lineLength [CurrentLine - 1] += lineLength [CurrentLine] - 1;
					lineLength.RemoveAt (CurrentLine);

					CurrentLine--;
					CurrentColumn = col;

					editMutex.ExitWriteLock ();
					LineRemoveEvent.Raise (this, null);
					return;
				}
				CurrentColumn--;
				buffer.Remove (this [CurrentLine] + CurrentColumn, 1);
				lineLength [CurrentLine]--;
			} else {
				int linesToRemove = SelectionEnd.Y - SelectionStart.Y;
				int ptr = this [SelectionStart.Y] + SelectionStart.X;
				int length = lineLength [SelectionStart.Y] - SelectionStart.X;
				int l = 1;
				while (l <= linesToRemove ) {
					length += lineLength [SelectionStart.Y + l];
					l++;
				}
				length -= lineLength [SelectionEnd.Y] - SelectionEnd.X;
				buffer.Remove (ptr, length);
				lineLength [SelectionStart.Y] = SelectionStart.X + lineLength [SelectionEnd.Y] - SelectionEnd.X;
				lineLength.RemoveRange (SelectionStart.Y + 1, linesToRemove);

				CurrentLine = SelectionStart.Y;
				CurrentColumn = SelectionStart.X;
				ResetSelection ();

				if (linesToRemove > 0)
					LineRemoveEvent.Raise (this, null);
				else
					LineUpadateEvent.Raise (this, null);
			}
			editMutex.ExitWriteLock ();
		}
		public void Load(string rawSource) {
			this.Clear();

			if (string.IsNullOrEmpty (rawSource))
				return;

			lineBreak = reghexLineBrk.Match (rawSource).Value;//store original line break
			string tmp = reghexLineBrk.Replace (rawSource, "\n");//use single char line break in buffer
			int ptr = 0;
			foreach (Match match in slb.Matches(tmp))
			{
				int l = match.Index + 1;
				int lg = l - ptr;
				lineLength.Add (lg);
				ptr = l;
			}
			lineLength.Add (0);

			buffer = new StringBuilder (tmp);
		}

//		public int CurrentTabulatedColumn {
//			get {
////				return lines [_currentLine].Content.Substring (0, _currentCol).
////					Replace ("\t", new String (' ', Interface.TabSize)).Length;
//			}
//		}

		#region moving cursor an selection
		Point selStartPos = -1;	//selection start (row,column)
		Point selEndPos = -1;	//selection end (row,column)

		public bool SelectionInProgress { get { return selStartPos >= 0; }}
		public void SetSelStartPos () {
			selStartPos = selEndPos = CurrentPosition;
			SelectionChanged.Raise (this, null);
		}
		public void SetSelEndPos () {
			selEndPos = CurrentPosition;
			SelectionChanged.Raise (this, null);
		}
		/// <summary>
		/// Set selection in buffer to -1, empty selection
		/// </summary>
		public void ResetSelection () {
			selStartPos = selEndPos = -1;
			SelectionChanged.Raise (this, null);
		}
		/// <summary>
		/// ordered selection start and end positions in char units
		/// </summary>
		public Point SelectionStart	{
			get { return selEndPos < 0 || selStartPos.Y < selEndPos.Y ? selStartPos :
					selStartPos.Y > selEndPos.Y ? selEndPos :
					selStartPos.X < selEndPos.X ? selStartPos : selEndPos; }
		}
		public Point SelectionEnd {
			get { return selEndPos < 0 || selStartPos.Y > selEndPos.Y ? selStartPos :
					selStartPos.Y < selEndPos.Y ? selEndPos :
					selStartPos.X > selEndPos.X ? selStartPos : selEndPos; }
		}
		public bool SelectionIsEmpty
		{ get { return selEndPos == selStartPos; } }
		/// <summary>
		/// Current column in buffer coordinate, tabulation = 1 char
		/// </summary>
		public int CurrentColumn{
			get { return _currentCol; }
			set {
				if (value == _currentCol)
					return;

				editMutex.EnterWriteLock ();

				if (value < 0)
					_currentCol = 0;
				else if (value >= lineLength [_currentLine]) {
					if (_currentLine < LineCount -1 && value > lineLength [_currentLine] - 1)
						_currentCol = lineLength [_currentLine] - 1;
					else
						_currentCol = lineLength [_currentLine];
				}else
					_currentCol = value;

				editMutex.ExitWriteLock ();

				PositionChanged.Raise (this, null);
			}
		}
		/// <summary>
		/// Current row in buffer coordinate, tabulation = 1 char
		/// </summary>
		public int CurrentLine{
			get { return _currentLine; }
			set {
				if (value == _currentLine)
					return;

				editMutex.EnterWriteLock ();

				if (value >= lineLength.Count)
					_currentLine = lineLength.Count-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;

				int c = _currentCol;
				_currentCol = 0;
				CurrentColumn = c;

				editMutex.ExitWriteLock();

				PositionChanged.Raise (this, null);
			}
		}
		/// <summary>
		/// Current position in buffer coordinate, tabulation = 1 char
		/// </summary>
		public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
		}
		/// <summary>
		/// get char at current position in buffer
		/// </summary>
		protected Char CurrentChar { get { return buffer[this [CurrentLine]]; } }
		public string SelectedText {
			get {
				if (SelectionIsEmpty)
					return "";
				Point selStart = SelectionStart;
				Point selEnd = SelectionEnd;

				int ptr = this [selStart.Y] + selStart.X;
				int length = lineLength[selStart.Y] - selStart.X;
				for (int i = selStart.Y+1; i <= selEnd.Y; i++) 
					length += lineLength [i];
				length -= lineLength[selEnd.Y] - selEnd.X;

				return buffer.ToString (ptr, length);
			}
		}

		public void GotoWordStart(){
			if (_currentCol == 0)
				MoveLeft ();
			if (_currentCol == 0)
				return;
			int ptrStart = BufferIndexOfCurrentPosition;
			int ptr = ptrStart;
			char c;
			//skip white spaces
			do {
				ptr--;
				c = this.GetCharAt (ptr);
			} while (!char.IsLetterOrDigit (c) && c != '\n' && ptr > 1);
				
			do {
				ptr--;
				c = this.GetCharAt (ptr);
			} while (char.IsLetterOrDigit (c) && c != '\n' && ptr > 1);

			if (ptr == 0)
				CurrentColumn = 0;
			else
				CurrentColumn -= ptrStart - ptr - 1;		
		}
		public void GotoWordEnd(){
			int limx = GetLineLength (_currentLine);
			if (_currentLine < lineLength.Count - 1)
				limx--;
			
			if (_currentCol == limx) {
				MoveRight ();
				limx = GetLineLength (_currentLine);
				if (_currentLine < lineLength.Count - 1)
					limx--;				
			}

			int ptrLine = GetBufferIndexOfLine(_currentLine);
			int ptrCol = _currentCol;

			char c;
			//skip white spaces
			do {				
				c = GetCharAt (ptrLine+ptrCol);
				ptrCol++;
			} while (!char.IsLetterOrDigit (c) && ptrCol < limx);

			do {
				c = GetCharAt (ptrLine + ptrCol);
				ptrCol++;
			} while (char.IsLetterOrDigit (c) && ptrCol < limx);
			CurrentColumn = ptrCol - 1;
		}
		/// <summary>
		/// Moves cursor one char to the left, move up if cursor reaches start of line
		/// </summary>
		public void MoveLeft(){
			if (CurrentColumn == 0) {
				CurrentLine--;
				CurrentColumn = int.MaxValue;
			} else
				CurrentColumn--;			
		}
		/// <summary>
		/// Moves cursor one char to the right, move down if cursor reaches end of line
		/// </summary>
		public void MoveRight(){
			if (_currentLine < LineCount -1){
				if (CurrentColumn >= lineLength [CurrentLine] - 1) {
					CurrentColumn = 0;
					CurrentLine++;
					return;
				}
			}
			CurrentColumn++;
		}

		#endregion
	}
}

