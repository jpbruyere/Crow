//
//  Label.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Cairo;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{    
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
        Alignment _textAlignment = Alignment.Left;
		bool horizontalStretch = false;
		bool verticalStretch = false;
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

		[XmlAttributeAttribute][DefaultValue("SteelBlue")]
		public virtual Color SelectionBackground {
			get { return selColor; }
			set {
				if (value == selColor)
					return;
				selColor = value;
				NotifyValueChanged ("SelectionBackground", selColor);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute][DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selFontColor; }
			set {
				if (value == selFontColor)
					return;
				selFontColor = value;
				NotifyValueChanged ("SelectionForeground", selFontColor);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute][DefaultValue(Alignment.Left)]
		public Alignment TextAlignment
        {
            get { return _textAlignment; }
            set { 
				if (value == _textAlignment)
					return;
				_textAlignment = value; 
				RegisterForGraphicUpdate ();
				RegisterForGraphicUpdate ();
				NotifyValueChanged ("TextAlignment", _textAlignment);
			}
        }
		[XmlAttributeAttribute][DefaultValue(false)]
		public virtual bool HorizontalStretch {
			get { return horizontalStretch; }
			set {
				if (horizontalStretch == value)
					return;
				horizontalStretch = value; 
				RegisterForGraphicUpdate ();
				NotifyValueChanged ("HorizontalStretch", horizontalStretch);
			}
		}
		[XmlAttributeAttribute][DefaultValue(false)]
		public virtual bool VerticalStretch {
			get { return verticalStretch; }
			set {
				if (verticalStretch == value)
					return;
				verticalStretch = value; 
				NotifyValueChanged ("VerticalStretch", verticalStretch);

			}
		} 
		[XmlAttributeAttribute][DefaultValue("label")]
        public string Text
        {
            get {				
				return lines == null ? 
					_text : lines.Aggregate((i, j) => i + Interface.LineBreak + j);
			}
            set
            {
				if (string.Equals (value, _text, StringComparison.Ordinal))
                    return;
					                                
                _text = value;

				if (string.IsNullOrEmpty(_text))
					_text = "";

				lines = getLines;

				this.RegisterForGraphicUpdate ();
				this.RegisterForLayouting (LayoutingType.Sizing);
				NotifyValueChanged ("Text", _text);
            }
        }
		[XmlAttributeAttribute][DefaultValue(false)]
		public bool Multiline
		{
			get { return _multiline; }
			set
			{
				if (value == _multiline)
					return;
				_multiline = value;
				NotifyValueChanged ("Multiline", _multiline);
				RegisterForGraphicUpdate();
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
		public int CurrentColumn{
			get { return _currentCol; }
			set { 
				if (value == _currentCol)
					return;
				if (value < 0)
					_currentCol = 0;
				else if (value > lines [_currentLine].Length)
					_currentCol = lines [_currentLine].Length;
				else
					_currentCol = value;
				NotifyValueChanged ("CurrentColumn", _currentCol);
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
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
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;
				NotifyValueChanged ("CurrentLine", _currentLine);
			}
		}
		//TODO:using HasFocus for drawing selection cause SelBegin and Release binding not to work
		[XmlAttributeAttribute][DefaultValue("-1")]
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
		[XmlAttributeAttribute][DefaultValue("-1")]
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

		[XmlIgnore]protected Char CurrentChar   //ordered selection start and end positions
		{
			get {
				return lines [CurrentLine] [CurrentColumn];
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

		public void GotoWordStart(){
			CurrentColumn--;
			//skip white spaces
			while (char.IsWhiteSpace (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			while (!char.IsWhiteSpace (lines [CurrentLine] [CurrentColumn]) && CurrentColumn > 0)
				CurrentColumn--;
			if (char.IsWhiteSpace (this.CurrentChar))
				CurrentColumn++;
		}
		public void GotoWordEnd(){
			//skip white spaces
			while (char.IsWhiteSpace (this.CurrentChar) && CurrentColumn < lines [CurrentLine].Length-1)
				CurrentColumn++;
			while (!char.IsWhiteSpace (this.CurrentChar) && CurrentColumn < lines [CurrentLine].Length-1)
				CurrentColumn++;
			if (!char.IsWhiteSpace (this.CurrentChar))
				CurrentColumn++;
		}
		public void DeleteChar()
		{
			if (selectionIsEmpty) {				
				if (CurrentColumn == 0) {
					if (CurrentLine == 0)
						return;
					CurrentLine--;
					CurrentColumn = lines [CurrentLine].Length;
					lines [CurrentLine] += lines [CurrentLine + 1];
					lines.RemoveAt (CurrentLine + 1);
					NotifyValueChanged ("Text", Text);
					return;
				}
				CurrentColumn--;
				lines [CurrentLine] = lines [CurrentLine].Remove (CurrentColumn, 1);
			} else {				
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
		/// <summary>
		/// Insert a line break.
		/// </summary>
		protected void InsertLineBreak()
		{
			lines.Insert(CurrentLine + 1, lines[CurrentLine].Substring(CurrentColumn));
			lines [CurrentLine] = lines [CurrentLine].Substring (0, CurrentColumn);
			CurrentLine++;
			CurrentColumn = 0;
			NotifyValueChanged ("Text", Text);
		}

		#region GraphicObject overrides
		protected override int measureRawSize(LayoutingType lt)
        {			
			if (lines == null)
				lines = getLines;
								
			using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
				using (Context gr = new Context (img)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);


					fe = gr.FontExtents;
					te = new TextExtents();

					if (lt == LayoutingType.Height){
						int lc = lines.Count;
						//ensure minimal height = text line height
						if (lc == 0)
							lc = 1; 
						
						return (int)Math.Ceiling(fe.Height * lc) + Margin * 2;
					}

					foreach (string s in lines) {
						string l = s.Replace("\t", new String (' ', Interface.TabSize));

						TextExtents tmp = gr.TextExtents (l);

						if (tmp.XAdvance > te.XAdvance)
							te = tmp;
					}
					return (int)Math.Ceiling (te.XAdvance) + Margin * 2;
				}
			}
        }
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;

			gr.Antialias = Antialias.Subpixel;


			rText = new Rectangle(new Size(
				measureRawSize(LayoutingType.Width), measureRawSize(LayoutingType.Height)));
			rText.Width -= 2 * Margin;
			rText.Height -= 2 * Margin;

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = ClientRectangle;

			rText.X = cb.X;
			rText.Y = cb.Y;

			if (horizontalStretch) {
				widthRatio = (float)cb.Width / (float)rText.Width;
				if (!verticalStretch)
					heightRatio = widthRatio;
			}

			if (verticalStretch) {
				heightRatio = (float)cb.Height / (float)rText.Height;
				if (!horizontalStretch)
					widthRatio = heightRatio;
			}
			
			rText.Width = (int)(widthRatio * (float)rText.Width);
			rText.Height = (int)(heightRatio * (float)rText.Height);

			switch (TextAlignment)
			{
			case Alignment.TopLeft:     //ok
				rText.X = cb.X;
				rText.Y = cb.Y;
				break;
			case Alignment.Top:   //ok						
				rText.Y = cb.Y;
				rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
				break;
			case Alignment.TopRight:    //ok
				rText.Y = cb.Y;
				rText.X = cb.Right - rText.Width;
				break;
			case Alignment.Left://ok
				rText.X = cb.X;
				rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
				break;
			case Alignment.Right://ok
				rText.X = cb.X + cb.Width - rText.Width;
				rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
				break;
			case Alignment.Bottom://ok
				rText.X = cb.Width / 2 - rText.Width / 2;
				rText.Y = cb.Height - rText.Height;
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
			}

			gr.FontMatrix = new Matrix(widthRatio * (float)Font.Size, 0, 0, heightRatio * (float)Font.Size, 0, 0);
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
//					TextAlignment == Alignment.Top ||
//					TextAlignment == Alignment.Bottom)
//					lineRect.X += (rText.Width - lineLength) / 2;
//				else if (TextAlignment == Alignment.Right ||
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

				gr.ShowText (l);

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
			RegisterForGraphicUpdate ();
		}
		public override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);

			SelBegin = -1;
			SelRelease = -1;
			RegisterForGraphicUpdate ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!(SelectionInProgress && HasFocus))
				return;

			updatemouseLocalPos (e.Position);

			RegisterForGraphicUpdate();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{			
			if (this.HasFocus){
				updatemouseLocalPos (e.Position);
				SelBegin = -1;
				SelRelease = -1;
				SelectionInProgress = true;
				RegisterForGraphicUpdate();//TODO:should put it in properties
			}          

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (!SelectionInProgress)
				return;
			
			updatemouseLocalPos (e.Position);
			SelectionInProgress = false;
			RegisterForGraphicUpdate ();
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
				
				te = gr.TextExtents(c);

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
