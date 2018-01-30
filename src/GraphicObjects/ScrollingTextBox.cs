//
// ScrollingTextBox.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Cairo;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Crow
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class ScrollingTextBox : ScrollingObject
	{
		#region CTOR
		public ScrollingTextBox (Interface iface):base(iface){
			KeyEventsOverrides = true;
		}
		public ScrollingTextBox ():base()
		{
			KeyEventsOverrides = true;
		}
		#endregion

		public event EventHandler TextChanged;

		public virtual void OnTextChanged(Object sender, EventArgs e)
		{
			TextChanged.Raise (this, e);
		}

		#region private and protected fields
		string lineBreak = Interface.LineBreak;
		int visibleLines = 1;
		List<string> lines;
		string _text = "label";
		Color selBackground;
		Color selForeground;
		Point mouseLocalPos = 0;//mouse coord in widget space
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
		Point _selBegin = -1;	//selection start (row,column)
		Point _selRelease = -1;	//selection end (row,column)

		protected Rectangle rText;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion

		[XmlAttributeAttribute][DefaultValue("label")]
		public string Text
		{
			get {
				return lines == null ?
					_text : lines.Aggregate((i, j) => i + Interface.LineBreak + j);
			}
			set
			{
				if (string.Equals (value, _text, StringComparison.Ordinal))
					return;

				_text = value;

				if (string.IsNullOrEmpty(_text))
					_text = "";

				lines = getLines;
				MaxScrollY = Math.Max (0, lines.Count - visibleLines);

				OnTextChanged (this, null);
				RegisterForGraphicUpdate ();
			}
		}


		[XmlAttributeAttribute][DefaultValue("BlueGray")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (value == selBackground)
					return;
				selBackground = value;
				NotifyValueChanged ("SelectionBackground", selBackground);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute][DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (value == selForeground)
					return;
				selForeground = value;
				NotifyValueChanged ("SelectionForeground", selForeground);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
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
				NotifyValueChanged ("CurrentColumn", _currentCol);
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
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
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;
				NotifyValueChanged ("CurrentLine", _currentLine);

				if (CurrentLine < ScrollY)
					ScrollY = CurrentLine;
				else if (CurrentLine >= ScrollY + visibleLines)
					ScrollY = Math.Max (0, CurrentLine - visibleLines + 1);
			}
		}
		[XmlIgnore]public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
		}
		//TODO:using HasFocus for drawing selection cause SelBegin and Release binding not to work
		/// <summary>
		/// Selection begin position in char units (line, column)
		/// </summary>
		[XmlAttributeAttribute][DefaultValue("-1")]
		public Point SelBegin {
			get { return _selBegin; }
			set {
				if (value == _selBegin)
					return;
				_selBegin = value;
				System.Diagnostics.Debug.WriteLine ("SelBegin=" + _selBegin);
				NotifyValueChanged ("SelBegin", _selBegin);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		/// <summary>
		/// Selection release position in char units (line, column)
		/// </summary>
		[XmlAttributeAttribute][DefaultValue("-1")]
		public Point SelRelease {
			get {
				return _selRelease;
			}
			set {
				if (value == _selRelease)
					return;
				_selRelease = value;
				NotifyValueChanged ("SelRelease", _selRelease);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		/// <summary>
		/// return char at CurrentLine, CurrentColumn
		/// </summary>
		[XmlIgnore]protected Char CurrentChar
		{
			get {
				return lines [CurrentLine] [CurrentColumn];
			}
		}
		/// <summary>
		/// ordered selection start and end positions in char units
		/// </summary>
		[XmlIgnore]protected Point selectionStart
		{
			get {
				return SelRelease < 0 || SelBegin.Y < SelRelease.Y ? SelBegin :
					SelBegin.Y > SelRelease.Y ? SelRelease :
					SelBegin.X < SelRelease.X ? SelBegin : SelRelease;
			}
		}
		[XmlIgnore]public Point selectionEnd
		{
			get {
				return SelRelease < 0 || SelBegin.Y > SelRelease.Y ? SelBegin :
					SelBegin.Y < SelRelease.Y ? SelRelease :
					SelBegin.X > SelRelease.X ? SelBegin : SelRelease;
			}
		}
		[XmlIgnore]public string SelectedText
		{
			get {

				if (SelRelease < 0 || SelBegin < 0)
					return "";
				if (selectionStart.Y == selectionEnd.Y)
					return lines [selectionStart.Y].Substring (selectionStart.X, selectionEnd.X - selectionStart.X);
				string tmp = "";
				tmp = lines [selectionStart.Y].Substring (selectionStart.X);
				for (int l = selectionStart.Y + 1; l < selectionEnd.Y; l++) {
					tmp += Interface.LineBreak + lines [l];
				}
				tmp += Interface.LineBreak + lines [selectionEnd.Y].Substring (0, selectionEnd.X);
				return tmp;
			}
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return SelRelease == SelBegin; } }

		List<string> getLines {
			get {
				return Regex.Split (_text, "\r\n|\r|\n|\\\\n").ToList();
			}
		}
		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			int tmp = _currentCol - 1;
			if (tmp < 0) {
				if (_currentLine == 0)
					return false;
				CurrentLine--;
				CurrentColumn = int.MaxValue;
			} else
				CurrentColumn = tmp;
			return true;
		}
		/// <summary>
		/// Moves cursor one char to the right.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			int tmp = _currentCol + 1;
			if (tmp > lines [_currentLine].Length){
				if (CurrentLine == lines.Count - 1)
					return false;
				CurrentLine++;
				CurrentColumn = 0;
			} else
				CurrentColumn = tmp;
			return true;
		}
		public void GotoWordStart(){
			if (lines[CurrentLine].Length == 0)
				return;
			CurrentColumn--;
			//skip white spaces
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			while (char.IsLetterOrDigit (lines [CurrentLine] [CurrentColumn]) && CurrentColumn > 0)
				CurrentColumn--;
			if (!char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void GotoWordEnd(){
			//skip white spaces
			if (CurrentColumn >= lines [CurrentLine].Length - 1)
				return;
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < lines [CurrentLine].Length-1)
				CurrentColumn++;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < lines [CurrentLine].Length-1)
				CurrentColumn++;
			if (char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void DeleteChar()
		{
			if (selectionIsEmpty) {
				if (CurrentColumn == 0) {
					if (CurrentLine == 0 && lines.Count == 1)
						return;
					CurrentLine--;
					CurrentColumn = lines [CurrentLine].Length;
					lines [CurrentLine] += lines [CurrentLine + 1];
					lines.RemoveAt (CurrentLine + 1);
					OnTextChanged (this, null);
					return;
				}
				CurrentColumn--;
				lines [CurrentLine] = lines [CurrentLine].Remove (CurrentColumn, 1);
			} else {
				int linesToRemove = selectionEnd.Y - selectionStart.Y + 1;
				int l = selectionStart.Y;

				if (linesToRemove > 0) {
					lines [l] = lines [l].Remove (selectionStart.X, lines [l].Length - selectionStart.X) +
						lines [selectionEnd.Y].Substring (selectionEnd.X, lines [selectionEnd.Y].Length - selectionEnd.X);
					l++;
					for (int c = 0; c < linesToRemove-1; c++)
						lines.RemoveAt (l);
					CurrentLine = selectionStart.Y;
					CurrentColumn = selectionStart.X;
				} else
					lines [l] = lines [l].Remove (selectionStart.X, selectionEnd.X - selectionStart.X);
				CurrentColumn = selectionStart.X;
				SelBegin = -1;
				SelRelease = -1;
			}
			OnTextChanged (this, null);
		}

		#region GraphicObject overrides
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;

				using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
					using (Context gr = new Context (img)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				MaxScrollY = 0;
				RegisterForGraphicUpdate ();
			}
		}
		protected override int measureRawSize(LayoutingType lt)
		{
			if (lt == LayoutingType.Height)
				return (int)Math.Ceiling(fe.Height * lines.Count) + Margin * 2;

			string txt = _text.Replace("\t", new String (' ', Interface.TabSize));


			int maxChar = 0;
			foreach (string s in Regex.Split (txt, "\r\n|\r|\n|\\\\n")) {
				if (maxChar < s.Length)
					maxChar = s.Length;
			}
			return (int)(fe.MaxXAdvance * maxChar) + Margin * 2;
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				updateVisibleLines ();
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Rectangle cb = ClientRectangle;

			Foreground.SetAsSource (gr);

			bool selectionInProgress = false;

			#region draw text cursor
			if (SelBegin != SelRelease)
				selectionInProgress = true;
			else if (HasFocus){
				gr.SetSourceColor(Color.Red);
				gr.LineWidth = 2.0;
				double cursorX = cb.X + (CurrentColumn - ScrollX) * fe.MaxXAdvance;
				gr.MoveTo (0.5 + cursorX, cb.Y + (CurrentLine - ScrollY) * fe.Height);
				gr.LineTo (0.5 + cursorX, cb.Y + (CurrentLine + 1 - ScrollY) * fe.Height);
				gr.Stroke();
			}
			#endregion

			Foreground.SetAsSource (gr);

			for (int i = 0; i < visibleLines; i++) {
				int curL = i + ScrollY;
				if (curL >= lines.Count)
					break;
				string lstr = lines[curL];

				gr.MoveTo (cb.X, cb.Y + fe.Ascent + fe.Height * i);
				gr.ShowText (lstr);
				gr.Fill ();

				if (selectionInProgress && curL >= selectionStart.Y && curL <= selectionEnd.Y) {

					double rLineX = cb.X,
					rLineY = cb.Y + i * fe.Height,
					rLineW = lstr.Length * fe.MaxXAdvance;

					System.Diagnostics.Debug.WriteLine ("sel start: " + selectionStart + " sel end: " + selectionEnd);
					if (curL == selectionStart.Y) {
						rLineX += (selectionStart.X - ScrollX) * fe.MaxXAdvance;
						rLineW -= selectionStart.X * fe.MaxXAdvance;
					}
					if (curL == selectionEnd.Y)
						rLineW -= (lstr.Length - selectionEnd.X) * fe.MaxXAdvance;

					gr.Save ();
					gr.Operator = Operator.Source;
					gr.Rectangle (rLineX, rLineY, rLineW, fe.Height);
					gr.SetSourceColor (SelectionBackground);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (SelectionForeground);
					gr.MoveTo (cb.X, cb.Y + fe.Ascent + fe.Height * i);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}
			}
		}
		#endregion

		#region Mouse handling
		void updatemouseLocalPos(Point mpos){
			Point mouseLocalPos = mpos - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;
			if (mouseLocalPos.X < 0)
				CurrentColumn--;
			else
				CurrentColumn = ScrollX +  (int)Math.Round (mouseLocalPos.X / fe.MaxXAdvance);

			if (mouseLocalPos.Y < 0)
				CurrentLine--;
			else
				CurrentLine = ScrollY + (int)Math.Floor (mouseLocalPos.Y / fe.Height);
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			CurrentInterface.MouseCursor = XCursor.Text;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			CurrentInterface.MouseCursor = XCursor.Default;
		}
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			//			SelBegin = new Point(0,0);
			//			SelRelease = new Point (lines.LastOrDefault ().Length, lines.Count-1);
			RegisterForRedraw ();
		}
		protected override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);

			//			SelBegin = -1;
			//			SelRelease = -1;
			RegisterForRedraw ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!e.Mouse.IsButtonDown (MouseButton.Left))
				return;
			if (!HasFocus || SelBegin < 0)
				return;

			updatemouseLocalPos (e.Position);
			SelRelease = CurrentPosition;

			RegisterForRedraw();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (this.HasFocus){
				updatemouseLocalPos (e.Position);
				SelBegin = SelRelease = CurrentPosition;
				RegisterForRedraw();//TODO:should put it in properties
			}

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (SelBegin == SelRelease)
				SelBegin = SelRelease = -1;

			updatemouseLocalPos (e.Position);
			RegisterForRedraw ();
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);

			GotoWordStart ();
			SelBegin = CurrentPosition;
			GotoWordEnd ();
			SelRelease = CurrentPosition;
			RegisterForRedraw ();
		}
		#endregion

		#region Keyboard handling
		public override void onKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			Key key = e.Key;

			switch (key)
			{
			case Key.Back:
				if (CurrentPosition == 0)
					return;
				this.DeleteChar();
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (selectionIsEmpty) {
					if (!MoveRight ())
						return;
				}else if (e.Shift)
					CurrentInterface.Clipboard = this.SelectedText;
				this.DeleteChar ();
				break;
			case Key.Enter:
			case Key.KeypadEnter:
				if (!selectionIsEmpty)
					this.DeleteChar ();
				this.InsertLineBreak ();
				break;
			case Key.Escape:
				Text = "";
				CurrentColumn = 0;
				SelRelease = -1;
				break;
			case Key.Home:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = new Point (CurrentColumn, CurrentLine);
					if (e.Control)
						CurrentLine = 0;
					CurrentColumn = 0;
					SelRelease = new Point (CurrentColumn, CurrentLine);
					break;
				}
				SelRelease = -1;
				if (e.Control)
					CurrentLine = 0;
				CurrentColumn = 0;
				break;
			case Key.End:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (e.Control)
						CurrentLine = int.MaxValue;
					CurrentColumn = int.MaxValue;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (e.Control)
					CurrentLine = int.MaxValue;
				CurrentColumn = int.MaxValue;
				break;
			case Key.Insert:
				if (e.Shift)
					this.Insert (CurrentInterface.Clipboard);
				else if (e.Control && !selectionIsEmpty)
					CurrentInterface.Clipboard = this.SelectedText;
				break;
			case Key.Left:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = new Point(CurrentColumn, CurrentLine);
					if (e.Control)
						GotoWordStart ();
					else if (!MoveLeft ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (e.Control)
					GotoWordStart ();
				else
					MoveLeft();
				break;
			case Key.Right:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (e.Control)
						GotoWordEnd ();
					else if (!MoveRight ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (e.Control)
					GotoWordEnd ();
				else
					MoveRight ();
				break;
			case Key.Up:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine--;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				CurrentLine--;
				break;
			case Key.Down:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine++;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				CurrentLine++;
				break;
			case Key.Menu:
				break;
			case Key.NumLock:
				break;
			case Key.PageDown:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine += visibleLines;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;				
				CurrentLine += visibleLines;
				break;
			case Key.PageUp:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine -= visibleLines;
					SelRelease = CurrentPosition;
					break;
				}				
				CurrentLine -= visibleLines;
				break;
			case Key.RWin:
				break;
			case Key.Tab:
				this.Insert ("\t");
				break;
			default:
				break;
			}
			RegisterForGraphicUpdate();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			this.Insert (e.KeyChar.ToString());

			SelRelease = -1;
			SelBegin = -1; //new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();
		}
		#endregion


		/// <summary> Compute x offset in cairo unit from text position </summary>
		double GetXFromTextPointer(Context gr, Point pos)
		{
			try {
				string l = lines [pos.Y].Substring (0, pos.X).
					Replace ("\t", new String (' ', Interface.TabSize));
				return gr.TextExtents (l).XAdvance;
			} catch{
				return -1;
			}
		}

		/// <summary> line break could be '\r' or '\n' or '\r\n' </summary>
		string detectLineBreakKind(){
			string strLB = "";

			if (string.IsNullOrEmpty(_text))
				return Interface.LineBreak;
			int i = 0;
			while ( i < _text.Length) {
				if (_text [i] == '\r') {
					strLB += '\r';
					i++;
				}
				if (i < _text.Length) {
					if (_text [i] == '\r')
						return "\r";
					if (_text [i] == '\n')
						strLB += '\n';
				}
				if (!string.IsNullOrEmpty (strLB))
					return strLB;
				i++;
			}
			return Interface.LineBreak;
		}

		void updateVisibleLines(){
			visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / fe.Height);
			MaxScrollY = Math.Max (0, lines.Count - visibleLines);

			System.Diagnostics.Debug.WriteLine ("update visible lines: " + visibleLines);
			System.Diagnostics.Debug.WriteLine ("update MaxScrollY: " + MaxScrollY);
		}


		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			if (!selectionIsEmpty)
				this.DeleteChar ();
			string[] strLines = Regex.Split (str, "\r\n|\r|\n|" + @"\\n").ToArray();
			lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, strLines[0]);
			CurrentColumn += strLines[0].Length;
			for (int i = 1; i < strLines.Length; i++) {
				InsertLineBreak ();
				lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, strLines[i]);
				CurrentColumn += strLines[i].Length;
			}
			OnTextChanged (this, null);
			RegisterForGraphicUpdate();
		}

		/// <summary>
		/// Insert a line break.
		/// </summary>
		protected void InsertLineBreak()
		{
			lines.Insert(CurrentLine + 1, lines[CurrentLine].Substring(CurrentColumn));
			lines [CurrentLine] = lines [CurrentLine].Substring (0, CurrentColumn);
			CurrentLine++;
			CurrentColumn = 0;
			OnTextChanged (this, null);
		}
	}
}