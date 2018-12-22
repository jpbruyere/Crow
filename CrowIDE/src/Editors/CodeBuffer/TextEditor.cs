//
// ScrollingTextBox.cs
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
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Crow.Cairo;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Crow.Text
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class TextEditor : Crow.Coding.Editor
	{		
		#region CTOR
		public TextEditor (): base()
		{			
			buffer = new TextBuffer ();
			buffer.LineUpadateEvent += Buffer_LineUpadateEvent;
			buffer.LineAdditionEvent += Buffer_LineAdditionEvent;;
			buffer.LineRemoveEvent += Buffer_LineRemoveEvent;
			buffer.BufferCleared += Buffer_BufferCleared;
			buffer.SelectionChanged += Buffer_SelectionChanged;
			buffer.PositionChanged += Buffer_PositionChanged;
			//buffer.Add ("");
		}
		#endregion

		string oldSource = "";
		volatile bool isDirty = false;

		#region private and protected fields
		int visibleLines = 1;
		int visibleColumns = 1;

		TextBuffer buffer;

		Color selBackground;
		Color selForeground;
		int selStartCol;
		int selEndCol;

		protected Rectangle rText;
		protected FontExtents fe;
		protected TextExtents te;

		Point mouseLocalPos;
		bool doubleClicked = false;
		#endregion

		/// <summary>
		/// Updates visible line in widget, adapt max scroll y and updatePrintedLines
		/// </summary>
		void updateVisibleLines(){
			visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / (fe.Ascent+fe.Descent));
			NotifyValueChanged ("VisibleLines", visibleLines);
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
//			System.Diagnostics.Debug.WriteLine ("update visible lines: " + visibleLines);
//			System.Diagnostics.Debug.WriteLine ("update MaxScrollY: " + MaxScrollY);
		}
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width)/ fe.MaxXAdvance);
			NotifyValueChanged ("VisibleColumns", visibleColumns);
			RegisterForGraphicUpdate ();
//			System.Diagnostics.Debug.WriteLine ("update visible columns: {0} leftMargin:{1}",visibleColumns, leftMargin);
//			System.Diagnostics.Debug.WriteLine ("update MaxScrollX: " + MaxScrollX);
		}
		void updateMaxScrollX (int longestTabulatedLineLength) {			
			MaxScrollX = Math.Max (0, longestTabulatedLineLength - visibleColumns);
			if (longestTabulatedLineLength > 0)
				NotifyValueChanged ("ChildWidthRatio", Slot.Width * visibleColumns / longestTabulatedLineLength);
		}
		void updateMaxScrollY () {			
			int lc = buffer.LineCount;
			MaxScrollY = Math.Max (0, lc - visibleLines);
			if (lc > 0)
				NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / lc);
			
		}			



		#region Editor overrides
		protected override void updateEditorFromProjFile ()
		{
			buffer.editMutex.EnterWriteLock ();
			loadSource ();
			buffer.editMutex.ExitWriteLock ();

			isDirty = false;
			oldSource = projFile.Source;
			projFile.RegisteredEditors [this] = true;
		}
		protected override void updateProjFileFromEditor ()
		{
			buffer.editMutex.EnterWriteLock ();
			string newsrc = buffer.FullText;
			buffer.editMutex.ExitWriteLock ();
			projFile.UpdateSource (this, newsrc);
		}
		protected override bool EditorIsDirty {
			get { return isDirty; }
			set { isDirty = value; }
		}
		protected override bool IsReady {
			get { return projFile != null && buffer != null; }
		}
		#endregion

		#region Buffer events handlers
		void Buffer_BufferCleared (object sender, EventArgs e)
		{
			editorMutex.EnterWriteLock ();

			MaxScrollX = MaxScrollY = 0;
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;

			editorMutex.ExitWriteLock ();
		}
		void Buffer_LineAdditionEvent (object sender, TextBufferEventArgs e)
		{
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			isDirty = true;
		}
		void Buffer_LineRemoveEvent (object sender, TextBufferEventArgs e)
		{
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_LineUpadateEvent (object sender, TextBufferEventArgs e)
		{
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_PositionChanged (object sender, EventArgs e)
		{			
			int cc = getTabulatedColumn (buffer.CurrentPosition);

			if (cc > visibleColumns + ScrollX) {
				ScrollX = cc - visibleColumns;
			} else if (cc < ScrollX)
				ScrollX = cc;

			if (buffer.CurrentLine >= visibleLines + ScrollY - 1)
				ScrollY = buffer.CurrentLine - visibleLines + 1;
			else if (buffer.CurrentLine < ScrollY)
				ScrollY = buffer.CurrentLine;
			
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
		}

		void Buffer_SelectionChanged (object sender, EventArgs e)
		{
			RegisterForGraphicUpdate ();
		}
		#endregion

		void notifyPositionChanged (){
			try {				
				NotifyValueChanged ("CurrentLine", buffer.CurrentLine+1);
				NotifyValueChanged ("CurrentColumn", buffer.CurrentColumn+1);
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
		}
			
		#region Public Crow Properties
		public int CurrentLine{
			get { return buffer == null ? 0 : buffer.CurrentLine+1; }
			set {
				try {
					int l = value - 1;
					if (l == buffer.CurrentLine)
						return;
					buffer.CurrentLine = l;									
				} catch (Exception ex) {					
					Console.WriteLine ("Error cur column: " + ex.ToString ());
				}
			}
		}
		public int CurrentColumn{
			get { return buffer == null ? 0 : buffer.CurrentColumn+1; }
			set {
				try {					
					if (value - 1 == buffer.CurrentColumn)
						return;
					buffer.CurrentColumn = value - 1;
				} catch (Exception ex) {					
					Console.WriteLine ("Error cur column: " + ex.ToString ());
				}
			}
		}
		[DefaultValue("Blue")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (value == selBackground)
					return;
				selBackground = value;
				NotifyValueChanged ("SelectionBackground", selBackground);
				RegisterForRedraw ();
			}
		}
		[DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (value == selForeground)
					return;
				selForeground = value;
				NotifyValueChanged ("SelectionForeground", selForeground);
				RegisterForRedraw ();
			}
		}
		public override int ScrollY {
			get {
				return base.ScrollY;
			}
			set {
				if (value == base.ScrollY)
					return;
				editorMutex.EnterWriteLock ();
				base.ScrollY = value;
				editorMutex.ExitWriteLock ();
				RegisterForGraphicUpdate ();
			}
		}
		#endregion


		void loadSource () {
			buffer.Load (projFile.Source);
			projFile.RegisteredEditors [this] = true;
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
		}

		int getTabulatedColumn (int col, int line) {
			return buffer.GetSubString (buffer [line],
				buffer.GetLineLength (line)).Substring(0,col).Replace ("\t", new String (' ', Interface.TabSize)).Length;
		}
		int getTabulatedColumn (Point pos) {
			return getTabulatedColumn (pos.X,pos.Y);
		}

		#region Drawing
		void drawLines(Context gr, Rectangle cb) {
			int longestTabulatedLine = 0;
			for (int i = 0; i < visibleLines; i++) {
				int lineIndex = i + ScrollY;
				if (lineIndex >= buffer.LineCount)//TODO:need optimize
					break;							

				double y = cb.Y + (fe.Ascent+fe.Descent) * i, x = cb.X;

				int lineLength = buffer.GetLineLength (lineIndex);
				if (lineIndex < buffer.LineCount - 1)//dont print line break
					lineLength--;
				string lstr = buffer.GetSubString (buffer [lineIndex],
					lineLength).Replace ("\t", new String (' ', Interface.TabSize));

				int lstrLength = lstr.Length;
				if (lstrLength > longestTabulatedLine)
					longestTabulatedLine = lstrLength;
				
				if (ScrollX < lstrLength)
					lstr = lstr.Substring (ScrollX);
				else
					lstr = "";

				gr.MoveTo (x, y + fe.Ascent);
				gr.ShowText (lstr);
				gr.Fill ();

				if (!buffer.SelectionIsEmpty && lineIndex >= buffer.SelectionStart.Y && lineIndex <= buffer.SelectionEnd.Y) {
					double rLineX = x,
					rLineY = y,
					rLineW = lstr.Length * fe.MaxXAdvance;

					if (lineIndex == buffer.SelectionStart.Y) {
						rLineX += (selStartCol - ScrollX) * fe.MaxXAdvance;
						rLineW -= (selStartCol - ScrollX) * fe.MaxXAdvance;
					}
					if (lineIndex == buffer.SelectionEnd.Y)
						rLineW -= (lstr.Length - selEndCol + ScrollX) * fe.MaxXAdvance;

					gr.Save ();
					gr.Operator = Operator.Source;
					gr.Rectangle (rLineX, rLineY, rLineW, (fe.Ascent+fe.Descent));
					gr.SetSourceColor (SelectionBackground);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (SelectionForeground);
					gr.MoveTo (x, y + fe.Ascent);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}	
			
			
			}		
		
			updateMaxScrollX(longestTabulatedLine);
		}
		#endregion

		#region GraphicObject overrides
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;

				using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
					using (Context gr = new Context (img)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				MaxScrollY = 0;
				RegisterForGraphicUpdate ();
			}
		}
		protected override int measureRawSize(LayoutingType lt)
		{
			if (lt == LayoutingType.Height)
				return (int)Math.Ceiling((fe.Ascent+fe.Descent) * buffer.LineCount) + Margin * 2;

			return 0;// (int)(fe.MaxXAdvance * buffer.GetLineLength(buffer.longestLineIdx)) + Margin * 2;
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				updateVisibleLines ();
			else if (layoutType == LayoutingType.Width)
				updateVisibleColumns ();
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Rectangle cb = ClientRectangle;

			Foreground.SetAsSource (gr);

			buffer.editMutex.EnterReadLock ();
			editorMutex.EnterReadLock ();

			#region draw text cursor
			if (buffer.SelectionInProgress){
				selStartCol = getTabulatedColumn (buffer.SelectionStart);
				selEndCol = getTabulatedColumn (buffer.SelectionEnd);
			}else if (HasFocus && CurrentLine >= 0){
				gr.LineWidth = 1.0;
				double cursorX = cb.X + (getTabulatedColumn(buffer.CurrentPosition) - ScrollX) * fe.MaxXAdvance ;
				double cursorY = cb.Y + (buffer.CurrentLine - ScrollY) * (fe.Ascent+fe.Descent);
				gr.MoveTo (0.5 + cursorX, cursorY);
				gr.LineTo (0.5 + cursorX, cursorY + fe.Ascent+fe.Descent);
				gr.Stroke();
			}
			#endregion

			drawLines (gr, cb);

			editorMutex.ExitReadLock ();

			buffer.editMutex.ExitReadLock ();

		}
		#endregion

		int getBufferColFromVisualCol (int line, int column) {
			int i = 0;
			int buffCol = 0;
			int buffPtr = buffer [line];
			while (i < column && buffCol < buffer.GetLineLength(line)) {
				if (buffer.GetCharAt(buffPtr + buffCol) == '\t')
					i += Interface.TabSize;
				else
					i++;
				buffCol++;
			}
			return buffCol;
		}


		#region Mouse handling

		int hoverLine = -1;
		public int HoverLine {
			get { return hoverLine; }
			set { 
				if (hoverLine == value)
					return;
				hoverLine = value;
				NotifyValueChanged ("HoverLine", hoverLine);				
			}
		}
		void updateHoverLine () {
//			int hvl = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)));
//			hvl = Math.Min (PrintedLines.Count, hvl);
//			HoverLine = buffer.IndexOf (PrintedLines[hvl]);
		}
		void updateCurrentPosFromMouseLocalPos(){			
			
			buffer.CurrentLine = ScrollY + (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)));
			int curVisualCol = ScrollX +  (int)Math.Round ((mouseLocalPos.X) / fe.MaxXAdvance);
			buffer.CurrentColumn = getBufferColFromVisualCol (buffer.CurrentLine, curVisualCol);
		}

		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			IFace.MouseCursor = MouseCursors.Text;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			IFace.MouseCursor = MouseCursors.Default;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			mouseLocalPos = e.Position - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;

			updateHoverLine ();

			if (e.Mouse.LeftButton == ButtonState.Released || !buffer.SelectionInProgress)
				return;

			//mouse is down
			updateCurrentPosFromMouseLocalPos();
			buffer.SetSelEndPos ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (!Focusable)
				return;

			base.onMouseDown (sender, e);

			if (doubleClicked) {
				doubleClicked = false;
				return;
			}

			updateCurrentPosFromMouseLocalPos ();
			buffer.SetSelStartPos ();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (buffer.SelectionIsEmpty)
				buffer.ResetSelection ();
		}

		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			//doubleClicked = true;
			base.onMouseDoubleClick (sender, e);

			buffer.GotoWordStart ();
			buffer.SetSelStartPos ();
			buffer.GotoWordEnd ();
			buffer.SetSelEndPos ();
		}

		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{
			base.onMouseWheel (sender, e);
		}
		#endregion

		#region Keyboard handling
		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			//base.onKeyDown (sender, e);

			Key key = e.Key;

			if (IFace.Ctrl) {
				switch (key) {
				case Key.S:
					projFile.Save ();
					break;
				case Key.W:
					editorMutex.EnterWriteLock ();
					if (IFace.Shift)
						projFile.Redo (null);
					else
						projFile.Undo (null);
					editorMutex.ExitWriteLock ();
					break;
				default:
					Console.WriteLine ("");
					break;
				}
			}

			switch (key)
			{
			case Key.BackSpace:
				buffer.Delete ();
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (buffer.SelectionIsEmpty)
					buffer.MoveRight ();
//				else if (e.Shift)
//					IFace.Clipboard = buffer.SelectedText;
				buffer.Delete ();
				break;
			case Key.ISO_Enter:
			case Key.KP_Enter:
				if (!buffer.SelectionIsEmpty)
					buffer.Delete ();
				buffer.InsertLineBreak ();
				break;
			case Key.Escape:
				buffer.ResetSelection ();
				break;
			case Key.Home:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (IFace.Ctrl)
						buffer.CurrentLine = 0;
					buffer.CurrentColumn = 0;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.CurrentLine = 0;
				buffer.CurrentColumn = 0;
				break;
			case Key.End:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (IFace.Ctrl)
						buffer.CurrentLine = int.MaxValue;
					buffer.CurrentColumn = int.MaxValue;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.CurrentLine = int.MaxValue;
				buffer.CurrentColumn = int.MaxValue;
				break;
			case Key.Insert:
				if (IFace.Shift)
					buffer.InsertAt (IFace.Clipboard);
				else if (IFace.Ctrl && !buffer.SelectionIsEmpty)
					IFace.Clipboard = buffer.SelectedText;
				break;
			case Key.Left:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (IFace.Ctrl)
						buffer.GotoWordStart ();
					else
						buffer.MoveLeft ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.GotoWordStart ();
				else
					buffer.MoveLeft();
				break;
			case Key.Right:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (IFace.Ctrl)
						buffer.GotoWordEnd ();
					else
						buffer.MoveRight ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.GotoWordEnd ();
				else
					buffer.MoveRight ();
				break;
			case Key.Up:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					CurrentLine--;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				CurrentLine--;
				break;
			case Key.Down:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					CurrentLine++;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				CurrentLine++;
				break;
			case Key.Menu:
				break;
			case Key.Num_Lock:
				break;
			case Key.Page_Down:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					CurrentLine += visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				CurrentLine += visibleLines;
				break;
			case Key.Page_Up:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					CurrentLine -= visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				CurrentLine -= visibleLines;
				break;
			case Key.Tab:
				buffer.InsertAt ("\t");
				break;
			default:
				break;
			}
			RegisterForGraphicUpdate();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			buffer.InsertAt (e.KeyChar.ToString());
			buffer.ResetSelection ();
		}
		#endregion
	}
}