// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;
using Crow.Text;
using Glfw;
using System;
using System.ComponentModel;

namespace Crow
{
	public class TextBox : Label
	{
		#region CTOR
		protected TextBox () { }
		public TextBox (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		/// <summary>
		/// Validate content of the text box. Occurs in non multiline TextBox when 'Enter' key
		/// is pressed.
		/// </summary>
		public event EventHandler<ValidateEventArgs> Validate;
		public virtual void OnValidate (Object sender, ValidateEventArgs e) {
			Validate.Raise (this, e);
		}

		#region Scrolling
		int scrollX, scrollY, maxScrollX, maxScrollY, mouseWheelSpeed;

		/// <summary>
		/// if true, key stroke are handled in derrived class
		/// </summary>
		protected bool KeyEventsOverrides = false;
		bool autoAdjustScroll = false;//if scrollXY is changed directly, dont try adjust scroll to cursor
		/// <summary> Horizontal Scrolling Position </summary>
		[DefaultValue (0)]
		public virtual int ScrollX {
			get { return scrollX; }
			set {
				//cancelAdjustScroll = true;

				if (scrollX == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollX)
					newS = maxScrollX;

				if (newS == scrollX)
					return;

				scrollX = newS;

				NotifyValueChangedAuto (scrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling Position </summary>
		[DefaultValue (0)]
		public virtual int ScrollY {
			get { return scrollY; }
			set {
				//cancelAdjustScroll = true;

				if (scrollY == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollY)
					newS = maxScrollY;

				if (newS == scrollY)
					return;

				scrollY = newS;

				NotifyValueChangedAuto (scrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Horizontal Scrolling maximum value </summary>
		[DefaultValue (0)]
		public virtual int MaxScrollX {
			get { return maxScrollX; }
			set {
				if (maxScrollX == value)
					return;

				maxScrollX = Math.Max (0, value);

				if (scrollX > maxScrollX)
					ScrollX = maxScrollX;

				NotifyValueChangedAuto (maxScrollX);
				//RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling maximum value </summary>
		[DefaultValue (0)]
		public virtual int MaxScrollY {
			get { return maxScrollY; }
			set {
				if (maxScrollY == value)
					return;

				maxScrollY = Math.Max (0, value);

				if (scrollY > maxScrollY)
					ScrollY = maxScrollY;

				NotifyValueChangedAuto (maxScrollY);
				//RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Mouse Wheel Scrolling multiplier </summary>
		[DefaultValue (20)]
		public virtual int MouseWheelSpeed {
			get { return mouseWheelSpeed; }
			set {
				if (mouseWheelSpeed == value)
					return;

				mouseWheelSpeed = value;

				NotifyValueChangedAuto (mouseWheelSpeed);
			}
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e) {			
			e.Handled = true;
			if (IFace.Shift)
				ScrollX += e.Delta * MouseWheelSpeed;
			else if (Multiline)
				ScrollY -= e.Delta * MouseWheelSpeed;
			else
				e.Handled = false;
			
			base.onMouseWheel (sender, e);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e) {            
			if (!HasFocus || !IFace.IsDown (MouseButton.Left)) {
				base.onMouseMove (sender, e);
				return;
			}
			Rectangle cb = ClientRectangle;

			if (CurrentLoc.Value.VisualCharXPosition < scrollX)
				ScrollX = (int)CurrentLoc.Value.VisualCharXPosition;
			else if (CurrentLoc.Value.VisualCharXPosition > cb.Width + scrollX)
				ScrollX = (int)CurrentLoc.Value.VisualCharXPosition - cb.Width;

			double lineHeight = fe.Ascent + fe.Descent;
			int firstLine = (int)Math.Ceiling((double)scrollY / lineHeight);
			int lastLine = (int)Math.Floor((double)(scrollY + cb.Height) / lineHeight) - 1;
			//Console.WriteLine ($"current: {currentLoc.Value.Line} first:{firstLine} last:{lastLine}");

			if (CurrentLoc.Value.Line < firstLine)
				ScrollY = (int)(lineHeight * CurrentLoc.Value.Line);
			else if (CurrentLoc.Value.Line > lastLine)
				ScrollY = (int)(lineHeight * (CurrentLoc.Value.Line + 1)) - cb.Height;

			e.Handled = true;
			base.onMouseMove (sender, e);
		}
		#endregion
		public override void OnLayoutChanges (LayoutingType layoutType) {
			base.OnLayoutChanges (layoutType);
			updateMaxScrolls (layoutType);
		}
		protected override void drawContent (Context gr) {
			gr.Translate (-scrollX, -scrollY);
			base.drawContent (gr);
			gr.Translate (scrollX, scrollY);
		}
		protected override bool cancelLinePrint (double lineHeght, double y, int clientHeight) =>
			y + lineHeght < scrollY || y - lineHeght > clientHeight + scrollY;
		protected override void updateHoverLocation (Point mouseLocalPos) {
			base.updateHoverLocation (mouseLocalPos + new Point (ScrollX, ScrollY));
		}
		protected override void measureTextBounds (Context gr) {
			base.measureTextBounds (gr);
			updateMaxScrolls (LayoutingType.Height);
			updateMaxScrolls (LayoutingType.Width);
		}
		internal override RectangleD? computeTextCursor (Rectangle cursor) {
			Rectangle cb = ClientRectangle;
			cursor -= new Point (scrollX, scrollY);

			if (autoAdjustScroll) {
				autoAdjustScroll = false;
				int goodMsrs = 0;
				if (cursor.Right < 0)
					ScrollX += cursor.Right;
				else if (cursor.X > cb.Width)
					ScrollX += cursor.X - cb.Width;
				else
					goodMsrs++;

				if (cursor.Y < 0)
					ScrollY += cursor.Y;
				else if (cursor.Bottom > cb.Height)
					ScrollY += cursor.Bottom - cb.Height;
				else
					goodMsrs++;

				if (goodMsrs < 2)
					return null;
			} else if (cursor.Right < 0 || cursor.X > cb.Width || cursor.Y < 0 || cursor.Bottom > cb.Height)
				return null;
			
			return cursor;            
		}

		void updateMaxScrolls (LayoutingType layout) {
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
		public virtual void Paste () {
			TextSpan selection = Selection;
			update (new TextChange (selection.Start, selection.Length, IFace.Clipboard));
		}

		#region Keyboard handling
		public override void onKeyDown (object sender, KeyEventArgs e) {
			Key key = e.Key;
			TextSpan selection = Selection;
			switch (key) {
			case Key.Backspace:
				if (selection.IsEmpty) {
					if (selection.Start == 0)
						return;
					if (CurrentLoc.Value.Column == 0) {
						int lbLength = lines[CurrentLoc.Value.Line - 1].LineBreakLength;
						update (new TextChange (selection.Start - lbLength, lbLength, ""));
					}else
						update (new TextChange (selection.Start - 1, 1, ""));
				} else
					update (new TextChange (selection.Start, selection.Length, ""));
				break;
			case Key.Delete:
				if (selection.IsEmpty) {
					if (selection.Start == Text.Length)
						return;
					if (CurrentLoc.Value.Column >= lines[CurrentLoc.Value.Line].Length) 
						update (new TextChange (selection.Start, lines[CurrentLoc.Value.Line].LineBreakLength, ""));                        
					else
						update (new TextChange (selection.Start, 1, ""));
				} else {
					if (IFace.Shift)
						IFace.Clipboard = SelectedText;
					update (new TextChange (selection.Start, selection.Length, ""));
				}
				break;
			case Key.Insert:
				if (IFace.Shift)
					Paste ();
				else if (IFace.Ctrl)
					Copy ();
				break;
			case Key.KeypadEnter:
			case Key.Enter:
				if (Multiline) {
					if (string.IsNullOrEmpty (LineBreak))
						detectLineBreak ();
					update (new TextChange (selection.Start, selection.Length, LineBreak));
				} else
					OnValidate (this, new ValidateEventArgs (_text));
				break;
			case Key.Escape:
				selectionStart = null;
				CurrentLoc = lines.GetLocation (selection.Start);
				RegisterForRedraw ();
				break;
			case Key.Tab:
				update (new TextChange (selection.Start, selection.Length, "\t"));
				break;
			case Key.PageUp:
				checkShift ();
				LineMove (-visibleLines);
				RegisterForRedraw ();
				break;
			case Key.PageDown:
				checkShift ();
				LineMove (visibleLines);
				RegisterForRedraw ();
				break;
			default:
				base.onKeyDown (sender, e);
				break;
			}
			autoAdjustScroll = true;
			e.Handled = true;
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e) {
			base.onKeyPress (sender, e);

			TextSpan selection = Selection;
			update (new TextChange (selection.Start, selection.Length, e.KeyChar.ToString ()));

			/*Insert (e.KeyChar.ToString());

			SelRelease = -1;
			SelBegin = new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();*/
		}
		#endregion

		protected void update (TextChange change) {
			lock (linesMutex) {
				ReadOnlySpan<char> src = Text.AsSpan ();
				Span<char> tmp = stackalloc char[src.Length + (change.ChangedText.Length - change.Length)];
				//Console.WriteLine ($"{Text.Length,-4} {change.Start,-4} {change.Length,-4} {change.ChangedText.Length,-4} tmp:{tmp.Length,-4}");
				src.Slice (0, change.Start).CopyTo (tmp);
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
				src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));
			
				_text = tmp.ToString ();
				lines.Update (change);
				//lines.Update (_text);
				selectionStart = null;

				CurrentLoc = lines.GetLocation (change.Start + change.ChangedText.Length);
				textMeasureIsUpToDate = false;
				IFace.forceTextCursor = true;
			}

			NotifyValueChanged ("Text", Text);
			OnTextChanged (this, new TextChangeEventArgs (change));
			
			RegisterForGraphicUpdate ();
		}
		protected override string LogName => "tb";
	}
}
