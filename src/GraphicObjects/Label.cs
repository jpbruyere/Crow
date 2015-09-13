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

namespace go
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
		protected string _text = "label";
        Alignment _textAlignment = Alignment.LeftCenter;        
		Font _font;
		bool _multiline = false;
		Color selColor;
		Color selFontColor;
		bool _selectable;
		Point mouseLocalPos;    //mouse coord in widget space, filled only when clicked        
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
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
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool Selectable {
			get { return _selectable; }
			set { _selectable = value; }
		}

        [XmlAttributeAttribute()][DefaultValue(Alignment.LeftCenter)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set { _textAlignment = value; }
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
					                
                registerForGraphicUpdate();
				this.RegisterForLayouting ((int)LayoutingType.Sizing);


                _text = value;

				if (string.IsNullOrEmpty(_text))
					_text = "";

				lines = getLines;
            }
        }
		[XmlAttributeAttribute()][DefaultValue("droid,10")]
		public Font Font {
			get { return _font; }
			set { _font = value; }
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				_multiline = value;
				registerForGraphicUpdate();
			}
		}
		[XmlIgnore]public int currentCol{
			get { return _currentCol; }
			set { 
				if (value < 0)
					_currentCol = 0;
				else if (value > lines [_currentLine].Count ())
					_currentCol = lines [_currentLine].Count ();
				else
					_currentCol = value; 
			}
		}
		[XmlIgnore]public int currentLine{
			get { return _currentLine; }
			set { 
				if (value > lines.Count)
					_currentLine = lines.Count; 
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value; 
			}
		}
		[XmlIgnore]protected Point selBegin;
		[XmlIgnore]protected Point selRelease;
		[XmlIgnore]protected Point selectionStart   //ordered selection start and end positions
		{
			get { 
				return selRelease < 0 || selBegin.Y < selRelease.Y ? selBegin : 
					selBegin.Y > selRelease.Y ? selRelease :
					selBegin.X < selRelease.X ? selBegin : selRelease;
			}
		}
		[XmlIgnore]public Point selectionEnd
		{  
			get { 
				return selRelease < 0 || selBegin.Y > selRelease.Y ? selBegin : 
					selBegin.Y < selRelease.Y ? selRelease :
					selBegin.X > selRelease.X ? selBegin : selRelease;
			}		
		}
		[XmlIgnore]public string selectedText
		{ 
			get { 
				return null; 
					//selectionEnd < 0 ? null : 
					//Text.Substring(selectionStart, selectionEnd - selectionStart); 
			}			
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return selRelease < 0; } }

		List<string> lines;
		List<string> getLines {
			get {				
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n").ToList() :
					new List<string>(new string[] { _text });
			}
		}

		public void DeleteChar()
		{
			if (selectionIsEmpty) {				
				if (currentCol == 0) {
					if (currentLine == 0)
						return;
					currentLine--;
					currentCol = lines [currentLine].Count ();
					lines [currentLine] += lines [currentLine + 1];
					lines.RemoveAt (currentLine + 1);
					return;
				}
				currentCol--;
				lines [currentLine] = lines [currentLine].Remove (currentCol, 1);
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
					currentCol = selectionStart.X;
					currentLine = selectionStart.Y;
				} else 
					lines [l] = lines [l].Remove (selectionStart.X, selectionEnd.X - selectionStart.X);
				currentCol = selectionStart.X;
				selBegin = -1;
				selRelease = -1;
			}
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			lines [currentLine] = lines [currentLine].Insert (currentCol, str);
			currentCol += str.Length;
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

			#region draw text cursor
			if (mouseLocalPos > 0)
			{
				computeTextCursor(gr);

				if (SelectionInProgress)
				{
					selRelease = new Point(currentCol, currentLine);
					SelEndCursorPos = textCursorPos;
				}
				else if (selBegin < 0)
				{
					selBegin = new Point(currentCol, currentLine);
					SelStartCursorPos = textCursorPos;
					selRelease = -1;
				}
				else
					computeTextCursorPosition(gr);
			}
			else
				computeTextCursorPosition(gr);

			if (HasFocus && Selectable)
			{
				//TODO:
				gr.Color = Foreground;
				gr.LineWidth = 1.5;
				gr.MoveTo(new PointD(textCursorPos + rText.X, rText.Y + currentLine * fe.Height));
				gr.LineTo(new PointD(textCursorPos + rText.X, rText.Y + (currentLine + 1) * fe.Height));
				gr.Stroke();
			}
			#endregion

			//*******************
			//debug selection
//			gr.Color = Color.Green;
//			Rectangle R = new Rectangle(
//				rText.X +  (int)SelEndCursorPos-10,
//				rText.Y + (int)(selRelease.Y * fe.Height), 
//				20, 
//				(int)fe.Height);
//			gr.Rectangle(R);
//			gr.Fill();
//			gr.Color = Color.UnmellowYellow;
//			R = new Rectangle(
//				rText.X + (int)SelStartCursorPos-10,
//				rText.Y + (int)(selBegin.Y * fe.Height), 
//				20, 
//				(int)fe.Height);
//			gr.Rectangle(R);
//			gr.Fill();
			//*******************

			gr.FontMatrix = new Matrix(widthRatio * Font.Size, 0, 0, heightRatio * Font.Size, 0, 0);

			for (int i = 0; i < lines.Count; i++) {				
				string l = lines [i].Replace ("\t", new String (' ', Interface.TabSize));
				if (selRelease >= 0 && i >= selectionStart.Y && i <= selectionEnd.Y) {					
					gr.Color = selColor;
					int lineLength = (int)gr.TextExtents (l).XAdvance;
					Rectangle selRect = new Rectangle (
						rText.X,
						rText.Y + (int)(i * fe.Height), 
						lineLength, 
						(int)fe.Height);

					int cpStart = (int)SelStartCursorPos,
						cpEnd = (int)SelEndCursorPos;

					if (selBegin.Y > selRelease.Y) {
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
			
//				double t = rText.Y;
//				gr.LineWidth = 1;
//				gr.Color = Color.Green;
//				gr.MoveTo (rText.X, t);
//				gr.LineTo (rText.X + rText.Width, t);
//				gr.Stroke ();
//				gr.MoveTo (rText.X, t + rText.Height);
//				gr.LineTo (rText.X + rText.Width, t + rText.Height);
//				gr.Stroke ();
//				gr.Rectangle (rText);
//				gr.Fill ();
//
//				t += fe.Ascent;
//				gr.Color = Color.Red;
//				gr.MoveTo (rText.X, t);
//				gr.LineTo (rText.X + rText.Width, t);
//				gr.Stroke ();

				gr.Color = Foreground;				
				gr.MoveTo (rText.X, rText.Y + fe.Ascent + fe.Height * i);

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

			if (!Selectable)
				return;
			
			SelectionInProgress = true;
			mouseLocalPos = e.Position - ScreenCoordinates(ClientRectangle).TopLeft;
			registerForGraphicUpdate();
		}
		public override void onMouseButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (this.HasFocus && Selectable){
				mouseLocalPos = e.Position - ScreenCoordinates(ClientRectangle).TopLeft;
				selBegin = -1;
				selRelease = -1;
			}else{
//				selBeginPos = 0;
//				selReleasePos = new Point(lines[lines.Count].Length-1, lines.Count-1);
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
			//FontExtents fe = gr.FontExtents;
			TextExtents te;

			double cPos = 0f;

			currentLine = (int)(mouseLocalPos.Y / fe.Height);

			//fix cu
			if (currentLine >= lines.Count)
				currentLine = lines.Count - 1;

			for (int i = 0; i < lines[currentLine].Length; i++)
			{
				string c = lines [currentLine].Substring (i, 1);
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
					currentCol = i;
					textCursorPos = cPos;
					mouseLocalPos = -1;
					return;
				}

				cPos += te.XAdvance;
			}
			currentCol = lines[currentLine].Length;
			textCursorPos = cPos;

			//reset mouseLocalPos
			mouseLocalPos = -1;
		}
		void computeTextCursorPosition(Context gr)
		{			
			TextExtents te;

			double cPos = 0f;

			int limit = currentCol;

			if (selectionEnd > 0)
				limit = Math.Max(currentCol, selectionEnd.X);

			for (int i = 0; i <= limit; i++)
			{
				if (i == currentCol)
					textCursorPos = cPos;
				if (i == selectionStart.X)
					SelStartCursorPos = cPos;
				if (i == selectionEnd.X)
					SelEndCursorPos = cPos;

				if (i < lines[currentLine].Length)
				{
					string c = lines [currentLine].Substring (i, 1);
					if (c == "\t")
						c = new string (' ', Interface.TabSize);
					#if _WIN32 || _WIN64
					byte[] c = System.Text.UTF8Encoding.UTF8.GetBytes(Text.Substring(i, 1));
					te = gr.TextExtents(c);
					#elif __linux__
					te = gr.TextExtents(c);
					#endif
					cPos += te.XAdvance;
				}
			}

		}		
    }
}
