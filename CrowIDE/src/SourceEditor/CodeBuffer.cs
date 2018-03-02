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

namespace Crow.Coding
{
	/// <summary>
	/// Code buffer, lines are arranged in a List<string>, new line chars are removed during string.split on '\n...',
	/// </summary>
	public class CodeBuffer
	{
		public ReaderWriterLockSlim editMutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		//those events are handled in SourceEditor to help keeping sync between textbuffer,parser and editor.
		//modified lines are marked for reparse
		#region Events
		public event EventHandler<CodeBufferEventArgs> LineUpadateEvent;
		public event EventHandler<CodeBufferEventArgs> LineRemoveEvent;
		public event EventHandler<CodeBufferEventArgs> LineAdditionEvent;
		public event EventHandler<CodeBufferEventArgs> FoldingEvent;
		public event EventHandler BufferCleared;
		public event EventHandler SelectionChanged;
		public event EventHandler PositionChanged;
		#endregion

		string lineBreak = Interface.LineBreak;
		List<CodeLine> lines = new List<CodeLine>();
		public int longestLineIdx = 0;
		public int longestLineCharCount = 0;
		/// <summary>
		/// real position in char arrays, tab = 1 char
		/// </summary>
		int _currentLine = 0;
		int _currentCol = 0;

		public int LineCount { get { return lines.Count;}}
		public int IndexOf (CodeLine cl) {
			return lines.IndexOf (cl);
		}

		public CodeLine this[int i]
		{
			get { return lines[i]; }
			set {
				if (lines [i] == value)
					return;
				editMutex.EnterWriteLock ();
				lines [i] = value;
				LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
				editMutex.ExitWriteLock ();
			}
		}

		public void RemoveAt(int i){
			editMutex.EnterWriteLock ();
			lines.RemoveAt (i);
			LineRemoveEvent.Raise (this, new CodeBufferEventArgs (i));
			editMutex.ExitWriteLock ();
		}
		public void Insert(int i, string item){
			editMutex.EnterWriteLock ();
			lines.Insert (i, item);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (i));
			editMutex.ExitWriteLock ();
		}
		public void Add(CodeLine item){
			editMutex.EnterWriteLock ();
			lines.Add (item);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (lines.Count - 1));
			editMutex.ExitWriteLock ();
		}
		public void AddRange (string[] items){
			int start = lines.Count;
			editMutex.EnterWriteLock ();
			for (int i = 0; i < items.Length; i++)
				lines.Add (items [i]);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (start, items.Length));
			editMutex.ExitWriteLock ();
		}
		public void AddRange (CodeLine[] items){
			int start = lines.Count;
			editMutex.EnterWriteLock ();
			lines.AddRange (items);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (start, items.Length));
			editMutex.ExitWriteLock ();
		}
		public void Clear () {
			editMutex.EnterWriteLock ();
			longestLineCharCount = 0;
			lines.Clear ();
			BufferCleared.Raise (this, null);
			editMutex.ExitWriteLock ();
		}
		public void UpdateLine(int i, string newContent){
			editMutex.EnterWriteLock ();
			this [i].Content = newContent;
			LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
			editMutex.ExitWriteLock ();
		}
		public void AppenedLine(int i, string newContent){
			editMutex.EnterWriteLock ();
			this [i].Content += newContent;
			LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
			editMutex.ExitWriteLock ();
		}
		public void ToogleFolding (int line) {
			if (!this [line].IsFoldable)
				return;
			editMutex.EnterWriteLock ();
			this [line].IsFolded = !this [line].IsFolded;
			FoldingEvent.Raise (this, new CodeBufferEventArgs (line));
			editMutex.ExitWriteLock ();
		}
		public void Load(string rawSource, string lineBrkRegex = @"\r\n|\r|\n|\\\n") {
			this.Clear();

			if (string.IsNullOrEmpty (rawSource))
				return;

			AddRange (Regex.Split (rawSource, lineBrkRegex));

			lineBreak = detectLineBreakKind (rawSource);
		}

		/// <summary>
		/// Finds the longest visual line as printed on screen with tabulation replaced with n spaces
		/// </summary>
		public void FindLongestVisualLine(){
			longestLineCharCount = 0;
			for (int i = 0; i < this.LineCount; i++) {
				if (lines[i].PrintableLength > longestLineCharCount) {
					longestLineCharCount = lines[i].PrintableLength;
					longestLineIdx = i;
				}
			}
			Debug.WriteLine ("Longest line: {0}->{1}", longestLineIdx, longestLineCharCount);
		}
		/// <summary> line break could be '\r' or '\n' or '\r\n' </summary>
		static string detectLineBreakKind(string buffer){
			string strLB = "";

			if (string.IsNullOrEmpty(buffer))
				return Interface.LineBreak;
			int i = 0;
			while ( i < buffer.Length) {
				if (buffer [i] == '\r') {
					strLB += '\r';
					i++;
				}
				if (i < buffer.Length) {
					if (buffer [i] == '\r')
						return "\r";
					if (buffer[i] == '\n')
						strLB += '\n';
				}
				if (!string.IsNullOrEmpty (strLB))
					return strLB;
				i++;
			}
			return Interface.LineBreak;
		}
		/// <summary>
		/// return all lines with linebreaks
		/// </summary>
		public string FullText{
			get {
				if (lines.Count == 0)
					return "";
				string tmp = "";
				for (int i = 0; i < lines.Count -1; i++)
					tmp += lines [i].Content + this.lineBreak;
				tmp += lines [lines.Count - 1].Content;
				return tmp;
			}
		}

		/// <summary>
		/// unfolded and not in folds line count
		/// </summary>
		public int UnfoldedLines {
			get {
				int i = 0, vl = 0;
				while (i < LineCount) {
					if (this [i].IsFolded)
						i = GetEndNodeIndex (i);
					i++;
					vl++;
				}
				//Debug.WriteLine ("unfolded lines: " + vl);
				return vl;
			}
		}

		/// <summary>
		/// convert visual position to buffer position
		/// </summary>
		Point getBuffPos (Point visualPos) {
			int i = 0;
			int buffCol = 0;
			while (i < visualPos.X) {
				if (this [visualPos.Y] [buffCol] == '\t')
					i += Interface.TabSize;
				else
					i++;
				buffCol++;
			}
			return new Point (buffCol, visualPos.Y);
		}

		public int GetEndNodeIndex (int line) {
			return IndexOf (this [line].SyntacticNode.EndLine);
		}

		int ConverteTabulatedPosOfCurLine (int column) {
			int tmp = 0;
			int i = 0;
			while (i < lines [_currentLine].Content.Length){
				if (lines [_currentLine].Content [i] == '\t')
					tmp += 4;
				else
					tmp++;
				if (tmp > column)
					break;
				i++;
			}
			return i;
		}

		int CurrentTabulatedColumn {
			get {
				return lines [_currentLine].Content.Substring (0, _currentCol).
					Replace ("\t", new String (' ', Interface.TabSize)).Length;
			}
		}
		/// <summary>
		/// Gets visual position computed from actual buffer position
		/// </summary>
//		public Point TabulatedPosition {
//			get { return new Point (TabulatedColumn, _currentLine); }
//		}
		/// <summary>
		/// set buffer current position from visual position
		/// </summary>
//		public void SetBufferPos(Point tabulatedPosition) {
//			CurrentPosition = getBuffPos(tabulatedPosition);
//		}

		#region Editing and moving cursor
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

		public string SelectedText {
			get {
				if (SelectionIsEmpty)
					return "";
				Point selStart = SelectionStart;
				Point selEnd = SelectionEnd;
				if (selStart.Y == selEnd.Y)
					return this [selStart.Y].Content.Substring (selStart.X, selEnd.X - selStart.X);
				string tmp = "";
				tmp = this [selStart.Y].Content.Substring (selStart.X);
				for (int l = selStart.Y + 1; l < selEnd.Y; l++) {
					tmp += Interface.LineBreak + this [l].Content;
				}
				tmp += Interface.LineBreak + this [selEnd.Y].Content.Substring (0, selEnd.X);
				return tmp;
			}
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
		int requestedColumn = -1;
		/// <summary>
		/// Current column in buffer coordinate, tabulation = 1 char
		/// </summary>
		public int CurrentColumn{
			get { return _currentCol; }
			set {
				if (value == _currentCol)
					return;
				if (value < 0)
					_currentCol = 0;
				else if (value > lines [_currentLine].Length)
					_currentCol = lines [_currentLine].Length;
				else
					_currentCol = value;

				requestedColumn = CurrentTabulatedColumn;
				//requestedColumn = _currentCol;

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
				if (value >= lines.Count)
					_currentLine = lines.Count-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;
//				if (_currentCol < 0)
//					requestedColumn = tabu _currentCol;
				int tabulatedRequestedCol = ConverteTabulatedPosOfCurLine(requestedColumn);
				if (requestedColumn > lines [_currentLine].PrintableLength)
					_currentCol = lines [_currentLine].Length;
				else
					//_currentCol = requestedColumn;
					_currentCol = tabulatedRequestedCol;
				//Debug.WriteLine ("buff cur line: " + _currentLine);
				PositionChanged.Raise (this, null);
			}
		}
		public CodeLine CurrentCodeLine {
			get { return this [_currentLine]; }
		}
		/// <summary>
		/// Current position in buffer coordinate, tabulation = 1 char
		/// </summary>
		public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
//			set {
//				_currentCol = value.X;
//				_currentLine = value.Y;
//			}
		}
		/// <summary>
		/// get char at current position in buffer
		/// </summary>
		protected Char CurrentChar { get { return lines [CurrentLine] [CurrentColumn]; } }

		public void GotoWordStart(){
			if (this[CurrentLine].Length == 0)
				return;
			CurrentColumn--;
			//skip white spaces
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			if (!char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void GotoWordEnd(){
			//skip white spaces
			if (CurrentColumn >= this [CurrentLine].Length - 1)
				return;
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < this [CurrentLine].Length-1)
				CurrentColumn++;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < this [CurrentLine].Length-1)
				CurrentColumn++;
			if (char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void DeleteChar()
		{
			editMutex.EnterWriteLock ();
			if (SelectionIsEmpty) {
				if (CurrentColumn == 0) {
					if (CurrentLine == 0) {
						editMutex.ExitWriteLock ();
						return;
					}
					CurrentLine--;
					CurrentColumn = this [CurrentLine].Length;
					AppenedLine (CurrentLine, this [CurrentLine + 1].Content);
					RemoveAt (CurrentLine + 1);
					editMutex.ExitWriteLock ();
					return;
				}
				CurrentColumn--;
				UpdateLine (CurrentLine, this [CurrentLine].Content.Remove (CurrentColumn, 1));
			} else {
				int linesToRemove = SelectionEnd.Y - SelectionStart.Y + 1;
				int l = SelectionStart.Y;

				if (linesToRemove > 0) {
					UpdateLine (l, this [l].Content.Remove (SelectionStart.X, this [l].Length - SelectionStart.X) +
					this [SelectionEnd.Y].Content.Substring (SelectionEnd.X, this [SelectionEnd.Y].Length - SelectionEnd.X));
					l++;
					for (int c = 0; c < linesToRemove - 1; c++)
						RemoveAt (l);
					CurrentLine = SelectionStart.Y;
					CurrentColumn = SelectionStart.X;
				} else
					UpdateLine (l, this [l].Content.Remove (SelectionStart.X, SelectionEnd.X - SelectionStart.X));
				CurrentColumn = SelectionStart.X;
				ResetSelection ();
			}
			editMutex.ExitWriteLock ();
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		public void Insert(string str)
		{
			if (!SelectionIsEmpty)
				this.DeleteChar ();
			string[] strLines = Regex.Split (str, "\r\n|\r|\n|" + @"\\n").ToArray();
			UpdateLine (CurrentLine, this [CurrentLine].Content.Insert (CurrentColumn, strLines[0]));
			CurrentColumn += strLines[0].Length;
			for (int i = 1; i < strLines.Length; i++) {
				InsertLineBreak ();
				UpdateLine (CurrentLine, this [CurrentLine].Content.Insert (CurrentColumn, strLines[i]));
				CurrentColumn += strLines[i].Length;
			}
		}
		/// <summary>
		/// Insert a line break.
		/// </summary>
		public void InsertLineBreak()
		{
			if (CurrentColumn > 0) {
				Insert (CurrentLine + 1, this [CurrentLine].Content.Substring (CurrentColumn));
				UpdateLine (CurrentLine, this [CurrentLine].Content.Substring (0, CurrentColumn));
			} else
				Insert(CurrentLine, "");

			CurrentColumn = 0;
			CurrentLine++;
		}
		#endregion
	}
}

