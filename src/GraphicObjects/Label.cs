using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Cairo;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.ComponentModel;
using OpenTK.Input;

namespace Crow
{
    [Serializable]
    public class Label : GraphicObject
    {
		#region CTOR
		public Label()
		{ 

		}
		public Label(string _text)
			: base()
		{
			Text = _text;
		}
		#endregion

        //TODO:change protected to private
        
		#region private and protected fields
		string _text = "label";
        Alignment _textAlignment = Alignment.LeftCenter;        
		bool _multiline = false;
		Color selColor;
		Color selFontColor;
		Point mouseLocalPos = -1;//mouse coord in widget space, filled only when clicked        
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
		Point _selBegin = -1;	//selection start (row,column)
		Point _selRelease = -1;	//selection end (row,column)
		double textCursorPos;   //cursor position in cairo units in widget client coord.
		double SelStartCursorPos = -1;
		double SelEndCursorPos = -1;
		bool SelectionInProgress = false;

        protected Rectangle rText;
		protected float widthRatio = 1f;
		protected float heightRatio = 1f;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion

		[XmlAttributeAttribute()][DefaultValue("SteelBlue")]
		public virtual Color SelectionBackground {
			get { return selColor; }
			set {
				if (value == selColor)
					return;
				selColor = value;
				NotifyValueChanged ("SelectionBackground", selColor);
				registerForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute()][DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selFontColor; }
			set {
				if (value == selFontColor)
					return;
				selFontColor = value;
				NotifyValueChanged ("SelectionForeground", selFontColor);
				registerForGraphicUpdate ();
			}
		}
        [XmlAttributeAttribute()][DefaultValue(Alignment.LeftCenter)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set { 
				if (value == _textAlignment)
					return;
				_textAlignment = value; 
				registerForGraphicUpdate ();
				NotifyValueChanged ("TextAlignment", _textAlignment);
			}
        }
		[XmlAttributeAttribute()][DefaultValue("label")]
        public string Text
        {
            get {				
				return lines == null ? 
					_text : lines.Aggregate((i, j) => i + Interface.LineBreak + j);
			}
            set
            {
                if (_text == value)
                    return;
					                                
                _text = value;

				if (string.IsNullOrEmpty(_text))
					_text = "";

				lines = getLines;

				this.registerForGraphicUpdate ();
				this.RegisterForLayouting ((int)LayoutingType.Sizing);
				NotifyValueChanged ("Text", _text);
            }
        }
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				if (value == _multiline)
					return;
				_multiline = value;
				NotifyValueChanged ("Multiline", _multiline);
				registerForGraphicUpdate();
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public int CurrentColumn{
			get { return _currentCol; }
			set { 
				if (value == _currentCol)
					return;
				if (value < 0)
					_currentCol = 0;
				else if (value > lines [_currentLine].Count ())
					_currentCol = lines [_currentLine].Count ();
				else
					_currentCol = value;
				NotifyValueChanged ("CurrentColumn", _currentCol);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(0)]
		public int CurrentLine{
			get { return _currentLine; }
			set { 
				if (value == _currentLine)
					return;
				if (value > lines.Count)
					_currentLine = lines.Count; 
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value; 
				NotifyValueChanged ("CurrentLine", _currentLine);
			}
		}
		//TODO:using HasFocus for drawing selection cause SelBegin and Release binding not to work
		[XmlAttributeAttribute()][DefaultValue("-1")]
		public Point SelBegin {
			get {
				return _selBegin;
			}
			set {
				if (value == _selBegin)
					return;
				_selBegin = value;
				NotifyValueChanged ("SelBegin", _selBegin);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}			
		[XmlAttributeAttribute()][DefaultValue("-1")]
		public Point SelRelease {
			get {
				return _selRelease;
			}
			set {
				if (value == _selRelease)
					return;
				_selRelease = value;
				NotifyValueChanged ("SelRelease", _selRelease);
				NotifyValueChanged ("SelectedText", SelectedText);
			}
		}

		[XmlIgnore]protected Point selectionStart   //ordered selection start and end positions
		{
			get { 
				return SelRelease < 0 || SelBegin.Y < SelRelease.Y ? SelBegin : 
					SelBegin.Y > SelRelease.Y ? SelRelease :
					SelBegin.X < SelRelease.X ? SelBegin : SelRelease;
			}
		}
		[XmlIgnore]public Point selectionEnd
		{  
			get { 
				return SelRelease < 0 || SelBegin.Y > SelRelease.Y ? SelBegin : 
					SelBegin.Y < SelRelease.Y ? SelRelease :
					SelBegin.X > SelRelease.X ? SelBegin : SelRelease;
			}		
		}
		[XmlIgnore]public string SelectedText
		{ 
			get {
				
				if (SelRelease < 0 || SelBegin < 0)
					return "";
				if (selectionStart.Y == selectionEnd.Y)
					return lines [selectionStart.Y].Substring (selectionStart.X, selectionEnd.X - selectionStart.X);
				string tmp = "";
				tmp = lines [selectionStart.Y].Substring (selectionStart.X);
				for (int l = selectionStart.Y + 1; l < selectionEnd.Y; l++) {
					tmp += Interface.LineBreak + lines [l];
				}
				tmp += Interface.LineBreak + lines [selectionEnd.Y].Substring (0, selectionEnd.X);
				return tmp;
			}			
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return SelRelease < 0; } }

		List<string> lines;
		List<string> getLines {
			get {				
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n|" + @"\\n").ToList() :
					new List<string>(new string[] { _text });
			}
		}

		public void DeleteChar()
		{
			if (selectionIsEmpty) {				
				if (CurrentColumn == 0) {
					if (CurrentLine == 0)
						return;
					CurrentLine--;
					CurrentColumn = lines [CurrentLine].Count ();
					lines [CurrentLine] += lines [CurrentLine + 1];
					lines.RemoveAt (CurrentLine + 1);
					NotifyValueChanged ("Text", Text);
					return;
				}
				CurrentColumn--;
				lines [CurrentLine] = lines [CurrentLine].Remove (CurrentColumn, 1);
			} else {
				Debug.WriteLine (selectionEnd.ToString());
				int linesToRemove = selectionEnd.Y - selectionStart.Y;
				int l = selectionStart.Y;

				if (linesToRemove > 0) {
					lines [l] = lines [l].Remove (selectionStart.X, lines [l].Length - selectionStart.X) +
						lines [selectionEnd.Y].Substring (selectionEnd.X, lines [selectionEnd.Y].Length - selectionEnd.X);
					l++;
					for (int c = 0; c < linesToRemove-1; c++)
						lines.RemoveAt (l);
					CurrentColumn = selectionStart.X;
					CurrentLine = selectionStart.Y;
				} else 
					lines [l] = lines [l].Remove (selectionStart.X, selectionEnd.X - selectionStart.X);
				CurrentColumn = selectionStart.X;
				SelBegin = -1;
				SelRelease = -1;
			}
			NotifyValueChanged ("Text", Text);
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			lines [CurrentLine] = lines [CurrentLine].Insert (CurrentColumn, str);
			CurrentColumn += str.Length;
			NotifyValueChanged ("Text", Text);
		}

		#region GraphicObject overrides
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(-1)]
		public override int Height {
			get { return base.Height; }
			set { base.Height = value; }
		}

		protected override Size measureRawSize()
        {
			Size size;

			if (lines == null)
				lines = getLines;
				
			using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
				using (Context gr = new Context (img)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);

					te = new TextExtents();

					foreach (string s in lines) {
						string l = s.Replace("\t", new String (' ', Interface.TabSize));

#if _WIN32 || _WIN64
						TextExtents tmp = gr.TextExtents(str.ToUtf8());
#elif __linux__
						TextExtents tmp = gr.TextExtents (l);
#endif
						if (tmp.XAdvance > te.XAdvance)
							te = tmp;
					}
					fe = gr.FontExtents;
					int lc = lines.Count;
					//ensure minimal height = text line height
					if (lc == 0)
						lc = 1; 
					size = new Size ((int)Math.Ceiling (te.XAdvance) + Margin * 2, (int)(fe.Height * lc) + Margin*2);
				}
			}

            return size;;
        }
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			gr.Antialias = Antialias.Subpixel;
			//gr.FontOptions.Antialias = Antialias.Subpixel;
			//gr.FontOptions.HintMetrics = HintMetrics.On;

			rText = new Rectangle(measureRawSize());
			rText.Width -= 2 * Margin;
			rText.Height -= 2 * Margin;

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = ClientRectangle;

			//ignore text alignment if size to content = true
			if (Bounds.Size < 0)
			{
				rText.X = cb.X;
				rText.Y = cb.Y;
			}else{
				switch (TextAlignment)
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
					rText.Width = (int)(widthRatio * rText.Width);
					rText.X = cb.Right - rText.Width;
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
					rText = cb;
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

			gr.FontMatrix = new Matrix(widthRatio * Font.Size, 0, 0, heightRatio * Font.Size, 0, 0);
			fe = gr.FontExtents;

			#region draw text cursor
			if (mouseLocalPos >= 0)
			{
				computeTextCursor(gr);

				if (SelectionInProgress)
				{
					if (SelBegin < 0){
						SelBegin = new Point(CurrentColumn, CurrentLine);
						SelStartCursorPos = textCursorPos;
						SelRelease = -1;
					}else{
						SelRelease = new Point(CurrentColumn, CurrentLine);
						if (SelRelease == SelBegin)
							SelRelease = -1;
						else
							SelEndCursorPos = textCursorPos;
					}						
				}
			}else
				computeTextCursorPosition(gr);


			#endregion

			//****** debug selection *************
//			if (SelRelease >= 0) {
//				gr.Color = Color.Green;
//				Rectangle R = new Rectangle (
//					             rText.X + (int)SelEndCursorPos - 2,
//					             rText.Y + (int)(SelRelease.Y * fe.Height), 
//					             4, 
//					             (int)fe.Height);
//				gr.Rectangle (R);
//				gr.Fill ();
//			}
//			if (SelBegin >= 0) {
//				gr.Color = Color.UnmellowYellow;
//				Rectangle R = new Rectangle (
//					rText.X + (int)SelStartCursorPos - 2,
//					rText.Y + (int)(SelBegin.Y * fe.Height), 
//					4, 
//					(int)fe.Height);
//				gr.Rectangle (R);
//				gr.Fill ();
//			}
			//*******************

			if (HasFocus )
			{
				Foreground.SetAsSource (gr);
				gr.LineWidth = 1.5;
				gr.MoveTo(new PointD(textCursorPos + rText.X, rText.Y + CurrentLine * fe.Height));
				gr.LineTo(new PointD(textCursorPos + rText.X, rText.Y + (CurrentLine + 1) * fe.Height));
				gr.Stroke();
			}

			for (int i = 0; i < lines.Count; i++) {				
				string l = lines [i].Replace ("\t", new String (' ', Interface.TabSize));
				int lineLength = (int)gr.TextExtents (l).XAdvance;
				Rectangle lineRect = new Rectangle (
					rText.X,
					rText.Y + (int)(i * fe.Height), 
					lineLength, 
					(int)fe.Height);

//				if (TextAlignment == Alignment.Center ||
//					TextAlignment == Alignment.TopCenter ||
//					TextAlignment == Alignment.BottomCenter)
//					lineRect.X += (rText.Width - lineLength) / 2;
//				else if (TextAlignment == Alignment.RightCenter ||
//					TextAlignment == Alignment.TopRight ||
//					TextAlignment == Alignment.BottomRight)
//					lineRect.X += (rText.Width - lineLength);
				
				if (SelRelease >= 0 && i >= selectionStart.Y && i <= selectionEnd.Y) {					
					gr.SetSourceColor(selColor);

					Rectangle selRect = lineRect ;

					int cpStart = (int)SelStartCursorPos,
						cpEnd = (int)SelEndCursorPos;

					if (SelBegin.Y > SelRelease.Y) {
						cpStart = cpEnd;
						cpEnd = (int)SelStartCursorPos;
					}

					if (i == selectionStart.Y) {
						selRect.Width -= cpStart;
						selRect.Left += cpStart;
					}
					if (i == selectionEnd.Y)				
						selRect.Width -= (lineLength - cpEnd);					

					gr.Rectangle (selRect);
					gr.Fill ();
				} 

				if (string.IsNullOrWhiteSpace (l))
					continue;

				Foreground.SetAsSource (gr);	
				gr.MoveTo (lineRect.X, rText.Y + fe.Ascent + fe.Height * i);

				#if _WIN32 || _WIN64
				gr.ShowText(l.ToUtf8());
				#elif __linux__
				gr.ShowText (l);
				#endif
				gr.Fill ();
			}
		}
		#endregion

		#region Mouse handling
		void updatemouseLocalPos(Point mpos){
			mouseLocalPos = mpos - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;
			if (mouseLocalPos.X < 0)
				mouseLocalPos.X = 0;
			if (mouseLocalPos.Y < 0)
				mouseLocalPos.Y = 0;
		}
		public override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			SelBegin = new Point(0,0);
			SelRelease = new Point (lines.LastOrDefault ().Length, lines.Count-1);
			registerForGraphicUpdate ();
		}
		public override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);

			SelBegin = -1;
			SelRelease = -1;
			registerForGraphicUpdate ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(SelectionInProgress && HasFocus))
				return;

			updatemouseLocalPos (e.Position);

			registerForGraphicUpdate();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{			
			if (this.HasFocus){
				updatemouseLocalPos (e.Position);
				SelBegin = -1;
				SelRelease = -1;
				SelectionInProgress = true;
			}          

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseDown (sender, e);

			registerForGraphicUpdate();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (!SelectionInProgress)
				return;
			
			updatemouseLocalPos (e.Position);
			SelectionInProgress = false;
			registerForGraphicUpdate ();
		}
		#endregion
		/// <summary>
		/// Update Current Column, line and TextCursorPos
		/// from mouseLocalPos
		/// </summary>
		void computeTextCursor(Context gr)
		{			
			TextExtents te;

			double cPos = 0f;

			CurrentLine = (int)(mouseLocalPos.Y / fe.Height);

			//fix cu
			if (CurrentLine >= lines.Count)
				CurrentLine = lines.Count - 1;

			for (int i = 0; i < lines[CurrentLine].Length; i++)
			{
				string c = lines [CurrentLine].Substring (i, 1);
				if (c == "\t")
					c = new string (' ', Interface.TabSize);
				
				#if _WIN32 || _WIN64
				byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(Text.Substring(i, 1));
				te = gr.TextExtents(c);
				#elif __linux__
				te = gr.TextExtents(c);
				#endif
				double halfWidth = te.XAdvance / 2;

				if (mouseLocalPos.X <= cPos + halfWidth)
				{
					CurrentColumn = i;
					textCursorPos = cPos;
					mouseLocalPos = -1;
					return;
				}

				cPos += te.XAdvance;
			}
			CurrentColumn = lines[CurrentLine].Length;
			textCursorPos = cPos;

			//reset mouseLocalPos
			mouseLocalPos = -1;
		}
		/// <summary> Computes offsets in cairo units </summary>
		void computeTextCursorPosition(Context gr)
		{			
			if (SelBegin >= 0)
				SelStartCursorPos = GetXFromTextPointer (gr, SelBegin);
			if (SelRelease >= 0)
				SelEndCursorPos = GetXFromTextPointer (gr, SelRelease);
			textCursorPos = GetXFromTextPointer (gr, new Point(CurrentColumn, CurrentLine));
		}
		/// <summary> Compute x offset in cairo unit from text position </summary>
		double GetXFromTextPointer(Context gr, Point pos)
		{
			try {
				string l = lines [pos.Y].Substring (0, pos.X).
					Replace ("\t", new String (' ', Interface.TabSize));
				return gr.TextExtents (l).XAdvance;
			} catch{
				return -1;
			}
		}
    }
}
