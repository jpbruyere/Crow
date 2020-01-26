// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Cairo;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Crow {
	public class Label : Widget
    {
		#region CTOR
		protected Label () : base(){}

		public Label(Interface iface) : base(iface)
		{

		}
//		public Label(string _text)
//			: base()
//		{
//			Text = _text;
//		}
		#endregion

		public event EventHandler<TextChangeEventArgs> TextChanged;

		public virtual void OnTextChanged(Object sender, TextChangeEventArgs e)
		{
			textMeasureIsUpToDate = false;
			NotifyValueChanged ("Text", Text);
			TextChanged.Raise (this, e);
		}
        //TODO:change protected to private

		#region private and protected fields
		string _text = "label";
        Alignment _textAlignment;
		bool horizontalStretch;
		bool verticalStretch;
		bool _selectable;
		bool _multiline;
		Color selBackground;
		Color selForeground;
		Point mouseLocalPos = -1;//mouse coord in widget space, filled only when clicked
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
		Point _selBegin = -1;	//selection start (row,column)
		Point _selRelease = -1;	//selection end (row,column)
		double textCursorPos;   //cursor position in cairo units in widget client coord.
		double SelStartCursorPos = -1;
		double SelEndCursorPos = -1;
		bool SelectionInProgress = false;

        protected Rectangle rText;
		protected float widthRatio = 1f;
		protected float heightRatio = 1f;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion

		[DefaultValue("SteelBlue")]
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
		[DefaultValue("White")]
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
		[DefaultValue(Alignment.Left)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set {
				if (value == _textAlignment)
					return;
				_textAlignment = value;
				RegisterForRedraw ();
				NotifyValueChanged ("TextAlignment", _textAlignment);
			}
        }
		[DefaultValue(false)]
		public virtual bool HorizontalStretch {
			get { return horizontalStretch; }
			set {
				if (horizontalStretch == value)
					return;
				horizontalStretch = value;
				RegisterForRedraw ();
				NotifyValueChanged ("HorizontalStretch", horizontalStretch);
			}
		}
		[DefaultValue(false)]
		public virtual bool VerticalStretch {
			get { return verticalStretch; }
			set {
				if (verticalStretch == value)
					return;
				verticalStretch = value;
				RegisterForRedraw ();
				NotifyValueChanged ("VerticalStretch", verticalStretch);
			}
		}
		[DefaultValue("label")]
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

				OnTextChanged (this, new TextChangeEventArgs (Text));
				RegisterForGraphicUpdate ();
            }
        }
		[DefaultValue(false)]
		public bool Selectable
		{
			get { return _selectable; }
			set
			{
				if (value == _selectable)
					return;
				_selectable = value;
				NotifyValueChanged ("Selectable", _selectable);
				SelBegin = -1;
				SelRelease = -1;
				RegisterForRedraw ();
			}
		}
		[DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				if (value == _multiline)
					return;
				_multiline = value;
				NotifyValueChanged ("Multiline", _multiline);
				RegisterForGraphicUpdate();
			}
		}
		[DefaultValue(0)]
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
		[DefaultValue(0)]
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
			}
		}
		[XmlIgnore]public Point CurrentPosition {
			get { return new Point(_currentCol, CurrentLine); }
		}
		//TODO:using HasFocus for drawing selection cause SelBegin and Release binding not to work
		/// <summary>
		/// Selection begin position in char units
		/// </summary>
		[DefaultValue("-1")]
		public Point SelBegin {
			get {
				return _selBegin;
			}
			set {
				if (value == _selBegin)
					return;
				_selBegin = value;
				NotifyValueChanged ("SelBegin", _selBegin);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		[DefaultValue("-1")]
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
		{ get { return SelRelease < 0; } }

		List<string> lines;
		List<string> getLines {
			get {
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n|\\\\n").ToList() :
					new List<string>(new string[] { _text });
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

					OnTextChanged (this, new TextChangeEventArgs (Text));
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
			OnTextChanged (this, new TextChangeEventArgs (Text));
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			if (!selectionIsEmpty)
				this.DeleteChar ();
			if (_multiline) {
				string[] strLines = Regex.Split (str, "\r\n|\r|\n|" + @"\\n").ToArray();
				lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, strLines[0]);
				CurrentColumn += strLines[0].Length;
				for (int i = 1; i < strLines.Length; i++) {
					InsertLineBreak ();
					lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, strLines[i]);
					CurrentColumn += strLines[i].Length;
				}
			} else {
				lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, str);
				CurrentColumn += str.Length;
			}
			OnTextChanged (this, new TextChangeEventArgs (Text));
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
			OnTextChanged (this, new TextChangeEventArgs (Text));
		}
		bool textMeasureIsUpToDate = false;
		Size cachedTextSize = default(Size);

		#region GraphicObject overrides
		protected override int measureRawSize(LayoutingType lt)
		{
			if (lines == null)
				lines = getLines;
			if (!textMeasureIsUpToDate) {
				using (Context gr = new Context (IFace.surf)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					gr.FontOptions = Interface.FontRenderingOptions;
					gr.Antialias = Interface.Antialias;

					fe = gr.FontExtents;
					te = new TextExtents ();

					cachedTextSize.Height = (int)Math.Ceiling ((fe.Ascent+fe.Descent) * Math.Max (1, lines.Count)) + Margin * 2;

					try {
						for (int i = 0; i < lines.Count; i++) {
							string l = lines[i].Replace ("\t", new String (' ', Interface.TAB_SIZE));

							TextExtents tmp = gr.TextExtents (l);

							if (tmp.XAdvance > te.XAdvance)
								te = tmp;
						}
						cachedTextSize.Width = (int)Math.Ceiling (te.XAdvance) + Margin * 2;
						textMeasureIsUpToDate = true;
					} catch {							
						return -1;
					}					
				}
			}
			return lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width;
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			rText = new Rectangle(new Size(
				measureRawSize(LayoutingType.Width), measureRawSize(LayoutingType.Height)));
			rText.Width -= 2 * Margin;
			rText.Height -= 2 * Margin;

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = ClientRectangle;

			rText.X = cb.X;
			rText.Y = cb.Y;

			if (horizontalStretch) {
				widthRatio = (float)cb.Width / (float)rText.Width;
				if (!verticalStretch)
					heightRatio = widthRatio;
			}

			if (verticalStretch) {
				heightRatio = (float)cb.Height / (float)rText.Height;
				if (!horizontalStretch)
					widthRatio = heightRatio;
			}

			rText.Width = (int)(widthRatio * (float)rText.Width);
			rText.Height = (int)(heightRatio * (float)rText.Height);

			switch (TextAlignment)
			{
			case Alignment.TopLeft:     //ok
				rText.X = cb.X;
				rText.Y = cb.Y;
				break;
			case Alignment.Top:   //ok
				rText.Y = cb.Y;
				rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
				break;
			case Alignment.TopRight:    //ok
				rText.Y = cb.Y;
				rText.X = cb.Right - rText.Width;
				break;
			case Alignment.Left://ok
				rText.X = cb.X;
				rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
				break;
			case Alignment.Right://ok
				rText.X = cb.X + cb.Width - rText.Width;
				rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
				break;
			case Alignment.Bottom://ok
				rText.X = cb.Width / 2 - rText.Width / 2;
				rText.Y = cb.Height - rText.Height;
				break;
			case Alignment.BottomLeft://ok
				rText.X = cb.X;
				rText.Y = cb.Bottom - rText.Height;
				break;
			case Alignment.BottomRight://ok
				rText.Y = cb.Bottom - rText.Height;
				rText.X = cb.Right - rText.Width;
				break;
			case Alignment.Center://ok
				rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
				//rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
				rText.Y = cb.Y + (int)Math.Floor((double)cb.Height / 2.0 - (double)rText.Height / 2.0);
				break;
			}

			//gr.FontMatrix = new Matrix(widthRatio * (float)Font.Size, 0, 0, heightRatio * (float)Font.Size, 0, 0);
			fe = gr.FontExtents;

			#region draw text cursor
			if (HasFocus && Selectable)
			{
				if (mouseLocalPos >= 0)
				{
					computeTextCursor(gr);

					if (SelectionInProgress)
					{
						if (SelBegin < 0){
							SelBegin = new Point(CurrentColumn, CurrentLine);
							SelStartCursorPos = textCursorPos;
							SelRelease = -1;
						}else{
							SelRelease = new Point(CurrentColumn, CurrentLine);
							if (SelRelease == SelBegin)
								SelRelease = -1;
							else
								SelEndCursorPos = textCursorPos;
						}
					}else
						computeTextCursorPosition(gr);
				}else
					computeTextCursorPosition(gr);

				Foreground.SetAsSource (gr);
				gr.LineWidth = 1.0;
				gr.MoveTo (0.5 + textCursorPos + rText.X, rText.Y + CurrentLine * (fe.Ascent+fe.Descent));
				gr.LineTo (0.5 + textCursorPos + rText.X, rText.Y + (CurrentLine + 1) * (fe.Ascent+fe.Descent));
				gr.Stroke();
			}
			#endregion

			//****** debug selection *************
//			if (SelRelease >= 0) {
//				new SolidColor(Color.DarkGreen).SetAsSource(gr);
//				Rectangle R = new Rectangle (
//					             rText.X + (int)SelEndCursorPos - 3,
//					             rText.Y + (int)(SelRelease.Y * (fe.Ascent+fe.Descent)),
//					             6,
//					             (int)(fe.Ascent+fe.Descent));
//				gr.Rectangle (R);
//				gr.Fill ();
//			}
//			if (SelBegin >= 0) {
//				new SolidColor(Color.DarkRed).SetAsSource(gr);
//				Rectangle R = new Rectangle (
//					rText.X + (int)SelStartCursorPos - 3,
//					rText.Y + (int)(SelBegin.Y * (fe.Ascent+fe.Descent)),
//					6,
//					(int)(fe.Ascent+fe.Descent));
//				gr.Rectangle (R);
//				gr.Fill ();
//			}
			//*******************

			for (int i = 0; i < lines.Count; i++) {
				string l = lines [i].Replace ("\t", new String (' ', Interface.TAB_SIZE));
				int lineLength = (int)gr.TextExtents (l).XAdvance;
				Rectangle lineRect = new Rectangle (
					rText.X,
					rText.Y + i * (int)(fe.Ascent+fe.Descent),
					lineLength,
					(int)(fe.Ascent+fe.Descent));

//				if (TextAlignment == Alignment.Center ||
//					TextAlignment == Alignment.Top ||
//					TextAlignment == Alignment.Bottom)
//					lineRect.X += (rText.Width - lineLength) / 2;
//				else if (TextAlignment == Alignment.Right ||
//					TextAlignment == Alignment.TopRight ||
//					TextAlignment == Alignment.BottomRight)
//					lineRect.X += (rText.Width - lineLength);
				if (string.IsNullOrWhiteSpace (l))
					continue;

				Foreground.SetAsSource (gr);
				gr.MoveTo (lineRect.X,(double)rText.Y + fe.Ascent + (fe.Ascent+fe.Descent) * i) ;

                gr.ShowText (l);
				gr.Fill ();

				if (Selectable) {
					if (SelRelease >= 0 && i >= selectionStart.Y && i <= selectionEnd.Y) {
						gr.SetSourceColor (selBackground);

						Rectangle selRect = lineRect;

						int cpStart = (int)SelStartCursorPos,
						cpEnd = (int)SelEndCursorPos;

						if (SelBegin.Y > SelRelease.Y) {
							cpStart = cpEnd;
							cpEnd = (int)SelStartCursorPos;
						}

						if (i == selectionStart.Y) {
							selRect.Width -= cpStart;
							selRect.Left += cpStart;
						}
						if (i == selectionEnd.Y)
							selRect.Width -= (lineLength - cpEnd);

						gr.Rectangle (selRect);
						gr.FillPreserve ();
						gr.Save ();
						gr.Clip ();
						gr.SetSourceColor (SelectionForeground);
						gr.MoveTo (lineRect.X, rText.Y + fe.Ascent + (fe.Ascent+fe.Descent) * i);
						gr.ShowText (l);
						gr.Fill ();
						gr.Restore ();
					}
				}
			}
		}
		#endregion

		#region Mouse handling
		void updatemouseLocalPos(Point mpos){
			mouseLocalPos = mpos - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;
			if (mouseLocalPos.X < 0)
				mouseLocalPos.X = 0;
			if (mouseLocalPos.Y < 0)
				mouseLocalPos.Y = 0;
		}
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			if (!_selectable)
				return;
			SelBegin = new Point(0,0);
			SelRelease = new Point (lines.LastOrDefault ().Length, lines.Count-1);
			RegisterForRedraw ();
		}
		protected override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);

			SelBegin = -1;
			SelRelease = -1;
			RegisterForRedraw ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(SelectionInProgress && HasFocus && _selectable))
				return;

			updatemouseLocalPos (e.Position);

			RegisterForRedraw();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (this.HasFocus && _selectable){
				updatemouseLocalPos (e.Position);
				SelBegin = -1;
				SelRelease = -1;
				SelectionInProgress = true;
				RegisterForRedraw();//TODO:should put it in properties
			}

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			if (!(this.HasFocus || _selectable))
				return;
			if (!SelectionInProgress)
				return;

			updatemouseLocalPos (e.Position);
			SelectionInProgress = false;
			RegisterForRedraw ();
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);
			if (!(this.HasFocus || _selectable))
				return;
			
			GotoWordStart ();
			SelBegin = CurrentPosition;
			GotoWordEnd ();
			SelRelease = CurrentPosition;
			SelectionInProgress = false;
			RegisterForRedraw ();
		}
		#endregion

		/// <summary>
		/// Update Current Column, line and TextCursorPos
		/// from mouseLocalPos
		/// </summary>
		void computeTextCursor(Context gr)
		{
			TextExtents te;

			double cPos = 0f;

			CurrentLine = (int)(mouseLocalPos.Y / (fe.Ascent+fe.Descent));

			//fix cu
			if (CurrentLine >= lines.Count)
				CurrentLine = lines.Count - 1;

			switch (TextAlignment) {
			case Alignment.Center:
			case Alignment.Top:
			case Alignment.Bottom:
				cPos+= ClientRectangle.Width - gr.TextExtents(lines [CurrentLine]).Width/2.0;
				break;
			case Alignment.Right:
			case Alignment.TopRight:
			case Alignment.BottomRight:
				cPos += ClientRectangle.Width - gr.TextExtents(lines [CurrentLine]).Width;
				break;
			}

			for (int i = 0; i < lines[CurrentLine].Length; i++)
			{
				string c = lines [CurrentLine].Substring (i, 1);
				if (c == "\t")
					c = new string (' ', Interface.TAB_SIZE);

				te = gr.TextExtents(c);

				double halfWidth = te.XAdvance / 2;

				if (mouseLocalPos.X <= cPos + halfWidth)
				{
					CurrentColumn = i;
					textCursorPos = cPos;
					mouseLocalPos = -1;
					return;
				}

				cPos += te.XAdvance;
			}
			CurrentColumn = lines[CurrentLine].Length;
			textCursorPos = cPos;

			//reset mouseLocalPos
			mouseLocalPos = -1;
		}
		/// <summary> Computes offsets in cairo units </summary>
		void computeTextCursorPosition(Context gr)
		{
			if (SelBegin >= 0)
				SelStartCursorPos = GetXFromTextPointer (gr, SelBegin);
			if (SelRelease >= 0)
				SelEndCursorPos = GetXFromTextPointer (gr, SelRelease);
			textCursorPos = GetXFromTextPointer (gr, new Point(CurrentColumn, CurrentLine));
		}
		/// <summary> Compute x offset in cairo unit from text position </summary>
		double GetXFromTextPointer(Context gr, Point pos)
		{
			try {
				string l = lines [pos.Y].Substring (0, pos.X).
					Replace ("\t", new String (' ', Interface.TAB_SIZE));
				return gr.TextExtents (l).XAdvance;
			} catch{
				return -1;
			}
		}
    }
}
