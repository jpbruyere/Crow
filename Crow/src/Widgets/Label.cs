// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Cairo;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using Glfw;

namespace Crow {
	internal struct TextSpan
    {
		public readonly int Start;
		public readonly int End;
		public TextSpan (int start, int end) {
			Start = start;
			End = end;
        }
	}

    [DebuggerDisplay ("{Line}, {Column}, {VisualCharXPosition}")]
    internal struct CharLocation : IEquatable<CharLocation>
    {
		public readonly int Line;
		public int Column;
		public double VisualCharXPosition;
		public CharLocation (int line, int column, double visualX = -1) {
			Line = line;
			Column = column;
			VisualCharXPosition = visualX;
        }
		public bool HasVisualX => Column >= 0 && VisualCharXPosition >= 0;

		public static bool operator ==(CharLocation a, CharLocation b)
			=> a.Equals (b);
		public static bool operator != (CharLocation a, CharLocation b)
			=> !a.Equals (b);
		public bool Equals (CharLocation other) {
			return Column < 0 ?
				Line == other.Line && VisualCharXPosition == other.VisualCharXPosition :
				Line == other.Line && Column == other.Column;
		}
        public override bool Equals (object obj) => obj is CharLocation loc ? Equals(loc) : false;
        public override int GetHashCode () {
            return Column < 0 ?
				HashCode.Combine (Line, VisualCharXPosition) :
				HashCode.Combine (Line, Column);
		}
    }
	internal struct LineSpan
    {
		public readonly int Start;
		public readonly int End;
		public readonly int EndIncludingLineBreak;
		public int LengthInPixel;
		public int Length => End - Start;
		public int LengthIncludingLineBreak => EndIncludingLineBreak - Start;
		public int LineBreakLength => EndIncludingLineBreak - End;
		public bool HasLineBreak => LineBreakLength > 0;
		public LineSpan (int start, int end, int endIncludingLineBreak) {
			Start = start;
			End = end;
			EndIncludingLineBreak = endIncludingLineBreak;
			LengthInPixel = -1;
        }
		public LineSpan WithStartOffset (int start) => new LineSpan (Start + start, End, EndIncludingLineBreak);
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
	/// <summary>
	/// Simple label widget possibly multiline but without tabulation handling.
	/// </summary>
	public class Label : Widget
    {
		#region CTOR
		protected Label () {}
		public Label(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		/// <summary>
		/// Occurs when Text has changed.
		/// </summary>
		public event EventHandler<TextChangeEventArgs> TextChanged;

		public virtual void OnTextChanged(Object sender, TextChangeEventArgs e)
		{
			textMeasureIsUpToDate = false;
			NotifyValueChanged ("Text", Text);
			TextChanged.Raise (this, e);
		}
        //TODO:change protected to private

		#region private and protected fields
		string _text;
        TextAlignment _textAlignment;
		bool horizontalStretch;
		bool verticalStretch;
		bool _selectable;
		bool _multiline;
		Color selBackground;
		Color selForeground;

		//Point mouseLocalPos = -1;//mouse coord in widget space, filled only when clicked

		
		CharLocation? hoverLoc = null;
		CharLocation? currentLoc = null;
		CharLocation? selectionStart = null;	//selection start (row,column)

		protected FontExtents fe;
		protected TextExtents te;
		#endregion
		/// <summary>
		/// Background color for selected text inside this label.
		/// </summary>
		[DefaultValue("SteelBlue")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (selBackground == value)
					return;
				selBackground = value;
				NotifyValueChangedAuto (selBackground);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Selected text color inside this label.
		/// </summary>
		[DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (selForeground == value)
					return;
				selForeground = value;
				NotifyValueChangedAuto (selForeground);
				RegisterForRedraw ();
			}
		}
		void resetLocationXs () {
			if (currentLoc.HasValue) {
				CharLocation cl = currentLoc.Value;
				cl.VisualCharXPosition = -1;
				currentLoc = cl;
			}
			if (selectionStart.HasValue) {
				CharLocation cl = selectionStart.Value;
				cl.VisualCharXPosition = -1;
				selectionStart = cl;
			}
		}
		/// <summary>
		/// If measure is not 'Fit', align text inside the bounds of this label.
		/// </summary>
		[DefaultValue(TextAlignment.Left)]		
		public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set {
				if (value == _textAlignment)
					return;
				_textAlignment = value;
				resetLocationXs ();

				RegisterForRedraw ();
				NotifyValueChangedAuto (_textAlignment);
			}
        }		
		/// <summary>
		/// Text to display in this label. May include linebreaks if Multiline is 'true'.
		/// If Multiline is false, linebreaks will be treated as unrecognized unicode char.
		/// </summary>
		[DefaultValue("label")]
        public string Text
        {
			get => _text;
            set
            {
				if (_text.AsSpan ().SequenceEqual (value.AsSpan ()))
                    return;

                _text = value;

				getLines ();

				OnTextChanged (this, new TextChangeEventArgs (Text));
				RegisterForGraphicUpdate ();
            }
        }

		/// <summary>
		/// If 'true', linebreaks will be interpreted. If 'false', linebreaks are threated as unprintable
		/// unicode characters.
		/// </summary>
		[DefaultValue(false)]
		public bool Multiline
		{
			get => _multiline;
			set
			{
				if (value == _multiline)
					return;
				_multiline = value;
				NotifyValueChangedAuto (_multiline);
				RegisterForGraphicUpdate();
			}
		}
		/*[DefaultValue(0)]
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
				NotifyValueChangedAuto (_currentCol);

				Rectangle cb = ClientRectangle;

				if (Width == Measure.Fit || cb.Width >= cachedTextSize.Width) {
					xTranslation = 0;
					return;
				}
				int xpos = xposition;
				if (xTranslation + xpos > cb.Width)
					xTranslation = cb.Width - xpos;
				else if (xpos < -xTranslation)
					xTranslation = -xpos;
				RegisterForRedraw ();
			}
		}
		int xTranslation = 0;
		int xposition {
			get {
				using (Context gr = new Context (IFace.surf)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					gr.FontOptions = Interface.FontRenderingOptions;
					gr.Antialias = Interface.Antialias;
					try {
						return (int)Math.Ceiling (gr.TextExtents (_text.GetLine (lines[_currentLine], _currentCol), Interface.TAB_SIZE).XAdvance);
					} catch {
						System.Diagnostics.Debug.WriteLine ("xpos measuring fault in label");
						return 0;
					}
				}
			}
		}

		[DefaultValue(0)]
		public int CurrentLine{
			get { return _currentLine; }
			set {
				if (value == _currentLine)
					return;
				if (value >= lines.Length)
					_currentLine = lines.Length-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;
				NotifyValueChangedAuto (_currentLine);
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
				NotifyValueChangedAuto (_selBegin);
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
				NotifyValueChangedAuto (_selRelease);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		/// <summary>
		/// return char at CurrentLine, CurrentColumn
		/// </summary>
		[XmlIgnore]protected Char CurrentChar => _text[lines[CurrentLine].Start + CurrentColumn];
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
					return _text.Substring (lines[selectionStart.Y].Start + selectionStart.X, selectionEnd.X - selectionStart.X);

				StringBuilder tmp = new StringBuilder (_text.GetLineIncludingLineBreak (lines[selectionStart.Y], selectionStart.X).ToString ());
				for (int l = selectionStart.Y + 1; l < selectionEnd.Y; l++)
					tmp.Append (_text.GetLineIncludingLineBreak (lines[l]).ToString ());
				tmp.Append (_text.Substring (lines[selectionEnd.Y].Start, selectionEnd.X));
				return tmp.ToString ();
			}
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return SelRelease < 0; } }

		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		*/
		public bool MoveLeft(){
			targetColumn = -1;
			CharLocation loc = currentLoc.Value;
			if (loc.Column == 0) {
				if (loc.Line == 0)
					return false;
				currentLoc = new CharLocation (loc.Line - 1, lines[loc.Line - 1].Length);
            }else
				currentLoc = new CharLocation (loc.Line, loc.Column - 1);
			return true;
		}
		public bool MoveRight () {
			targetColumn = -1;
			CharLocation loc = currentLoc.Value;
			if (loc.Column == lines[loc.Line].Length) {
				if (loc.Line == lines.Length - 1)
					return false;
				currentLoc = new CharLocation (loc.Line + 1, 0);
			} else
				currentLoc = new CharLocation (loc.Line, loc.Column + 1);
			return true;
		}
		int targetColumn = -1;
		public bool MoveUp () {
			CharLocation loc = currentLoc.Value;
			if (loc.Line == 0)
				return false;
			
			if (loc.Column > lines[loc.Line - 1].Length) {
				if (targetColumn < 0)
					targetColumn = loc.Column;
				currentLoc = new CharLocation (loc.Line - 1, lines[loc.Line - 1].Length);
			}else if (targetColumn < 0)
				currentLoc = new CharLocation (loc.Line - 1, loc.Column);
			else if (targetColumn > lines[loc.Line - 1].Length)
				currentLoc = new CharLocation (loc.Line - 1, lines[loc.Line - 1].Length);
			else
				currentLoc = new CharLocation (loc.Line - 1, targetColumn);

			return true;
		}
		public bool MoveDown () {
			CharLocation loc = currentLoc.Value;
			if (loc.Line == lines.Length - 1)
				return false;

			if (loc.Column > lines[loc.Line + 1].Length) {
				if (targetColumn < 0)
					targetColumn = loc.Column;
				currentLoc = new CharLocation (loc.Line + 1, lines[loc.Line + 1].Length);
			} else if (targetColumn < 0)
				currentLoc = new CharLocation (loc.Line + 1, loc.Column);
			else if (targetColumn > lines[loc.Line + 1].Length)
				currentLoc = new CharLocation (loc.Line + 1, lines[loc.Line + 1].Length);
			else
				currentLoc = new CharLocation (loc.Line + 1, targetColumn);

			return true;
		}
		/*
		/// <summary>
		/// Moves cursor one char to the right.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			int tmp = _currentCol + 1;
			if (tmp > lines [_currentLine].Length){
				if (CurrentLine == lines.Length - 1)
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
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
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
					if (CurrentLine == 0 && lines.Length == 1)
						return;
					CurrentLine--;
					CurrentColumn = lines [CurrentLine].Length;

					Text = _text.Remove (lines[CurrentLine].End, lines[CurrentLine].LineBreakLength);
					return;
				}
				CurrentColumn--;
				Text = _text.Remove (lines[CurrentLine].Start + CurrentColumn, 1);				
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
			//OnTextChanged (this, new TextChangeEventArgs (Text));
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
				ReadOnlySpan<char> a = _text.AsSpan ().Slice (0 ,lines[CurrentLine].Start + CurrentColumn);
				ReadOnlySpan<char> b = _text.AsSpan ().Slice (lines[CurrentLine].Start + CurrentColumn);

				_text = string.Concat( (a, str.AsSpan(), b);

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
		*/
		bool textMeasureIsUpToDate = false;
		Size cachedTextSize = default(Size);
		LineSpan[] lines;
		void getLines () {
			if (string.IsNullOrEmpty (_text)) {
				lines = new LineSpan[] { new LineSpan (0, 0, 0) };
				return;
			}
			if (!_multiline) {
				lines = new LineSpan[] { new LineSpan (0, _text.Length, _text.Length) };
				return;
			}

			List<LineSpan> _lines = new List<LineSpan> ();			
			int start = 0, i = 0;
			while (i < _text.Length) {
				char c = _text[i];
				if (c == '\r') {
					if (++i < _text.Length) {
						if (_text[i] == '\n')
							_lines.Add (new LineSpan (start, i - 1, ++i));
						else
							_lines.Add (new LineSpan (start, i - 1, i));
					} else
						_lines.Add (new LineSpan (start, i - 1, i));
					start = i;
				} else if (c == '\n') {
					if (++i < _text.Length) {
						if (_text[i] == '\r')
							_lines.Add (new LineSpan (start, i - 1, ++i));
						else
							_lines.Add (new LineSpan (start, i - 1, i));
					} else
						_lines.Add (new LineSpan (start, i - 1, i));
					start = i;

				} else if (c == '\u0085' || c == '\u2028' || c == '\u2029')
					_lines.Add (new LineSpan (start, i - 1, i));
				else
					i++;
			}

			if (start < i)
				_lines.Add (new LineSpan (start, _text.Length, _text.Length));
			else
				_lines.Add (new LineSpan (_text.Length, _text.Length, _text.Length));

			lines = _lines.ToArray ();
		}


		#region GraphicObject overrides
		public override int measureRawSize(LayoutingType lt)
		{
			if (lines == null)
				getLines ();

			if (!textMeasureIsUpToDate) {
				using (Context gr = new Context (IFace.surf)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					gr.FontOptions = Interface.FontRenderingOptions;
					gr.Antialias = Interface.Antialias;

					fe = gr.FontExtents;
					te = new TextExtents ();

					cachedTextSize.Height = (int)Math.Ceiling ((fe.Ascent+fe.Descent) * Math.Max (1, lines.Length));

					TextExtents tmp = default;
					int longestLine = 0;
					for (int i = 0; i < lines.Length; i++) {							
						if (lines[i].LengthInPixel < 0) {
							if (lines[i].Length == 0)
								lines[i].LengthInPixel = 0;// (int)Math.Ceiling (fe.MaxXAdvance);
							else {
								gr.TextExtents (_text.GetLine (lines[i]), Interface.TAB_SIZE, out tmp);
								lines[i].LengthInPixel = (int)Math.Ceiling (tmp.XAdvance);
							}
						}
						if (lines[i].LengthInPixel > lines[longestLine].LengthInPixel)
							longestLine = i;
					}
					cachedTextSize.Width = lines[longestLine].LengthInPixel;
					textMeasureIsUpToDate = true;				
				}
			}
			return Margin * 2 + (lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width);
		}

		double getX (int clientWidth, ref LineSpan ls) {
			switch (TextAlignment) {
			case TextAlignment.Right:
				return clientWidth - ls.LengthInPixel;				
			case TextAlignment.Center:
				return clientWidth / 2 - ls.LengthInPixel / 2;
			}
			return 0;
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			gr.Save ();			

			Rectangle cb = ClientRectangle;

			fe = gr.FontExtents;
			int lineHeight = (int)(fe.Ascent + fe.Descent);

			CharLocation selStart = default, selEnd = default;
			bool selectionNotEmpty = false;

			if (HasFocus) {
				updateLocation (gr, cb.Width, ref currentLoc);
				if (selectionStart.HasValue) {
					updateLocation (gr, cb.Width, ref selectionStart);
					if (currentLoc.Value != selectionStart.Value)
						selectionNotEmpty = true;
					
				}
				if (selectionNotEmpty) {
					if (currentLoc.Value.Line < selectionStart.Value.Line) {
						selStart = currentLoc.Value;
						selEnd = selectionStart.Value;
					}else if (currentLoc.Value.Line > selectionStart.Value.Line) {
						selStart = selectionStart.Value;
						selEnd = currentLoc.Value;
					} else if (currentLoc.Value.Column < selectionStart.Value.Column) {
						selStart = currentLoc.Value;
						selEnd = selectionStart.Value;
					} else {
						selStart = selectionStart.Value;
						selEnd = currentLoc.Value;
					}
				} else {
					Foreground.SetAsSource (IFace, gr);
					gr.LineWidth = 1.0;
					gr.MoveTo (0.5 + currentLoc.Value.VisualCharXPosition + cb.X, cb.Y + currentLoc.Value.Line * lineHeight);
					gr.LineTo (0.5 + currentLoc.Value.VisualCharXPosition + cb.X, cb.Y + (currentLoc.Value.Line + 1) * lineHeight);
					gr.Stroke ();
				}
			}

			if (string.IsNullOrEmpty (_text)) {
				gr.Restore ();
				return;
			}

			Foreground.SetAsSource (IFace, gr);

			TextExtents extents;
			Span<byte> bytes = stackalloc byte[128];			

			for (int i = 0; i < lines.Length; i++) {
				int encodedBytes = -1;
				if (lines[i].Length > 0) {

					int size = lines[i].Length * 4 + 1;
					if (bytes.Length < size)
						bytes = size > 512 ? new byte[size] : stackalloc byte[size];

					encodedBytes = Encoding.UTF8.GetBytes (_text.GetLine (lines[i]), bytes);
					bytes[encodedBytes++] = 0;

					if (lines[i].LengthInPixel < 0) {
						gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
						lines[i].LengthInPixel = (int)extents.XAdvance;
					}
				}

				Rectangle lineRect = new Rectangle (
					Width.IsFit && !Multiline ? cb.X : (int)getX (cb.Width, ref lines[i]) + cb.X,
					cb.Y + i * lineHeight,
					lines[i].LengthInPixel,
					lineHeight);

				if (encodedBytes > 0) {
					gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
					gr.ShowText (bytes.Slice (0, encodedBytes));
				}

				if (HasFocus && selectionNotEmpty) {
					Rectangle selRect = lineRect;
					if (_multiline) {
						if (i >= selStart.Line && i <= selEnd.Line) {
							if (selStart.Line == selEnd.Line) {
								selRect.X = (int)selStart.VisualCharXPosition + cb.X;
								selRect.Width = (int)(selEnd.VisualCharXPosition - selStart.VisualCharXPosition);
							} else if (i == selStart.Line) {
								int newX = (int)selStart.VisualCharXPosition + cb.X;
								selRect.Width -= (newX - selRect.X) - 10;
								selRect.X = newX;
							} else if (i == selEnd.Line) {
								selRect.Width = (int)selEnd.VisualCharXPosition - selRect.X;
							} else								
								selRect.Width += 10;
						} else
							continue;
                    } else {
						selRect.X = (int)selStart.VisualCharXPosition + cb.X;
						selRect.Width = (int)(selEnd.VisualCharXPosition - selStart.VisualCharXPosition);						
					}

					gr.SetSource (selBackground);
					gr.Rectangle (selRect);
					if (encodedBytes < 0)
						gr.Fill ();
					else {
						gr.FillPreserve ();
						gr.Save ();
						gr.Clip ();
						gr.SetSource (SelectionForeground);
						gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
						gr.ShowText (bytes.Slice (0, encodedBytes));
						gr.Restore ();
					}
					Foreground.SetAsSource (IFace, gr);
				}
			}

			gr.Restore ();
		}
		#endregion

		#region Mouse handling
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);
			if (currentLoc == null) {
				selectionStart = new CharLocation (0, 0);				
				currentLoc = new CharLocation (lines.Length - 1, lines[lines.Length - 1].Length);
			}
			RegisterForRedraw ();
		}
		protected override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);
			RegisterForRedraw ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			Point mouseLocalPos = e.Position - ScreenCoordinates (Slot).TopLeft - ClientRectangle.TopLeft;
			int hoverLine = _multiline ?
				(int)Math.Min (Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent + fe.Descent))), lines.Length - 1) : 0;
			hoverLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X);

			if (HasFocus && IFace.IsDown (Glfw.MouseButton.Left)) {
				currentLoc = hoverLoc;
				RegisterForRedraw ();				
			}
		}
		
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (e.Button == Glfw.MouseButton.Left) {
				targetColumn = -1;
				if (HasFocus) {					
					if (!IFace.Shift)						 
						selectionStart = hoverLoc;
					else if (!selectionStart.HasValue)
						selectionStart = currentLoc;
					currentLoc = hoverLoc;

					RegisterForRedraw ();
					e.Handled = true;
				}					
			}
			base.onMouseDown (sender, e);

			//done at the end to set 'hasFocus' value after testing it
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			if (e.Button != Glfw.MouseButton.Left || !HasFocus || !selectionStart.HasValue)
				return;			
			if (selectionStart.Value == currentLoc.Value) {
				selectionStart = null;
				//RegisterForRedraw ();
			}			
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);
			/*if (!(this.HasFocus || _selectable))
				return;
			
			GotoWordStart ();
			SelBegin = CurrentPosition;
			GotoWordEnd ();
			SelRelease = CurrentPosition;
			SelectionInProgress = false;*/
			RegisterForRedraw ();
		}
		#endregion

		#region Keyboard handling
		void checkShift () {
			if (IFace.Shift) {
				if (!selectionStart.HasValue)
					selectionStart = currentLoc;
			} else
				selectionStart = null;
		}
		public override void onKeyDown (object sender, KeyEventArgs e) {
			
			switch (e.Key) {
			case Key.Escape:
				selectionStart = null;
				RegisterForRedraw ();
				break;
			case Key.Home:
				checkShift ();
				if (IFace.Ctrl)
					currentLoc = new CharLocation (0, 0);
				else
					currentLoc = new CharLocation (currentLoc.Value.Line, 0);
				RegisterForRedraw ();
				break;
			case Key.End:
				checkShift ();
				int l = IFace.Ctrl ? lines.Length - 1 : currentLoc.Value.Line;
				currentLoc = new CharLocation (l, lines[l].Length);
				RegisterForRedraw ();
				break;
/*			case Key.Insert:
				if (IFace.Shift)
					this.Insert (IFace.Clipboard);
				else if (IFace.Ctrl && !selectionIsEmpty)
					IFace.Clipboard = this.SelectedText;
				break;*/
			case Key.Left:
				checkShift ();
				/*if (IFace.Ctrl)
					GotoWordStart ();
				else*/
				MoveLeft ();
				RegisterForRedraw ();
				break;
			case Key.Right:
				checkShift ();
				/*if (IFace.Ctrl)
					GotoWordStart ();
				else*/
				MoveRight ();
				RegisterForRedraw ();
				break;
			case Key.Up:
				checkShift ();
				MoveUp ();
				RegisterForRedraw ();
				break;
			case Key.Down:
				checkShift ();
				MoveDown ();
				RegisterForRedraw ();
				break; 
			default:
				base.onKeyDown (sender, e);
				return;
			}
			e.Handled = true;			
			
		}
        #endregion
        /// <summary>
        /// Update Current Column, line and TextCursorPos
        /// from mouseLocalPos
        /// </summary>
        void updateLocation (Context gr, int clientWidth, ref CharLocation? location)
		{
			if (location == null)
				return;
			CharLocation loc = location.Value;
			if (loc.HasVisualX)
				return;
			LineSpan ls = lines[loc.Line];
			ReadOnlySpan<char> curLine = _text.GetLine (lines[loc.Line]);
			double cPos = Width.IsFit && !Multiline ? 0 : getX (clientWidth, ref ls);
			
			if (loc.Column >= 0) {
				loc.VisualCharXPosition = gr.TextExtents (curLine.Slice (0, loc.Column), Interface.TAB_SIZE).XAdvance + cPos;
				location = loc;
				return;
			}

			TextExtents te;			
			Span<byte> bytes = stackalloc byte[5];//utf8 single char buffer + '\0'

			for (int i = 0; i < ls.Length; i++)
			{
				int encodedBytes = Encoding.UTF8.GetBytes (curLine.Slice (i, 1), bytes);
				bytes[encodedBytes] = 0;

				gr.TextExtents (bytes, out te);
				double halfWidth = te.XAdvance / 2;

				if (loc.VisualCharXPosition <= cPos + halfWidth)
				{
					loc.Column = i;
					loc.VisualCharXPosition = cPos;
					location = loc;
					return;
				}

				cPos += te.XAdvance;
			}
			loc.Column = ls.Length;
			loc.VisualCharXPosition = cPos;
			location = loc;
		}


    }
}
