//
// TextBox.cs
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
using Cairo;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Crow
{
    public class TextBox : Label
    {
		#region CTOR
		protected TextBox() : base(){}
		public TextBox(Interface iface) : base(iface)
		{ }
//		public TextBox(string _initialValue)
//			: base(_initialValue)
//		{
//
//		}
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
		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			Key key = e.Key;

			switch (key)
			{
			case Key.BackSpace:
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
				}else if (IFace.Shift)
					IFace.Clipboard = this.SelectedText;
				this.DeleteChar ();
				break;
			case Key.KP_Enter:
			case Key.Return:
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
			case Key.Menu:
				break;
			case Key.Num_Lock:
				break;
			case Key.Page_Down:				
				break;
			case Key.Page_Up:
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
