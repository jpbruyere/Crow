// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Cairo;
using Crow.Text;
using Glfw;
using System;

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


        #region Keyboard handling
        public override void onKeyDown (object sender, KeyEventArgs e) {
            Key key = e.Key;
            TextSpan selection = Selection;
            switch (key) {
            case Key.Backspace:
                if (selection.IsEmpty) {
                    if (selection.Start == 0)
                        return;
                    if (currentLoc.Value.Column == 0) {
                        int lbLength = lines[currentLoc.Value.Line - 1].LineBreakLength;
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
                    if (currentLoc.Value.Column >= lines[currentLoc.Value.Line].Length) 
                        update (new TextChange (selection.Start, lines[currentLoc.Value.Line].LineBreakLength, ""));                        
                    else
                        update (new TextChange (selection.Start, 1, ""));
                } else {
                    if (IFace.Shift)
                        IFace.Clipboard = Text.AsSpan (selection.Start, selection.End).ToString ();
                    update (new TextChange (selection.Start, selection.Length, ""));
                }
                break;
            case Key.Insert:
                if (IFace.Shift)
                    update (new TextChange (selection.Start, selection.Length, IFace.Clipboard));
                else if (IFace.Ctrl && !selection.IsEmpty)
                    IFace.Clipboard = Text.AsSpan (selection.Start, selection.End).ToString ();
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
                currentLoc = lines.GetLocation (selection.Start);
                RegisterForRedraw ();
                break;
            case Key.Tab:
                update (new TextChange (selection.Start, selection.Length, "\t"));
                break;
            default:
                base.onKeyDown (sender, e);
                break;
            }
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

        void update (TextChange change) {
            Span<char> tmp = stackalloc char[Text.Length + (change.ChangedText.Length - change.Length)];
            ReadOnlySpan<char> src = Text.AsSpan ();
            src.Slice (0, change.Start).CopyTo (tmp);
            change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
            src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));

            _text = tmp.ToString ();

            getLines ();
            selectionStart = null;
            currentLoc = lines.GetLocation (change.Start + change.ChangedText.Length);

            textMeasureIsUpToDate = false;
            NotifyValueChanged ("Text", Text);
            OnTextChanged (this, new TextChangeEventArgs (change));

            RegisterForGraphicUpdate ();
        }
    }
}
