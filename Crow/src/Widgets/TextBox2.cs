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
    public class TextBox2 : Label2, IEditableTextWidget
    {
		#region CTOR
		protected TextBox2 () {	}
		public TextBox2(Interface iface, string style = null) : base (iface, style) { }
        #endregion
	
		public Command CMDCut, CMDPaste;
		protected override void initCommands () {
			CMDCut = new ActionCommand ("Cut", Cut, "#icons.scissors.svg",  false);
			CMDCopy = new ActionCommand ("Copy", Copy, "#icons.copy-file.svg",  false);
			CMDPaste = new ActionCommand ("Paste", Paste, "#icons.paste-on-document.svg",  true);

			ContextCommands = new CommandGroup (CMDCut, CMDCopy, CMDPaste);
		}

        protected override CharLocation? CurrentLoc {
			get => base.CurrentLoc;
			set {
				base.CurrentLoc = value;
				CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
        public override int CurrentLine {
			get => base.CurrentLine;
			set {
				base.CurrentLine = value;
				CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
        public override int CurrentColumn {
			get => base.CurrentColumn;
			set {
				base.CurrentColumn = value;
				CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}


        public virtual bool DrawCursor (IContext ctx, out Rectangle rect) {
			if (CurrentLoc == null) {
				rect = default;
				return false;
			}
			CharLocation loc = currentLoc.Value;

			if (!loc.HasVisualX) {
				ctx.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				ctx.SetFontSize (Font.Size);

				if (currentLoc?.Column < 0) {
					updateLocation (ctx, ref loc);
					//update is locked, should not notify while updating graphics
					//NotifyValueChanged ("CurrentColumn", CurrentColumn);
				} else
					updateLocation (ctx, ref loc);
			}
			currentLoc = loc;

			RectangleD? textCursor = computeTextCursor (new RectangleD (loc.VisualCharXPosition, lineHeight * visualCurrentLine, 1.0, lineHeight));

			if (textCursor == null) {
				rect = default;
				return false;
			}

			Rectangle c = ContextCoordinates (textCursor.Value + Slot.Position + ClientRectangle.Position);
			Foreground.SetAsSource (IFace, ctx, c);
			ctx.LineWidth = 1.0;
			ctx.MoveTo (0.5 + c.X, c.Y);
			ctx.LineTo (0.5 + c.X, c.Bottom);
			ctx.Stroke ();
			rect = c;
			return true;
		}

        public override bool Paint(IContext ctx)
        {
            bool painted = base.Paint(ctx);
			if (HasFocus && painted && IFace.drawTextCursor) {
				DrawCursor(ctx, out Rectangle r);
			}
			return painted;
        }
		


		#region Keyboard handling
		public override void onKeyPress (object sender, KeyPressEventArgs e) {
			base.onKeyPress (sender, e);

			if (!e.Handled) {
				TextSpan selection = Selection;
				update (new TextChange (selection.Start, selection.Length, e.KeyChar.ToString ()));

				e.Handled = true;
			}
		}
		public override void onKeyDown (object sender, KeyEventArgs e) {
			Key key = e.Key;
			TextSpan selection = Selection;

			/*document.EnterReadLock();
			try {*/
				switch (key) {
				case Key.Backspace:
					if (selection.IsEmpty) {
						if (selection.Start == 0)
							return;
						if (CurrentLoc.Value.Column == 0) {
							int lbLength = buffer.GetLine (CurrentLoc.Value.Line - 1).LineBreakLength;
							update (new TextChange (selection.Start - lbLength, lbLength, ""));
						}else
							update (new TextChange (selection.Start - 1, 1, ""));
					} else
						update (new TextChange (selection.Start, selection.Length, ""));
					break;
				case Key.Delete:
					if (selection.IsEmpty) {
						if (selection.Start == buffer.Length)
							return;
						if (CurrentLoc.Value.Column >= buffer.GetLine (CurrentLoc.Value.Line).Length)
							update (new TextChange (selection.Start, buffer.GetLine (CurrentLoc.Value.Line).LineBreakLength, ""));
						else
							update (new TextChange (selection.Start, 1, ""));
					} else {
						if (e.Modifiers == Modifier.Shift)
							IFace.Clipboard = SelectedText;
						update (new TextChange (selection.Start, selection.Length, ""));
					}
					break;
				case Key.Insert:
					if (e.Modifiers.HasFlag (Modifier.Shift))
						Paste ();
					else if (e.Modifiers.HasFlag (Modifier.Control))
						Copy ();
					break;
				case Key.KeypadEnter:
				case Key.Enter:
					update (new TextChange (selection.Start, selection.Length, buffer.GetLineBreak ()));
					break;
				case Key.Tab:
					update (new TextChange (selection.Start, selection.Length, "\t"));
					break;
				default:
					base.onKeyDown (sender, e);
					return;
				}
				autoAdjustScroll = true;
				IFace.forceTextCursor();
				e.Handled = true;
			/*} finally {
				document.ExitReadLock ();
			}*/
		}
		#endregion

		#region textBox
		protected virtual RectangleD? computeTextCursor (Rectangle cursor) {
			Rectangle cb = ClientRectangle;
			cursor -= new Point (ScrollX, ScrollY);

			if (autoAdjustScroll) {
				autoAdjustScroll = false;
				int goodMsrs = 0;
				if (cursor.Left < 0)
					ScrollX += cursor.Left;
				else if (cursor.X > cb.Width)
					ScrollX += cursor.X - cb.Width + 5;
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

		public virtual void Paste () {
			TextSpan selection = Selection;
			update (new TextChange (selection.Start, selection.Length, IFace.Clipboard));
		}

		protected override void update (TextChange change) {
			buffer.Update(change);
			if (!disableTextChangedEvent)
				OnTextChanged (this, new TextChangeEventArgs (change));
			if (HasFocus) {
				SelectionStart = null;
				CharLocation newLoc = change.ChangedText == null ?
					buffer.GetLocation (change.Start)	: buffer.GetLocation (change.Start + change.ChangedText.Length);
				updateLocation(ref newLoc);//ensure tabulated column is uptodate on each changes
				CurrentLoc = newLoc;
			}
			textMeasureIsUpToDate = false;
			IFace.forceTextCursor();
			autoAdjustScroll = true;

			RegisterForGraphicUpdate ();
		}

		#endregion
	}
}
