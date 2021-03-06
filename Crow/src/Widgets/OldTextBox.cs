﻿// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;
using Glfw;
using System;

namespace Crow
{
	[Obsolete]
	public class OldTextBox : OldLabel
    {
		#region CTOR
		protected OldTextBox() {}
		public OldTextBox(Interface iface, string style = null) : base (iface, style) { }
		#endregion

		#region GraphicObject overrides
		[XmlIgnore]public override bool HasFocus   //trigger update when lost focus to errase text beam
        {
            get => base.HasFocus;
            set {
				if (base.HasFocus == value)
					return;
                base.HasFocus = value;
                RegisterForRedraw();
            }
        }

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);
		}
		#endregion
			
        #region Keyboard handling
		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			Key key = e.Key;

			switch (key)
			{
			case Key.Backspace:
				if (CurrentPosition == 0)
					return;
				DeleteChar();
				break;
			case Key.Delete:
				if (selectionIsEmpty) {
					if (!MoveRight ())
						return;
				}else if (IFace.Shift)
					IFace.Clipboard = SelectedText;
				DeleteChar ();
				break;
			case Key.KeypadEnter:
			case Key.Enter:
				if (!selectionIsEmpty)
					DeleteChar ();
				if (Multiline)
					InsertLineBreak ();
				else
					OnTextChanged(this,new TextChangeEventArgs(default));
				break;
			case Key.Escape:
				Text = "";
				CurrentColumn = 0;
				SelRelease = -1;
				break;
			case Key.Home:
				if (IFace.Shift) {
					if (selectionIsEmpty)
						SelBegin = new Point (CurrentColumn, CurrentLine);
					if (IFace.Ctrl)
						CurrentLine = 0;
					CurrentColumn = 0;
					SelRelease = new Point (CurrentColumn, CurrentLine);
					break;
				}
				SelRelease = -1;
				if (IFace.Ctrl)
					CurrentLine = 0;
				CurrentColumn = 0;
				break;
			case Key.End:
				if (IFace.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (IFace.Ctrl)
						CurrentLine = int.MaxValue;
					CurrentColumn = int.MaxValue;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (IFace.Ctrl)
					CurrentLine = int.MaxValue;
				CurrentColumn = int.MaxValue;
				break;
			case Key.Insert:
				if (IFace.Shift)
					this.Insert (IFace.Clipboard);
				else if (IFace.Ctrl && !selectionIsEmpty)
					IFace.Clipboard = this.SelectedText;
				break;
			case Key.Left:
				if (IFace.Shift) {
					if (selectionIsEmpty)
						SelBegin = new Point(CurrentColumn, CurrentLine);
					if (IFace.Ctrl)
						GotoWordStart ();
					else if (!MoveLeft ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (IFace.Ctrl)
					GotoWordStart ();
				else
					MoveLeft();
				break;
			case Key.Right:
				if (IFace.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (IFace.Ctrl)
						GotoWordEnd ();
					else if (!MoveRight ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				if (IFace.Ctrl)
					GotoWordEnd ();
				else
					MoveRight ();
				break;
			case Key.Up:
				if (IFace.Shift) {
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
				if (IFace.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine++;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				CurrentLine++;				
				break;
			case Key.Tab:
				this.Insert ("\t");
				break;
			default:
				break;
			}
			e.Handled = true;
			base.onKeyDown (sender, e);
			RegisterForGraphicUpdate ();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			this.Insert (e.KeyChar.ToString());

			SelRelease = -1;
			SelBegin = new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();
		}
        #endregion
	} 
}
