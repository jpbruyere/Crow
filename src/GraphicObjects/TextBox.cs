//
//  TextBox.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using Cairo;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Crow
{
    public class TextBox : Label
    {
		#region CTOR
		public TextBox()
		{ }
		public TextBox(string _initialValue)
			: base(_initialValue)
		{

		}
		#endregion

		#region GraphicObject overrides
		[XmlIgnore]public override bool HasFocus   //trigger update when lost focus to errase text beam
        {
            get
            {
                return base.HasFocus;
            }
            set
            {
                base.HasFocus = value;
                RegisterForRedraw();
            }
        }

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);
			FontExtents fe = gr.FontExtents;
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
				if (Multiline)
					this.InsertLineBreak ();
				else
					OnTextChanged(this,new TextChangeEventArgs(Text));
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
				break;
			case Key.PageUp:
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
			SelBegin = new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();
		}
        #endregion
	} 
}
