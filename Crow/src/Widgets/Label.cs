// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Cairo;
using System.ComponentModel;
using Glfw;
using Crow.Text;

namespace Crow
{
    /// <summary>
    /// Simple label widget possibly multiline but without tabulation handling.
    /// </summary>
    public class Label : Widget, IEditableTextWidget
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
			TextChanged.Raise (this, e);
		}
        //TODO:change protected to private

		#region private and protected fields
		protected string _text;
        TextAlignment _textAlignment;
		bool _multiline;		
		Color selBackground;
		Color selForeground;

		int targetColumn = -1;//handle line changes with long->short->long line length sequence.		

		protected CharLocation? hoverLoc = null;
		protected CharLocation? currentLoc = null;
		protected CharLocation? selectionStart = null;  //selection start (row,column)

		protected CharLocation? CurrentLoc {
			get => currentLoc;
			set {
				if (currentLoc == value)
					return;
				currentLoc = value;
				NotifyValueChanged ("CurrentLine", CurrentLine);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);
            }
        }
		public int CurrentLine {
			get => currentLoc.HasValue ? currentLoc.Value.Line : 0;
			set {
				if (currentLoc?.Line == value)
					return;
				currentLoc = new CharLocation (value, currentLoc.Value.Column, currentLoc.Value.VisualCharXPosition);
				NotifyValueChanged ("CurrentLine", CurrentLine);
			}
        }
		public int CurrentColumn {
			get => currentLoc.HasValue ? currentLoc.Value.Column < 0 ? 0 : currentLoc.Value.Column : 0;
			set {
				if (currentLoc?.Line == value)
					return;
				currentLoc = new CharLocation (currentLoc.Value.Line, value, currentLoc.Value.VisualCharXPosition);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);
			}
		}
		/// <summary>
		/// Set current cursor position in label.
		/// </summary>
		/// <param name="position">Absolute character position in text.</param>
		public void SetCursorPosition (int position) {
			CharLocation loc = lines.GetLocation (position);
			loc.Column = Math.Min (loc.Column, lines[loc.Line].Length);
			CurrentLoc = loc;
		}

		protected LineCollection lines;
		protected bool textMeasureIsUpToDate = false;
		protected object linesMutex = new object ();
		protected string LineBreak = null;
		protected Size cachedTextSize = default (Size);
		protected bool mixedLineBreak = false;

		protected FontExtents fe;
		protected TextExtents te;
		#endregion


		/// <summary>
		/// Background color for selected text inside this label.
		/// </summary>
		[DefaultValue ("SteelBlue")]
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

				CurrentLoc?.ResetVisualX ();
				selectionStart?.ResetVisualX ();

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

				int oldTextLength = string.IsNullOrEmpty (_text) ? 0 : _text.Length;
				lock (linesMutex) {					
					_text = value;
					getLines ();
					textMeasureIsUpToDate = false;
				}
				constraintsLocations();
				NotifyValueChanged ("Text", Text);
				OnTextChanged (this, new TextChangeEventArgs (new TextChange (0, oldTextLength, _text)));
				RegisterForGraphicUpdate ();
            }
        }

		void constraintsLocations () {
			if (selectionStart.HasValue) {
				CharLocation loc = CurrentLoc.Value;
				int l = Math.Min (loc.Line, lines.Count - 1);
				selectionStart = new CharLocation (l, Math.Min (loc.Column, lines[l].Length - 1));
			}
			if (CurrentLoc.HasValue) {
				CharLocation loc = CurrentLoc.Value;
				int l = Math.Min (loc.Line, lines.Count - 1);
				CurrentLoc = new CharLocation (l, Math.Min (loc.Column, lines[l].Length - 1));
			}
		}

		/// <summary>
		/// If 'true', linebreaks will be interpreted. If 'false', linebreaks are threated as unprintable
		/// unicode characters. Default value is 'False'.
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
				getLines ();
				constraintsLocations();
				NotifyValueChangedAuto (_multiline);
				RegisterForGraphicUpdate();
			}
		}
		

		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>		
		public bool MoveLeft(){
			//targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == 0) {
				if (loc.Line == 0)
					return false;
				CurrentLoc = new CharLocation (loc.Line - 1, lines[loc.Line - 1].Length);
            }else
				CurrentLoc = new CharLocation (loc.Line, loc.Column - 1);
			return true;
		}
		public bool MoveRight () {
			targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == lines[loc.Line].Length) {
				if (loc.Line == lines.Count - 1)
					return false;
				CurrentLoc = new CharLocation (loc.Line + 1, 0);
			} else
				CurrentLoc = new CharLocation (loc.Line, loc.Column + 1);
			return true;
		}		
		public bool LineMove (int lineDiff) {
			CharLocation loc = CurrentLoc.Value;
			int newLine = Math.Min (Math.Max (0, loc.Line + lineDiff), lines.Count - 1);

			if (newLine == loc.Line)
				return false;

			if (loc.Column > lines[newLine].Length) {
				if (targetColumn < 0)
					targetColumn = loc.Column;
				CurrentLoc = new CharLocation (newLine, lines[newLine].Length);
			} else if (targetColumn < 0)
				CurrentLoc = new CharLocation (newLine, loc.Column);
			else if (targetColumn > lines[newLine].Length)
				CurrentLoc = new CharLocation (newLine, lines[newLine].Length);
			else
				CurrentLoc = new CharLocation (newLine, targetColumn);

			return true;
        }
		protected int visibleLines => (int)((double)ClientRectangle.Height / (fe.Ascent + fe.Descent));
		public void GotoWordStart(){
			int pos = lines.GetAbsolutePosition (CurrentLoc.Value);			
			//skip white spaces
			while (pos > 0 && !char.IsLetterOrDigit (_text[pos-1]))
				pos--;
			while (pos > 0 && char.IsLetterOrDigit (_text[pos-1]))
				pos--;
			CurrentLoc = lines.GetLocation (pos);
		}
		public void GotoWordEnd(){
			int pos = lines.GetAbsolutePosition (CurrentLoc.Value);
			//skip white spaces
			while (pos < _text.Length -1 && !char.IsLetterOrDigit (_text[pos]))
				pos++;
			while (pos < _text.Length - 1 && char.IsLetterOrDigit (_text[pos]))
				pos++;
			CurrentLoc = lines.GetLocation (pos);
		}		

		protected void detectLineBreak () {
			if (!_multiline)
				return;			
			mixedLineBreak = false;

			if (lines.Count == 0 || lines[0].LineBreakLength == 0) {
				LineBreak = Environment.NewLine;
				return;
            }
			LineBreak = _text.GetLineBreak (lines[0]).ToString ();

			for (int i = 1; i < lines.Count; i++) {
				ReadOnlySpan<char> lb = _text.GetLineBreak (lines[i]);
				if (!lb.SequenceEqual (LineBreak)) {
					mixedLineBreak = true;
					break;
                }
			}
        }
		
		protected void getLines () {			
			if (lines == null)
				lines = new LineCollection (_multiline ? 4 : 1);
			else
				lines.Clear ();

			if (string.IsNullOrEmpty (_text))
				lines.Add (new TextLine (0, 0, 0));
			else if (!_multiline)
				lines.Add (new TextLine (0, _text.Length, _text.Length));
			else
				lines.Update (_text);
		}
		/// <summary>
		/// Current Selected text span.
		/// </summary>
		public TextSpan Selection {
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
				return new TextSpan (lines.GetAbsolutePosition (selStart), lines.GetAbsolutePosition (selEnd));
			}
        }
		public string SelectedText {
			get {
				TextSpan selection = Selection;
				return selection.IsEmpty ? "" : Text.AsSpan (selection.Start, selection.Length).ToString ();
			}
        }
		public bool SelectionIsEmpty => selectionStart.HasValue ? Selection.IsEmpty : true;

		protected virtual void measureTextBounds (Context gr) {
			fe = gr.FontExtents;
			te = new TextExtents ();

			cachedTextSize.Height = (int)Math.Ceiling ((fe.Ascent + fe.Descent) * Math.Max (1, lines.Count));

			TextExtents tmp = default;
			int longestLine = 0;
			for (int i = 0; i < lines.Count; i++) {
				if (lines[i].LengthInPixel < 0) {
					if (lines[i].Length == 0)
						lines.UpdateLineLengthInPixel (i, 0);// (int)Math.Ceiling (fe.MaxXAdvance);
					else {
						gr.TextExtents (_text.GetLine (lines[i]), Interface.TAB_SIZE, out tmp);
						lines.UpdateLineLengthInPixel (i, (int)Math.Ceiling (tmp.XAdvance));
					}
				}
				if (lines[i].LengthInPixel > lines[longestLine].LengthInPixel)
					longestLine = i;
			}
			cachedTextSize.Width = lines[longestLine].LengthInPixel;
			textMeasureIsUpToDate = true;
		}
		protected virtual void drawContent (Context gr) {
			Rectangle cb = ClientRectangle;
			fe = gr.FontExtents;
			double lineHeight = fe.Ascent + fe.Descent;

			CharLocation selStart = default, selEnd = default;
			bool selectionNotEmpty = false;

			if (HasFocus) {
				if (currentLoc?.Column < 0) {
					updateLocation (gr, cb.Width, ref currentLoc);
					NotifyValueChanged ("CurrentColumn", CurrentColumn);
				} else
					updateLocation (gr, cb.Width, ref currentLoc);
				if (selectionStart.HasValue) {
					updateLocation (gr, cb.Width, ref selectionStart);
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
				} else
					IFace.forceTextCursor = true;
			}

			if (!string.IsNullOrEmpty (_text)) {
				Foreground.SetAsSource (IFace, gr);

				TextExtents extents;
				Span<byte> bytes = stackalloc byte[128];
				double y = 0;

				for (int i = 0; i < lines.Count; i++) {
					if (!cancelLinePrint (lineHeight, y, cb.Height)) {
						int encodedBytes = -1;
						if (lines[i].Length > 0) {
							int size = lines[i].Length * 4 + 1;
							if (bytes.Length < size)
								bytes = size > 512 ? new byte[size] : stackalloc byte[size];

							encodedBytes = Crow.Text.Encoding.ToUtf8 (_text.GetLine (lines[i]), bytes);
							bytes[encodedBytes++] = 0;

							if (lines[i].LengthInPixel < 0) {
								gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
								lines.UpdateLineLengthInPixel (i, (int)extents.XAdvance);
							}
						}

						RectangleD lineRect = new RectangleD (
							(int)getX (cb.Width, lines[i]) + cb.X,
							y + cb.Top, lines[i].LengthInPixel, lineHeight);

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

						if (HasFocus && selectionNotEmpty) {
							RectangleD selRect = lineRect;
							if (_multiline) {
								if (i >= selStart.Line && i <= selEnd.Line) {
									if (selStart.Line == selEnd.Line) {
										selRect.X = selStart.VisualCharXPosition + cb.X;
										selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
									} else if (i == selStart.Line) {
										double newX = selStart.VisualCharXPosition + cb.X;
										selRect.Width -= (newX - selRect.X) - 10.0;
										selRect.X = newX;
									} else if (i == selEnd.Line) {
										selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
									} else
										selRect.Width += 10.0;
								} else {
									y += lineHeight;
									continue;
								}
							} else {
								selRect.X = selStart.VisualCharXPosition + cb.X;
								selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
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
					y += lineHeight;
				}
			}
		}
		protected virtual void updateHoverLocation (Point mouseLocalPos) {
			int hoverLine = _multiline ?
				(int)Math.Min (Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent + fe.Descent))), lines.Count - 1) : 0;
			hoverLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X);
		}
		protected virtual bool cancelLinePrint (double lineHeght, double y, int clientHeight) => false;
		RectangleD? textCursor = null;
		internal virtual RectangleD? computeTextCursor (Rectangle cursor) {
			Rectangle cb = ClientRectangle;
			if (cursor.X > cb.Width && cursor.Y > cb.Height)
				return null;
			return cursor;
		}
		public virtual bool DrawCursor (Context ctx, out Rectangle rect) {
			if (CurrentLoc == null || !SelectionIsEmpty) {
				rect = default;
				return false;
			}
			if (!CurrentLoc.Value.HasVisualX) {
				ctx.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				ctx.SetFontSize (Font.Size);
				ctx.FontOptions = Interface.FontRenderingOptions;
				ctx.Antialias = Interface.Antialias;
				lock (linesMutex) {					
					if (currentLoc?.Column < 0) {
						updateLocation (ctx, ClientRectangle.Width, ref currentLoc);
						NotifyValueChanged ("CurrentColumn", CurrentColumn);
					} else
						updateLocation (ctx, ClientRectangle.Width, ref currentLoc);
				}
				textCursor = null;
			}


			int lineHeight = (int)(fe.Ascent + fe.Descent);
			textCursor = computeTextCursor (new RectangleD (CurrentLoc.Value.VisualCharXPosition, CurrentLoc.Value.Line * lineHeight, 1.0, lineHeight));

			if (textCursor == null) {
				rect = default;
				return false;
			}
			//}
			Rectangle c = ScreenCoordinates (textCursor.Value + Slot.Position + ClientRectangle.Position);
			ctx.ResetClip ();
			Foreground.SetAsSource (IFace, ctx, c);			
			ctx.LineWidth = 1.0;
			ctx.MoveTo (0.5 + c.X, c.Y);
			ctx.LineTo (0.5 + c.X, c.Bottom);
			ctx.Stroke ();
			rect = c;
			return true;
		}

		protected void updateLocation (Context gr, int clientWidth, ref CharLocation? location) {
			if (location == null)
				return;
			CharLocation loc = location.Value;
			//Console.WriteLine ($"updateLocation: {loc} text:{_text.Length}");
			if (loc.HasVisualX)
				return;
			TextLine ls = lines[loc.Line];
			ReadOnlySpan<char> curLine = _text.GetLine (ls);
			double cPos = getX (clientWidth, ls);

			if (loc.Column >= 0) {
				//int encodedBytes = Crow.Text.Encoding2.ToUtf8 (curLine.Slice (0, loc.Column), bytes);
#if DEBUG
				if (loc.Column > curLine.Length) {
                    System.Diagnostics.Debug.WriteLine ($"loc.Column: {loc.Column} curLine.Length:{curLine.Length}");
					loc.Column = curLine.Length;
				}
#endif
				loc.VisualCharXPosition = gr.TextExtents (curLine.Slice (0, loc.Column), Interface.TAB_SIZE).XAdvance + cPos;
				location = loc;
			} else {
				TextExtents te;
				Span<byte> bytes = stackalloc byte[5];//utf8 single char buffer + '\0'

				for (int i = 0; i < ls.Length; i++) {
					int encodedBytes = Crow.Text.Encoding.ToUtf8 (curLine.Slice (i, 1), bytes);
					bytes[encodedBytes] = 0;

					gr.TextExtents (bytes, out te);
					double halfWidth = te.XAdvance / 2;

					if (loc.VisualCharXPosition <= cPos + halfWidth) {
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
		double getX (int clientWidth, TextLine ls) {
			switch (TextAlignment) {
			case TextAlignment.Right:
				return clientWidth - ls.LengthInPixel;
			case TextAlignment.Center:
				return clientWidth / 2 - ls.LengthInPixel / 2;
			}
			return 0;
		}
		protected void checkShift () {
			if (IFace.Shift) {
				if (!selectionStart.HasValue)
					selectionStart = CurrentLoc;
			} else
				selectionStart = null;
		}

		#region GraphicObject overrides
		public override bool UpdateLayout (LayoutingType layoutType) {
			if ((LayoutingType.Sizing | layoutType) != LayoutingType.None) {
				if (!System.Threading.Monitor.TryEnter (linesMutex))
					return false;
			}
			bool result = base.UpdateLayout (layoutType);
			System.Threading.Monitor.Exit (linesMutex);
			return result;
		}
		public override int measureRawSize(LayoutingType lt)
		{
			if ((bool)lines?.IsEmpty)
				getLines ();

			if (!textMeasureIsUpToDate) {
				using (Context gr = new Context (IFace.surf)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					gr.FontOptions = Interface.FontRenderingOptions;
					gr.Antialias = Interface.Antialias;

					measureTextBounds (gr);
				}
			}
			return Margin * 2 + (lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width);
		}
		
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;
			
			if (!textMeasureIsUpToDate) {
				lock (linesMutex)
					measureTextBounds (gr);
            }
			
			if (ClipToClientRect) {
				gr.Save ();
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			lock (linesMutex)
				drawContent (gr);
			
			if (ClipToClientRect)
				gr.Restore ();
		}
		#endregion

		#region Mouse handling
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);
			if (CurrentLoc == null) {
				selectionStart = new CharLocation (0, 0);				
				CurrentLoc = new CharLocation (lines.Count - 1, lines[lines.Count - 1].Length);
			}
			RegisterForRedraw ();
		}
		protected override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);
			RegisterForRedraw ();
		}
        public override void onMouseEnter (object sender, MouseMoveEventArgs e) {
            base.onMouseEnter (sender, e);
			if (Focusable)
				IFace.MouseCursor = MouseCursor.ibeam;			
		}
        public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			updateHoverLocation (ScreenPointToLocal (e.Position));

			if (HasFocus && IFace.IsDown (MouseButton.Left)) {
				CurrentLoc = hoverLoc;				
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
						selectionStart = CurrentLoc;
					CurrentLoc = hoverLoc;
					IFace.forceTextCursor = true;
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
				selectionStart = null;
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);
			if (e.Button != MouseButton.Left || !HasFocus)
				return;

			GotoWordStart ();
			selectionStart = CurrentLoc;			
			GotoWordEnd ();
			RegisterForRedraw ();
		}
		#endregion

		#region Keyboard handling
		public override void onKeyDown (object sender, KeyEventArgs e) {
			
			switch (e.Key) {
			case Key.Escape:
				selectionStart = null;
				RegisterForRedraw ();
				break;
			case Key.Home:
				targetColumn = -1;
				checkShift ();
				if (IFace.Ctrl)
					CurrentLoc = new CharLocation (0, 0);
				else
					CurrentLoc = new CharLocation (CurrentLoc.Value.Line, 0);
				RegisterForRedraw ();
				break;
			case Key.End:
				checkShift ();
				int l = IFace.Ctrl ? lines.Count - 1 : CurrentLoc.Value.Line;
				CurrentLoc = new CharLocation (l, lines[l].Length);
				RegisterForRedraw ();
				break;
			case Key.Insert:
				if (IFace.Ctrl && !SelectionIsEmpty)
					IFace.Clipboard = SelectedText;
				break;
			case Key.Left:
				checkShift ();
				if (IFace.Ctrl)
					GotoWordStart ();
				else
					MoveLeft ();
				RegisterForRedraw ();
				break;
			case Key.Right:
				checkShift ();
				if (IFace.Ctrl)
					GotoWordEnd ();
				else
					MoveRight ();
				RegisterForRedraw ();
				break;
			case Key.Up:
				checkShift ();
				LineMove (-1);
				RegisterForRedraw ();
				break;
			case Key.Down:
				checkShift ();
				LineMove (1);
				RegisterForRedraw ();
				break;
			default:
				base.onKeyDown (sender, e);
				return;
			}
			IFace.forceTextCursor = true;
			e.Handled = true;			
		}
		#endregion


	}
}
