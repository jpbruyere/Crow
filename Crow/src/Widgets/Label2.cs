// Copyright (c) 2013-2022  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;

using System.ComponentModel;
using Glfw;

using Crow.Text;
using Drawing2D;

namespace Crow
{
    /// <summary>
    /// Simple label widget possibly multiline but without tabulation handling.
    /// </summary>
    public class Label2 : ScrollingObject
    {
		#region CTOR
		protected Label2 () { }
		public Label2(Interface iface, string style = null) : base (iface, style) { }
        #endregion
        
		protected override void onInitialized(object sender, EventArgs e)
        {
			initCommands ();
            base.onInitialized(sender, e);
        }

		protected TextBuffer buffer = new TextBuffer("");
		protected bool disableTextChangedEvent;

		public Command CMDCopy;
		protected virtual void initCommands () {
			CMDCopy = new ActionCommand ("Copy", Copy, "#icons.copy-file.svg",  false);

			ContextCommands = new CommandGroup (CMDCopy);
		}

		public event EventHandler<TextChangeEventArgs> TextChanged;
		public event EventHandler<TextSelectionChangeEventArgs> SelectionChanged;
		public virtual void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			TextChanged.Raise (this, e);
		}
		public virtual void OnSelectionChanged(Object sender, TextSelectionChangeEventArgs e)
		{
			SelectionChanged.Raise (this, e);
		}

		[DefaultValue("label")]
        public string Text
        {
			get => buffer.Span.ToString();
            set
            {
				if (buffer.ReadOnlySpan.SequenceEqual (value.AsSpan ()))
                    return;

				update(new TextChange (0, buffer.Length, value));
				NotifyValueChanged ("Text", Text);
            }
        }
		
		double targetColumn = -1;//handle line changes with long->short->long line length sequence.
		protected CharLocation? hoverLoc = null;
		protected CharLocation? currentLoc = null;
		protected CharLocation? selectionStart = null;  //selection start (row,column)

		protected virtual CharLocation? CurrentLoc {
			get => currentLoc;
			set {
				if (currentLoc == value)
					return;
				currentLoc = value;
				NotifyValueChanged ("CurrentLine", CurrentLine);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);
				NotifyValueChanged ("TabulatedColumn", TabulatedColumn);

				CMDCopy.CanExecute = !SelectionIsEmpty;
			}
		}
		public virtual int CurrentLine {
			get => currentLoc.HasValue ? currentLoc.Value.Line : 0;
			set {
				if (currentLoc?.Line == value)
					return;
				currentLoc = new CharLocation (value, currentLoc.Value.Column, currentLoc.Value.VisualCharXPosition);
				NotifyValueChanged ("CurrentLine", CurrentLine);

				CMDCopy.CanExecute = !SelectionIsEmpty;
			}
		}
		public virtual int CurrentColumn {
			get => currentLoc.HasValue ? currentLoc.Value.Column < 0 ? 0 : currentLoc.Value.Column : 0;
			set {
				if (CurrentColumn == value)
					return;
				currentLoc = new CharLocation (currentLoc.Value.Line, value);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);

				CMDCopy.CanExecute = !SelectionIsEmpty;
			}
		}
		public int TabulatedColumn {
			get => currentLoc.HasValue ? currentLoc.Value.TabulatedColumn < 0 ? 0 : currentLoc.Value.TabulatedColumn : 0;
		}

		/// <summary>
		/// Set current cursor position in label.
		/// </summary>
		/// <param name="position">Absolute character position in text.</param>
		public void SetCursorPosition (int position) {
			CharLocation loc = buffer.GetLocation (position);
			loc.Column = Math.Min (loc.Column, buffer.GetLine (loc.Line).Length);
			CurrentLoc = loc;
		}

		Color selForeground, selBackground;

		protected bool textMeasureIsUpToDate = false;
		//protected object linesMutex = new object ();

		protected Size cachedTextSize = default (Size);
		protected FontExtents fe;
		protected TextExtents te;
		protected int tabSize;
		TextAlignment _textAlignment;

		/// <summary>
		/// Background color for selected text inside this label.
		/// </summary>
		[DefaultValue ("LightSteelBlue")]
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
		[DefaultValue(4)]
		public int TabSize {
			get => tabSize;
			set {
				if (tabSize == value)
					return;
				tabSize = value;
				NotifyValueChanged("TabSize",tabSize);
			}
		}
		[DefaultValue(TextAlignment.Left)]
		public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set {
				if (value == _textAlignment)
					return;
				_textAlignment = value;

				CurrentLoc?.ResetVisualX ();
				SelectionStart?.ResetVisualX ();

				RegisterForRedraw ();
				NotifyValueChangedAuto (_textAlignment);
			}
        }		
		protected double lineHeight => fe.Ascent + fe.Descent;

		protected virtual int getAbsoluteLineIndexFromVisualLineMove (int startLine, int visualLineDiff)
			=> Math.Min (Math.Max (0, startLine + visualLineDiff), visualLineCount - 1);
		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == 0) {
				if (loc.Line == 0)
					return false;
				int newLine = getAbsoluteLineIndexFromVisualLineMove (loc.Line, -1);
				loc = new CharLocation (newLine, buffer.GetLine (newLine).Length);
			}else
				loc = new CharLocation (loc.Line, loc.Column - 1);
			updateLocation(ref loc);
			CurrentLoc = loc;
			return true;
		}
		public bool MoveRight () {
			targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == buffer.GetLine (loc.Line).Length) {
				if (loc.Line == buffer.LinesCount - 1)
					return false;
				loc = new CharLocation (
					getAbsoluteLineIndexFromVisualLineMove (loc.Line, 1), 0);
			} else
				loc = new CharLocation (loc.Line, loc.Column + 1);
			updateLocation(ref loc);
			CurrentLoc = loc;			
			return true;
		}
		public bool LineMove (int lineDiff) {
			if (!CurrentLoc.HasValue)
				return false;

			CharLocation loc = CurrentLoc.Value;
			int newLine = getAbsoluteLineIndexFromVisualLineMove (loc.Line, lineDiff);
			if (newLine == loc.Line)
				return false;

			using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);
				CharLocation newLoc;
				double targetX;
				if (targetColumn > 0) {
					targetX = targetColumn;
				} else {
					if (!loc.HasVisualX)
						updateLocation(gr, ref loc);
					targetX = loc.VisualCharXPosition;
				}

				newLoc = new CharLocation(newLine, -1, targetX);
				updateLocation(ref newLoc);

				if (newLoc.VisualCharXPosition < targetX) {
					if (targetColumn < 0)
						targetColumn = loc.VisualCharXPosition;
				} else {
					targetColumn = -1;
				}

				CurrentLoc = newLoc;
			}

			/*if (loc.Column > document.GetLine (newLine).Length) {
				if (targetColumn < 0)
					targetColumn = loc.Column;
				CurrentLoc = new CharLocation (newLine, document.GetLine (newLine).Length);
			} else if (targetColumn < 0)
				CurrentLoc = new CharLocation (newLine, loc.Column);
			else if (targetColumn > document.GetLine (newLine).Length)
				CurrentLoc = new CharLocation (newLine, document.GetLine (newLine).Length);
			else
				CurrentLoc = new CharLocation (newLine, targetColumn);*/

			return true;
		}

		/// <summary>
		/// Current Selected text span. May be used to set current position, or current selection.
		/// </summary>
		public TextSpan Selection {
			set {
				if (value.IsEmpty)
					SelectionStart = null;
				else
					SelectionStart = buffer.GetLocation (value.Start);
				CurrentLoc = buffer.GetLocation (value.End);
			}
			get {
				if (CurrentLoc == null)
					return default;
				CharLocation selStart = CurrentLoc.Value, selEnd = CurrentLoc.Value;
				if (selectionStart.HasValue) {
					if (CurrentLoc.Value.Line < selectionStart.Value.Line) {
						selStart = CurrentLoc.Value;
						selEnd = selectionStart.Value;
					} else if (CurrentLoc.Value.Line > selectionStart.Value.Line) {
						selStart = selectionStart.Value;
						selEnd = CurrentLoc.Value;
					} else if (CurrentLoc.Value.Column < selectionStart.Value.Column) {
						selStart = CurrentLoc.Value;
						selEnd = selectionStart.Value;
					} else {
						selStart = selectionStart.Value;
						selEnd = CurrentLoc.Value;
					}
				}
				return new TextSpan (buffer.GetAbsolutePosition (selStart), buffer.GetAbsolutePosition (selEnd));
			}
		}
		public string SelectedText {
			get {
				TextSpan selection = Selection;
				return selection.IsEmpty ? "" : buffer.GetText (selection).ToString ();
			}
		}
		public bool SelectionIsEmpty => selectionStart.HasValue ? Selection.IsEmpty : true;
		public CharLocation? SelectionStart {
			get => selectionStart;
			set {
				if (selectionStart == value)
					return;
				selectionStart = value;
				if (SelectionChanged != null)
					OnSelectionChanged (this, new TextSelectionChangeEventArgs (Selection));
			}
		}		
		/// <summary>
		/// on screen visible line bounded by the client rectangle
		/// </summary>
		protected int visibleLines => (int)Math.Ceiling(ClientRectangle.Height / lineHeight);
		/// <summary>
		/// total line count
		/// </summary>
		protected virtual int visualLineCount => buffer.LinesCount;

		protected virtual void measureTextBounds (IContext gr) {
			fe = gr.FontExtents;
			te = new TextExtents ();

			try {

				cachedTextSize.Height = (int)Math.Ceiling (lineHeight * Math.Max (1, visualLineCount));

				TextExtents tmp = default;
				int longestLine = 0;
				for (int i = 0; i < buffer.LinesCount; i++) {
					TextLine l = buffer.GetLine (i);
					if (l.LengthInPixel < 0) {
						if (l.Length == 0)
							l.LengthInPixel = 0;// (int)Math.Ceiling (fe.MaxXAdvance);
						else {
							gr.TextExtents (buffer.GetText (l), Interface.TAB_SIZE, out tmp);
							l.LengthInPixel = (int)Math.Ceiling (tmp.XAdvance);
							buffer.SetLine(i, l);
						}
					}
					if (l.LengthInPixel > buffer.GetLine (longestLine).LengthInPixel)
						longestLine = i;
				}
				cachedTextSize.Width = buffer.GetLine (longestLine).LengthInPixel;
				textMeasureIsUpToDate = true;

				updateMaxScrolls (LayoutingType.Height);
				updateMaxScrolls (LayoutingType.Width);
			} finally {
				
			}
		}
		protected virtual void drawContent (IContext gr) {			
			Rectangle cb = ClientRectangle;
			fe = gr.FontExtents;
			double lineHeight = fe.Ascent + fe.Descent;

			CharLocation selStart = default, selEnd = default;
			bool selectionNotEmpty = false;

			gr.Translate (-ScrollX, 0);

			//document.EnterReadLock();
			
			try {
				if (HasFocus) {
					if (currentLoc?.Column < 0) {
						updateLocation (gr, ref currentLoc);
					} else
						updateLocation (gr, ref currentLoc);
					if (selectionStart.HasValue) {
						updateLocation (gr, ref selectionStart);
						if (CurrentLoc.Value != selectionStart.Value)
							selectionNotEmpty = true;
					}
					if (selectionNotEmpty) {
						if (CurrentLoc.Value.Line < selectionStart.Value.Line) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else if (CurrentLoc.Value.Line > selectionStart.Value.Line) {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						} else if (CurrentLoc.Value.Column < selectionStart.Value.Column) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						}
					}
				}

				if (buffer.Length > 0) {
					int skippedLines = (int)Math.Floor(ScrollY / lineHeight);
					int curLine = skippedLines;
					double y = -(ScrollY % lineHeight);

					Foreground?.SetAsSource (IFace, gr);

					TextExtents extents;
					Span<byte> bytes = stackalloc byte[128];
					
					while (curLine < buffer.LinesCount && curLine - skippedLines < visibleLines) {
						
						int encodedBytes = -1;
						TextLine l = buffer.GetLine (curLine);
						if (l.Length > 0) {
							int size = l.Length * 4 + 1;
							if (bytes.Length < size)
								bytes = new byte[size];

							encodedBytes = buffer.GetText (l).ToUtf8 (bytes);
							bytes[encodedBytes++] = 0;

							if (l.LengthInPixel < 0) {
								gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
								l.LengthInPixel = (int)extents.XAdvance;
								buffer.SetLine(curLine, l);
							}
						}

						RectangleD lineRect = new RectangleD (
							(int)cb.X,
							y + cb.Top, l.LengthInPixel, lineHeight);

						if (encodedBytes > 0) {
							gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
							gr.ShowText (bytes.Slice (0, encodedBytes));
						}
						/********** DEBUG TextLineCollection *************
						gr.SetSource (Colors.Red);
						gr.SetFontSize (9);
						gr.MoveTo (700, lineRect.Y + fe.Ascent);
						gr.ShowText ($"({lines[i].Start}, {lines[i].End}, {lines[i].EndIncludingLineBreak})");
						gr.SetFontSize (Font.Size);
						Foreground.SetAsSource (IFace, gr);
						********** DEBUG TextLineCollection *************/

						if (selectionNotEmpty) {
							RectangleD selRect = lineRect;

							if (curLine >= selStart.Line && curLine <= selEnd.Line) {
								if (selStart.Line == selEnd.Line) {
									selRect.X = selStart.VisualCharXPosition + cb.X;
									selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
								} else if (curLine == selStart.Line) {
									double newX = selStart.VisualCharXPosition + cb.X;
									selRect.Width -= (newX - selRect.X) - 10.0;
									selRect.X = newX;
								} else if (curLine == selEnd.Line) {
									selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
								} else
									selRect.Width += 10.0;
							} else {
								y += lineHeight;
								curLine++;
								continue;
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
					
						y += lineHeight;
						curLine++;
					}
				}
			} finally {
				//document.ExitReadLock ();
			}

			gr.Translate (ScrollX, 0);
		}
		protected int getLineIndexFromMousePosition (Point mouseLocalPos) =>
			(int)Math.Min (Math.Max (0, Math.Floor ((mouseLocalPos.Y + ScrollY)/ lineHeight)), visualLineCount - 1);
		protected int getVisualLineIndex (Point mouseLocalPos) =>
			(int)Math.Min (Math.Max (0, Math.Floor (mouseLocalPos.Y / lineHeight)), visibleLines - 1);
		protected virtual int visualCurrentLine => CurrentLoc.HasValue ? CurrentLoc.Value.Line : 0;

		protected virtual void updateHoverLocation (Point mouseLocalPos) {
			int hoverLine = getLineIndexFromMousePosition (mouseLocalPos);
			NotifyValueChanged("MouseY", mouseLocalPos.Y + ScrollY);
			NotifyValueChanged("ScrollY", ScrollY);
			NotifyValueChanged("VisibleLines", visibleLines);
			NotifyValueChanged("HoverLine", hoverLine);
			CharLocation newLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X + ScrollX);
			updateLocation (ref newLoc);
			hoverLoc = newLoc;
		}
		protected void updateLocation(ref CharLocation loc) {
			if (loc.HasVisualX)
				return;
			using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);
				updateLocation(gr, ref loc);
			}				
		}
		[Obsolete("use override without clientWidth parameter")]		
		protected void updateLocation (IContext gr, int clientWidth, ref CharLocation? location) {
			if (location == null)
				return;
			CharLocation loc = location.Value;
			updateLocation(gr, ref loc);
			location = loc;
		}
		protected void updateLocation (IContext gr, ref CharLocation? location) {
			if (location == null)
				return;
			CharLocation loc = location.Value;
			updateLocation(gr, ref loc);
			location = loc;
		}			
		protected void updateLocation (IContext gr, ref CharLocation loc) {
			if (loc.HasVisualX)
				return;
			
			ReadOnlySpan<char> curLine = buffer.GetText(buffer.GetLine (loc.Line));
			if (loc.Column >= 0) {
				//int encodedBytes = Crow.Text.Encoding2.ToUtf8 (curLine.Slice (0, loc.Column), bytes);
#if DEBUG
				if (loc.Column > curLine.Length) {
					System.Diagnostics.Debug.WriteLine ($"loc.Column: {loc.Column} curLine.Length:{curLine.Length}");
					loc.Column = curLine.Length;
				}
#endif
				ReadOnlySpan<char> buff = curLine.Slice (0, loc.Column);
				loc.TabulatedColumn = buff.Count('\t') * (tabSize-1) + buff.Length;
				loc.VisualCharXPosition = gr.TextExtents (buff).XAdvance;
			} else {
				TextExtents te;
				Span<byte> bytes = stackalloc byte[5];//utf8 single char buffer + '\0'
				int totChar = 0;
				double cPos = 0;

				for (int i = 0; i < curLine.Length; i++) {
					int encodedBytes = curLine.Slice (i, 1).ToUtf8 (bytes, ref totChar, tabSize);
					bytes[encodedBytes] = 0;

					gr.TextExtents (bytes, out te);
					double halfWidth = te.XAdvance / 2;

					if (loc.VisualCharXPosition <= cPos + halfWidth) {
						loc.Column = i;
						loc.VisualCharXPosition = cPos;
						loc.TabulatedColumn = totChar;
						return;
					}

					cPos += te.XAdvance;
				}
				loc.Column = curLine.Length;
				loc.VisualCharXPosition = cPos;
				loc.TabulatedColumn = totChar;
			}
		}

		protected void checkShift (KeyEventArgs e) {
			if (e.Modifiers.HasFlag (Modifier.Shift)) {
				if (!selectionStart.HasValue)
					SelectionStart = CurrentLoc;
			} else
				SelectionStart = null;
		}

		public override void OnLayoutChanges (LayoutingType layoutType) {
			base.OnLayoutChanges (layoutType);
			updateMaxScrolls (layoutType);
		}
		public override int measureRawSize(LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {
				if (!textMeasureIsUpToDate) {
					using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);
						measureTextBounds (gr);
					}
				}
				return lt == LayoutingType.Height ? cachedTextSize.Height + 2 * Margin.Height : cachedTextSize.Width + 2 * Margin.Width;
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		protected override void onDraw (IContext gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			if (!textMeasureIsUpToDate) {
				measureTextBounds (gr);
			}

			if (ClipToClientRect) {
				gr.Save ();
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			drawContent (gr);

			if (ClipToClientRect)
				gr.Restore ();
		}
		

		#region Mouse handling
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			if (CurrentLoc == null)
				CurrentLoc = new CharLocation (0, 0);

			RegisterForRedraw ();
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e) {
			base.onMouseEnter (sender, e);
			if (Focusable) {
				HasFocus = true;
				IFace.MouseCursor = MouseCursor.ibeam;
			}
		}
        public override void onMouseLeave(object sender, MouseMoveEventArgs e)
        {
            base.onMouseLeave(sender, e);
			if (Focusable && HasFocus) {
				HasFocus = false;
				CurrentLoc = null;
			}
        }
        public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			mouseMove (e);
		}
		public override void onMouseWheel(object sender, MouseWheelEventArgs e)
		{
			base.onMouseWheel(sender, e);
			mouseMove (e);
		}
		protected virtual void mouseMove (MouseEventArgs e) {
			updateHoverLocation (ScreenPointToLocal (e.Position));

			if (HasFocus && IFace.IsDown (MouseButton.Left)) {
				CurrentLoc = hoverLoc;
				autoAdjustScroll = true;
				IFace.forceTextCursor();
				RegisterForRedraw ();
			}
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (!e.Handled && e.Button == Glfw.MouseButton.Left) {
				targetColumn = -1;
				if (HasFocus) {
					if (!IFace.Shift)
						SelectionStart = hoverLoc;
					else if (!selectionStart.HasValue)
						SelectionStart = CurrentLoc;
					CurrentLoc = hoverLoc;
					IFace.forceTextCursor();
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
			if (e.Button != MouseButton.Left || !HasFocus || !selectionStart.HasValue)
				return;
			if (selectionStart.Value == CurrentLoc.Value)
				SelectionStart = null;
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);
			if (e.Button != MouseButton.Left || !HasFocus)
				return;

			SelectionStart = buffer.GetWordStart (CurrentLoc.Value);
			CurrentLoc = buffer.GetWordEnd (CurrentLoc.Value);
			RegisterForRedraw ();
		}
		#endregion

		public override void onKeyDown (object sender, KeyEventArgs e) {
			Key key = e.Key;
			TextSpan selection = Selection;

			/*document.EnterReadLock();
			try {*/
				switch (key) {
				case Key.Escape:
					SelectionStart = null;
					CurrentLoc = buffer.GetLocation (selection.Start);
					RegisterForRedraw ();
					break;
				case Key.Tab:
					update (new TextChange (selection.Start, selection.Length, "\t"));
					break;
				case Key.PageUp:
					checkShift (e);
					LineMove (-visibleLines);
					RegisterForRedraw ();
					break;
				case Key.PageDown:
					checkShift (e);
					LineMove (visibleLines);
					RegisterForRedraw ();
					break;
				case Key.Home:
					targetColumn = -1;
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = new CharLocation (0, 0);
					else
						CurrentLoc = new CharLocation (CurrentLoc.Value.Line, 0);
					RegisterForRedraw ();
					break;
				case Key.End:
					checkShift (e);
					int l = e.Modifiers.HasFlag (Modifier.Control) ? buffer.LinesCount - 1 : CurrentLoc.Value.Line;
					CurrentLoc = new CharLocation (l, buffer.GetLine (l).Length);
					RegisterForRedraw ();
					break;
				case Key.Left:
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = buffer.GetWordStart (CurrentLoc.Value);
					else
						MoveLeft ();
					RegisterForRedraw ();
					break;
				case Key.Right:
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = buffer.GetWordEnd (CurrentLoc.Value);
					else
						MoveRight ();
					RegisterForRedraw ();
					break;
				case Key.Up:
					checkShift (e);
					LineMove (-1);
					RegisterForRedraw ();
					break;
				case Key.Down:
					checkShift (e);
					LineMove (1);
					RegisterForRedraw ();
					break;
				case Key.A:
					if (e.Modifiers.HasFlag (Modifier.Control)) {
						SelectionStart = new CharLocation (0, 0);
						CurrentLoc = buffer.EndLocation;
					}
					break;
				default:
					base.onKeyDown (sender, e);
					return;
				}
				autoAdjustScroll = true;
				e.Handled = true;
			/*} finally {
				document.ExitReadLock ();
			}*/
		}


		protected bool autoAdjustScroll = false;//if scrollXY is changed directly, dont try adjust scroll to cursor
		protected virtual void updateMaxScrolls (LayoutingType layout) {
			Rectangle cb = ClientRectangle;
			if (layout == LayoutingType.Width) {
				MaxScrollX = cachedTextSize.Width - cb.Width;
				NotifyValueChanged ("PageWidth", ClientRectangle.Width);
				if (cachedTextSize.Width > 0)
					NotifyValueChanged ("ChildWidthRatio", Math.Min (1.0, (double)cb.Width / cachedTextSize.Width));
			} else if (layout == LayoutingType.Height) {
				MaxScrollY = cachedTextSize.Height - cb.Height;
				NotifyValueChanged ("PageHeight", ClientRectangle.Height);
				if (cachedTextSize.Height > 0)
					NotifyValueChanged ("ChildHeightRatio", Math.Min (1.0, (double)cb.Height / cachedTextSize.Height));
			}
		}
		public virtual void Cut () {
			TextSpan selection = Selection;
			if (selection.IsEmpty)
				return;
			IFace.Clipboard = SelectedText;
			update (new TextChange (selection.Start, selection.Length, ""));
		}
		public virtual void Copy () {
			TextSpan selection = Selection;
			if (selection.IsEmpty)
				return;
			IFace.Clipboard = SelectedText;
		}

		protected virtual void update (TextChange change) {
			buffer.Update(change);
			if (!disableTextChangedEvent)
				OnTextChanged (this, new TextChangeEventArgs (change));
			CurrentLoc = null;
			textMeasureIsUpToDate = false;
			IFace.forceTextCursor();
			autoAdjustScroll = true;
			RegisterForGraphicUpdate ();
		}

	}
}
