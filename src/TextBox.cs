using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using Cairo;
using System.Diagnostics;

namespace go
{
    public class TextBoxWidget : Label
    {
        public TextBoxWidget(string _initialValue, WidgetEvent _onTextChanged = null)
            : base(_initialValue)
        {
            onTextChanged = _onTextChanged;
            focusable = true;
            selColor = Color.SkyBlue;
            selFontColor = Color.Black;
            fontColor = Color.Black;
            background = Color.White;
            textAlignment = Alignment.LeftCenter;
        }

        //trigger update when lost focus to errase text beam
        public override bool hasFocus
        {
            get
            {
                return base.hasFocus;
            }
            set
            {
                base.hasFocus = value;
                registerForGraphicUpdate();
            }
        }
        public WidgetEvent onTextChanged;

        Color selColor;
        Color selFontColor;

        //mouse coord in widget space, filled only when clicked
        Point mouseLocalPos;

        //0 based cursor position in string
        private int _currentPos;
        public int currentPos
        {
            get { return _currentPos; }
            set { _currentPos = value; }
        }
        public int selStartPos;
        public int selEndPos;
        //ordered selection start and end positions
        public int selectionStart
        {
            get { return selEndPos < 0 ? selStartPos : Math.Min(selStartPos, selEndPos); }
        }
        public int selectionEnd
        { get { return selEndPos < 0 ? selEndPos : Math.Max(selStartPos, selEndPos); } }


        //cursor position in cairo units in widget client coord.
        double textCursorPos;
        double SelStartCursorPos = -1;
        double SelEndCursorPos = -1;

        bool SelectionInProgress = false;

        public string selectedText
        { get { return selectionEnd < 0 ? null : text.Substring(selectionStart, selectionEnd - selectionStart); } }
        public bool selectionIsEmpty
        { get { return string.IsNullOrEmpty(selectedText); } }

        #region Keyboard handling
        public override void ProcessKeyboard(Key key)
        {
            switch (key)
            {
                case Key.Back:
                    if (!selectionIsEmpty)
                    {
                        text = text.Remove(selectionStart, selectionEnd - selectionStart);
                        selEndPos = -1;
                        currentPos = selStartPos;
                    }
                    else if (currentPos > 0)
                    {
                        currentPos--;
                        text = text.Remove(currentPos, 1);
                    }
                    break;
                case Key.Clear:
                    break;
                case Key.Delete:
                    if (!selectionIsEmpty)
                    {
                        text = text.Remove(selectionStart, selectionEnd - selectionStart);
                        selEndPos = -1;
                        currentPos = selStartPos;
                    }
                    else if (currentPos < text.Length)
                        text = text.Remove(currentPos, 1);
                    break;
                case Key.Down:
                    break;
                case Key.End:
                    currentPos = text.Length;
                    break;
                case Key.Enter:
                case Key.KeypadEnter:
                    if (onTextChanged != null)
                        onTextChanged(this);
                    break;
                case Key.Escape:
                    text = "";
                    currentPos = 0;
                    selEndPos = -1;
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
                    if (currentPos < text.Length)
                        currentPos++;
                    break;
                case Key.Tab:
                    text = text.Insert(currentPos, "\t");
                    currentPos++;
                    break;
                case Key.Up:
                    break;
                case Key.KeypadDecimal:
                    text = text.Insert(currentPos, new string(new char[] { Interface.decimalSeparator }));
                    currentPos++;
                    break;
                case Key.Space:
                    text = text.Insert(currentPos, " ");
                    currentPos++;
                    break;
                case Key.KeypadDivide:
                case Key.Slash:
                    text = text.Insert(currentPos, "/");
                    currentPos++;
                    break;
                case Key.KeypadMultiply:
                    text = text.Insert(currentPos, "*");
                    currentPos++;
                    break;
                case Key.KeypadMinus:
                case Key.Minus:
                    text = text.Insert(currentPos, "-");
                    currentPos++;
                    break;
                case Key.KeypadPlus:
                case Key.Plus:
                    text = text.Insert(currentPos, "+");
                    currentPos++;
                    break;
                default:
                    if (!selectionIsEmpty)
                    {
                        text = text.Remove(selectionStart, selectionEnd - selectionStart);
                        currentPos = selStartPos;
                    }

                    string k = "?";
                    if ((char)key >= 67 && (char)key <= 76)
                        k = ((int)key - 67).ToString();
                    else if ((char)key >= 109 && (char)key <= 118)
                        k = ((int)key - 109).ToString();
                    else if (Interface.capitalOn)
                        k = key.ToString();
                    else
                        k = key.ToString().ToLower();

                    text = text.Insert(currentPos, k);
                    currentPos++;

                    selEndPos = -1;
                    selStartPos = currentPos;

                    break;
            }
            registerForGraphicUpdate();

        }
        #endregion

        #region Mouse handling
        public override bool ProcessMousePosition(Point mousePos)
        {
            Rectangle scb = ScreenCoordBounds;

            if (scb.Contains(mousePos))
            {
                Interface.hoverWidget = this;

                //Interface.SetCursor(System.Windows.Forms.Cursors.IBeam.Handle);

                if (Interface.Mouse[MouseButton.Left])
                {
                    SelectionInProgress = true;
                    Debug.WriteLine("selection in progress");
                    mouseLocalPos = mousePos - ScreenCoordBounds.TopLeft - rText.TopLeft;
                    registerForGraphicUpdate();
                }

                return true;
            }
            else
            {
                Interface.hoverWidget = null;
                return false;
            }
        }
        public override void ProcessMouseDown(Point mousePos)
        {
            Interface.activeWidget = this;

            if (!this.hasFocus)
            {
                selStartPos = 0;
                selEndPos = text.Length;
            }
            else
            {
                mouseLocalPos = mousePos - ScreenCoordBounds.TopLeft - rText.TopLeft;
                selStartPos = -1;
                selEndPos = -1;
            }
            base.ProcessMouseDown(mousePos);

            registerForGraphicUpdate();
        }
        public override void ProcessMouseUp(Point mousePos)
        {
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
                byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(text.Substring(i, 1));
                te = gr.TextExtents(c);
#elif __linux__
                te = gr.TextExtents(text.Substring(i, 1));
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

                if (i < text.Length)
                {
#if _WIN32 || _WIN64
                    byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(text.Substring(i, 1));
                    te = gr.TextExtents(c);
#elif __linux__
					te = gr.TextExtents(text.Substring(i,1));
#endif
                    cPos += te.XAdvance;
                }
            }

        }

        internal override void updateGraphic()
        {
            byte[] txt = utf8Text;
            int stride = 4 * renderBounds.Width;


            //init  bmp with widget background and border
            base.updateGraphic();

            using (ImageSurface bitmap =
                new ImageSurface(bmp, Format.Argb32, renderBounds.Width, renderBounds.Height, stride))
            {
                using (Context gr = new Context(bitmap))
                {
                    gr.FontOptions.Antialias = Antialias.Subpixel;
                    gr.FontOptions.HintMetrics = HintMetrics.On;
                    gr.SetFontSize(fontSize);
                    FontExtents fe = gr.FontExtents;
                    rText = new Rectangle(new Point(0, 0), measureRawSize());
                    //gr.Rotate(Math.PI);

                    //  double a = Math.PI;
                    //gr.Transform(new c.Matrix(Math.Cos(a),-Math.Sin(a),Math.Sin(a),Math.Cos(a),renderBounds.Width,renderBounds.Height));
                    gr.Antialias = Antialias.Subpixel;
                    gr.LineWidth = borderWidth;

                    float widthRatio = 1f;
                    float heightRatio = 1f;

                    Rectangle cb = clientBounds;
                    Interface.StrokeLoweredRectangle(gr, cb, 1);
                    //ignore text alignment if size to content = true
                    if (!sizeToContent)
                    {
                        switch (textAlignment)
                        {
                            case Alignment.None:
                                break;
                            case Alignment.TopLeft:     //ok
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.TopCenter:   //ok
                                rText.Y = cb.Y;
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                break;
                            case Alignment.TopRight:    //ok
                                rText.X = cb.Right - rText.Width;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.TopStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                rText.Width = cb.Width;
                                rText.Height = (int)(rText.Height * heightRatio);
                                break;
                            case Alignment.LeftCenter://ok
                                rText.X = cb.X;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.LeftStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * cb.Width);
                                break;
                            case Alignment.RightCenter://ok
                                rText.X = cb.X + cb.Width - rText.Width;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.RightStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * cb.Width);
                                rText.X = cb.X;
                                rText.Y = cb.Y;
                                break;
                            case Alignment.BottomCenter://ok
                                rText.X = cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Height - rText.Height;
                                break;
                            case Alignment.BottomStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.Width = cb.Width;
                                rText.Height = (int)(rText.Height * heightRatio);
                                rText.Y = cb.Bottom - rText.Height;
                                rText.X = cb.X;
                                break;
                            case Alignment.BottomLeft://ok
                                rText.X = cb.X;
                                rText.Y = cb.Bottom - rText.Height;
                                break;
                            case Alignment.BottomRight://ok
                                rText.Y = cb.Bottom - rText.Height;
                                rText.X = cb.Right - rText.Width;
                                break;
                            case Alignment.Center://ok
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                break;
                            case Alignment.Fit://ok, peut être mieu aligné                            
                                widthRatio = (float)cb.Width / rText.Width;
                                heightRatio = (float)cb.Height / rText.Height;
                                rText = cb.Clone;
                                break;
                            case Alignment.HorizontalStretch://ok
                                heightRatio = widthRatio = (float)cb.Width / rText.Width;
                                rText.Width = cb.Width;
                                rText.Height = (int)(heightRatio * rText.Height);
                                rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
                                rText.X = cb.X;
                                break;
                            case Alignment.VerticalStretch://ok
                                heightRatio = widthRatio = (float)cb.Height / rText.Height;
                                rText.Height = cb.Height;
                                rText.Width = (int)(widthRatio * rText.Width);
                                rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
                                rText.Y = cb.Y;
                                break;
                            default:
                                break;
                        }
                    }



                    gr.FontMatrix = new Matrix(widthRatio * fontSize, 0, 0, heightRatio * fontSize, 0, 0);

                    fe = gr.FontExtents;

                    #region draw text cursor
                    if (mouseLocalPos > 0)
                    {
                        computeTextCursor(gr);

                        if (SelectionInProgress)
                        {
                            selEndPos = currentPos;
                            SelEndCursorPos = textCursorPos;
                        }
                        else if (selStartPos < 0)
                        {
                            selStartPos = currentPos;
                            SelStartCursorPos = textCursorPos;
                            selEndPos = -1;
                        }
                        else
                            computeTextCursorPosition(gr);

                    }
                    else
                        computeTextCursorPosition(gr);
                    if (hasFocus)
                    {
                        gr.LineWidth = 1;
                        gr.MoveTo(new PointD(textCursorPos + rText.TopLeft.X, rText.Y + rText.TopLeft.Y));
                        gr.LineTo(new PointD(textCursorPos + rText.TopLeft.X, rText.Y + fe.Height + rText.TopLeft.Y));
                        gr.Stroke();
                    }

                    #endregion

                    if (selEndPos >= 0)
                    {
                        gr.Color = selColor;
                        Rectangle selRect =
                            new Rectangle((int)SelStartCursorPos + rText.TopLeft.X, (int)(rText.Y) + rText.TopLeft.Y, (int)(SelEndCursorPos - SelStartCursorPos), (int)fe.Height);
                        gr.Rectangle(selRect);
                        gr.Fill();

                        gr.Color = selFontColor;
                    }
                    else
                        gr.Color = fontColor;

                    gr.MoveTo(rText.X, rText.Y + fe.Ascent);
#if _WIN32 || _WIN64
                    gr.ShowText(txt);
#elif __linux__
                    gr.ShowText(text);
#endif
                    gr.Fill();

                    


                    //gr.LineWidth = 1;
                    //gr.MoveTo(new PointD(rText.TopLeft.X, rText.Y));
                    //gr.LineTo(new PointD(rText.TopLeft.X, rText.Y + fe.Ascent));
                    //gr.Stroke();

                }
                bitmap.Flush();
                //bitmap.WriteToPng(directories.rootDir + @"test.png");
            }

            //registerForRedraw();
        }

    }
}
