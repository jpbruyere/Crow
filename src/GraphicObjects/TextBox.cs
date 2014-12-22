using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using Cairo;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Globalization;
using System.ComponentModel;

namespace go
{
    public class TextBoxWidget : Label
    {
		#region CTOR
		public TextBoxWidget(string _initialValue, GOEvent _onTextChanged = null)
			: base(_initialValue)
		{

		}

		public TextBoxWidget()
		{ }
		#endregion

		static bool _capitalOn = false;	//????????????????
		[XmlIgnore]public static bool capitalOn
		{
			get
			{
				return _capitalOn;
					//Keyboard[Key.ShiftLeft] || Keyboard[Key.ShiftRight] ?
					//!_capitalOn : _capitalOn;
			}
			set { _capitalOn = value; }
		}

		#region private fields
        Color selColor;
        Color selFontColor;
        Point mouseLocalPos;    //mouse coord in widget space, filled only when clicked        
        int _currentPos;        //0 based cursor position in string
        double textCursorPos;   //cursor position in cairo units in widget client coord.
        double SelStartCursorPos = -1;
        double SelEndCursorPos = -1;
        bool SelectionInProgress = false;
		#endregion

		[XmlAttributeAttribute()][DefaultValue("SkyBlue")]
		public virtual Color SelectionBackground {
			get { return selColor; }
			set {
				selColor = value;
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("Black")]
		public virtual Color SelectionForeground {
			get { return selFontColor; }
			set {
				selFontColor = value;
				registerForGraphicUpdate ();
			}
		}

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
                registerForGraphicUpdate();
            }
        }
		[XmlAttributeAttribute()][DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Color Background {
			get { return base.Background; }
			set { base.Background = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("Black")]
		public virtual Color Foreground {
			get { return base.Foreground; }
			set { base.Foreground = value; }
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);
			//			gr.FontOptions.Antialias = Antialias.Subpixel;
			//			gr.FontOptions.HintMetrics = HintMetrics.On;
			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			FontExtents fe = gr.FontExtents;

			#region draw text cursor
			if (mouseLocalPos > 0)
			{
				computeTextCursor(gr);

				if (SelectionInProgress)
				{
					selReleasePos = currentPos;
					SelEndCursorPos = textCursorPos;
				}
				else if (selBeginPos < 0)
				{
					selBeginPos = currentPos;
					SelStartCursorPos = textCursorPos;
					selReleasePos = -1;
				}
				else
					computeTextCursorPosition(gr);

			}
			else
				computeTextCursorPosition(gr);

			if (HasFocus)
			{
				//TODO:
				gr.Color = Foreground;
				gr.LineWidth = 2;
				gr.MoveTo(new PointD(textCursorPos + rText.X, rText.Y ));
				gr.LineTo(new PointD(textCursorPos + rText.X, rText.Y + fe.Height));
				gr.Stroke();
			}

			#endregion

			if (selReleasePos >= 0)
			{
				gr.Color = selColor;
				Rectangle selRect =
					new Rectangle((int)SelStartCursorPos + rText.X, (int)(rText.Y), (int)(SelEndCursorPos - SelStartCursorPos), (int)fe.Height);
				gr.Rectangle(selRect);
				gr.Fill();

				gr.Color = selFontColor;
			}
			else
				gr.Color = Foreground;

			gr.MoveTo(rText.X, rText.Y + fe.Ascent);
			#if _WIN32 || _WIN64
			gr.ShowText(txt);
			#elif __linux__
			gr.ShowText(Text);
			#endif
			gr.Fill();

		}
		#endregion

		public event EventHandler<TextChangeEventArgs> TextChanged = delegate { };

		public virtual void onTextChanged(Object sender, TextChangeEventArgs e)
		{
			TextChanged (this, e);
		}


        [XmlIgnore]public int currentPos{
            get { return _currentPos; }
            set { _currentPos = value; }
        }
        [XmlIgnore]public int selBeginPos;
        [XmlIgnore]public int selReleasePos;
        [XmlIgnore]public int selectionStart   //ordered selection start and end positions
        {
            get { return selReleasePos < 0 ? selBeginPos : Math.Min(selBeginPos, selReleasePos); }
        }
        [XmlIgnore]public int selectionEnd
        { get { return selReleasePos < 0 ? selReleasePos : Math.Max(selBeginPos, selReleasePos); } }
        [XmlIgnore]public string selectedText
        { get { return selectionEnd < 0 ? null : Text.Substring(selectionStart, selectionEnd - selectionStart); } }
        [XmlIgnore]public bool selectionIsEmpty
        { get { return string.IsNullOrEmpty(selectedText); } }


			
        #region Keyboard handling
		public override void onKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			Key key = e.Key;

			switch (key)
			{
			case Key.Back:
				if (!selectionIsEmpty)
				{
					Text = Text.Remove(selectionStart, selectionEnd - selectionStart);
					selReleasePos = -1;
					currentPos = selBeginPos;
				}
				else if (currentPos > 0)
				{
					currentPos--;
					Text = Text.Remove(currentPos, 1);
				}
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (!selectionIsEmpty)
				{
					Text = Text.Remove(selectionStart, selectionEnd - selectionStart);
					selReleasePos = -1;
					currentPos = selBeginPos;
				}
				else if (currentPos < Text.Length)
					Text = Text.Remove(currentPos, 1);
				break;
			case Key.Down:
				break;
			case Key.End:
				currentPos = Text.Length;
				break;
			case Key.Enter:
			case Key.KeypadEnter:
				onTextChanged(this,new TextChangeEventArgs(Text));
				break;
			case Key.Escape:
				Text = "";
				currentPos = 0;
				selReleasePos = -1;
				break;
			case Key.Home:
				currentPos = 0;
				break;
			case Key.Insert:
				break;
			case Key.Left:
				if (currentPos > 0)
					currentPos--;
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
			case Key.Right:
				if (currentPos < Text.Length)
					currentPos++;
				break;
			case Key.Tab:
				Text = Text.Insert(currentPos, "\t");
				currentPos++;
				break;
			case Key.Up:
				break;
			case Key.KeypadDecimal:
				Text = Text.Insert(currentPos, new string(new char[] 
					{ Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) }));
				currentPos++;
				break;
			case Key.Space:
				Text = Text.Insert(currentPos, " ");
				currentPos++;
				break;
			case Key.KeypadDivide:
			case Key.Slash:
				Text = Text.Insert(currentPos, "/");
				currentPos++;
				break;
			case Key.KeypadMultiply:
				Text = Text.Insert(currentPos, "*");
				currentPos++;
				break;
			case Key.KeypadMinus:
			case Key.Minus:
				Text = Text.Insert(currentPos, "-");
				currentPos++;
				break;
			case Key.KeypadPlus:
			case Key.Plus:
				Text = Text.Insert(currentPos, "+");
				currentPos++;
				break;
			default:
				if (!selectionIsEmpty)
				{
					Text = Text.Remove(selectionStart, selectionEnd - selectionStart);
					currentPos = selBeginPos;
				}

				string k = "?";
				if ((char)key >= 67 && (char)key <= 76)
					k = ((int)key - 67).ToString();
				else if ((char)key >= 109 && (char)key <= 118)
					k = ((int)key - 109).ToString();
				else if (capitalOn)
					k = key.ToString();
				else
					k = key.ToString().ToLower();

				Text = Text.Insert(currentPos, k);
				currentPos++;

				selReleasePos = -1;
				selBeginPos = currentPos;

				break;
			}
			registerForGraphicUpdate();
		}
        #endregion

        #region Mouse handling
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
//			SelectionInProgress = true;                
//			mouseLocalPos = e.Position - ScreenCoordBounds.TopLeft - rText.TopLeft;
//			registerForGraphicUpdate();

			base.onMouseEnter (sender, e);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if ((sender as OpenTKGameWindow).activeWidget != this)
				return;

			SelectionInProgress = true;
			mouseLocalPos = e.Position - ScreenCoordinates(rText).TopLeft;
			registerForGraphicUpdate();
		
		}
		public override void onMouseButtonDown (object sender, MouseButtonEventArgs e)
		{
            if (this.HasFocus){
				mouseLocalPos = e.Position - ScreenCoordinates(rText).TopLeft - rText.TopLeft;
				selBeginPos = -1;
				selReleasePos = -1;
			}else{
				selBeginPos = 0;
				selReleasePos = Text.Length;
			}            

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseButtonDown (sender, e);

            registerForGraphicUpdate();
		}
		public override void onMouseButtonUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseButtonUp (sender, e);
			SelectionInProgress = false;
		}
        #endregion

        void computeTextCursor(Context gr)
        {
            FontExtents fe = gr.FontExtents;
            TextExtents te;

            double cPos = 0f;
            for (int i = 0; i < _text.Length; i++)
            {
#if _WIN32 || _WIN64
                byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(Text.Substring(i, 1));
                te = gr.TextExtents(c);
#elif __linux__
                te = gr.TextExtents(Text.Substring(i, 1));
#endif
                double halfWidth = te.XAdvance / 2;

                if (mouseLocalPos.X <= cPos + halfWidth)
                {
                    currentPos = i;
                    textCursorPos = cPos;
                    mouseLocalPos = -1;
                    return;
                }

                cPos += te.XAdvance;
            }
            currentPos = _text.Length;
            textCursorPos = cPos;

            //reset mouseLocalPos
            mouseLocalPos = -1;
        }
        void computeTextCursorPosition(Context gr)
        {
            FontExtents fe = gr.FontExtents;
            TextExtents te;

            double cPos = 0f;

            int limit = currentPos;

            if (selectionEnd > 0)
                limit = Math.Max(currentPos, selectionEnd);

            for (int i = 0; i <= limit; i++)
            {
                if (i == currentPos)
                    textCursorPos = cPos;
                if (i == selectionStart)
                    SelStartCursorPos = cPos;
                if (i == selectionEnd)
                    SelEndCursorPos = cPos;

                if (i < Text.Length)
                {
#if _WIN32 || _WIN64
                    byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(Text.Substring(i, 1));
                    te = gr.TextExtents(c);
#elif __linux__
					te = gr.TextExtents(Text.Substring(i,1));
#endif
                    cPos += te.XAdvance;
                }
            }

        }			
	} 
}
