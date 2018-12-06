﻿//
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
using Cairo;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Crow
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class DebugLogViewer : ScrollingObject
	{		
		#region CTOR
		public DebugLogViewer (): base()
		{
			
		}
		#endregion

		string oldSource = "";
		//save requested position on error, and try it on next move
		int requestedLine = 0, requestedCol = 0;
		volatile bool isDirty = false;

		const int leftMarginGap = 10;//gap between items in margin and text
		const int foldSize = 9;//folding rectangles size
		const int foldHSpace = 4;//folding level tabulation x
		int foldMargin { get { return parser == null ? 0 : parser.SyntacticTreeMaxDepth * foldHSpace; }}//folding margin size

		#region private and protected fields
		bool foldingEnabled = true;
		int leftMargin = 0;	//margin used to display line numbers, folding errors,etc...
		int visibleLines = 1;
		int visibleColumns = 1;
		int firstPrintedLine = -1;
		int printedCurrentLine = 0;//Index of the currentline in the PrintedLines array

		CodeBuffer buffer;
		BufferParser parser;
		List<CodeLine> PrintedLines;//list of lines visible in the Editor depending on scrolling and folding

		Dictionary<int, TextFormatting> formatting = new Dictionary<int, TextFormatting>();
		Dictionary<string, string> parsing = new Dictionary<string, string>();

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

		void measureLeftMargin () {
			leftMargin = 0;
			if (PrintLineNumbers)
				leftMargin += (int)Math.Ceiling((double)buffer.LineCount.ToString().Length * fe.MaxXAdvance) +6;
			if (foldingEnabled)
				leftMargin += foldMargin;
			if (leftMargin > 0)
				leftMargin += leftMarginGap;			
			updateVisibleColumns ();
		}

		/// <summary>
		/// Updates visible line in widget, adapt max scroll y and updatePrintedLines
		/// </summary>
		void updateVisibleLines(){
			visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / (fe.Ascent+fe.Descent));
			NotifyValueChanged ("VisibleLines", visibleLines);
			updateMaxScrollY ();
			updatePrintedLines ();
			RegisterForGraphicUpdate ();
//			System.Diagnostics.Debug.WriteLine ("update visible lines: " + visibleLines);
//			System.Diagnostics.Debug.WriteLine ("update MaxScrollY: " + MaxScrollY);
		}
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width - leftMargin)/ fe.MaxXAdvance);
			NotifyValueChanged ("VisibleColumns", visibleColumns);
			updateMaxScrollX ();
//			System.Diagnostics.Debug.WriteLine ("update visible columns: {0} leftMargin:{1}",visibleColumns, leftMargin);
//			System.Diagnostics.Debug.WriteLine ("update MaxScrollX: " + MaxScrollX);
		}
		void updateMaxScrollX () {
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);
			if (buffer.longestLineCharCount > 0)
				NotifyValueChanged ("ChildWidthRatio", Slot.Width * visibleColumns / buffer.longestLineCharCount);			
		}
		void updateMaxScrollY () {
			if (parser == null || !foldingEnabled) {
				MaxScrollY = Math.Max (0, buffer.LineCount - visibleLines);
				if (buffer.UnfoldedLines > 0)
					NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / buffer.UnfoldedLines);							
			} else {
				MaxScrollY = Math.Max (0, buffer.UnfoldedLines - visibleLines);
				if (buffer.UnfoldedLines > 0)
					NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / buffer.UnfoldedLines);							
			}
		}			
		void updatePrintedLines () {
			buffer.editMutex.EnterReadLock ();
			editorMutex.EnterWriteLock ();

			PrintedLines = new List<CodeLine> ();
			int curL = 0;
			int i = 0;

			while (curL < buffer.LineCount && i < ScrollY) {
				if (buffer [curL].IsFolded)
					curL = buffer.GetEndNodeIndex (curL);
				curL++;
				i++;
			}

			firstPrintedLine = curL;
			i = 0;
			while (i < visibleLines && curL < buffer.LineCount) {
				PrintedLines.Add (buffer [curL]);

				if (buffer [curL].IsFolded)
					curL = buffer.GetEndNodeIndex (curL);

				curL++;
				i++;
			}

			buffer.editMutex.ExitReadLock ();
			editorMutex.ExitWriteLock ();
		}
		void updateOnScreenCurLineFromBuffCurLine(){
			printedCurrentLine = PrintedLines.IndexOf (buffer.CurrentCodeLine);
		}
		void toogleFolding (int line) {
			if (parser == null || !foldingEnabled)
				return;
			buffer.ToogleFolding (line);
		}

		#region Editor overrides
		protected override void updateEditorFromProjFile ()
		{
			Debug.WriteLine("\t\tSourceEditor updateEditorFromProjFile");

			buffer.editMutex.EnterWriteLock ();
			loadSource ();
			buffer.editMutex.ExitWriteLock ();

			isDirty = false;
			oldSource = projFile.Source;
			CurrentLine = requestedLine;
			CurrentColumn = requestedCol;
			projFile.RegisteredEditors [this] = true;
		}
		protected override void updateProjFileFromEditor ()
		{
			Debug.WriteLine("\t\tSourceEditor updateProjFileFromEditor");

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

			buffer.longestLineCharCount = 0;
			buffer.longestLineIdx = 0;
			measureLeftMargin ();
			MaxScrollX = MaxScrollY = 0;
			PrintedLines = null;
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;

			editorMutex.ExitWriteLock ();
		}
		void Buffer_LineAdditionEvent (object sender, CodeBufferEventArgs e)
		{
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				int charCount = buffer[lptr].PrintableLength;
				if (charCount > buffer.longestLineCharCount) {
					buffer.longestLineIdx = lptr;
					buffer.longestLineCharCount = charCount;
				}else if (lptr <= buffer.longestLineIdx)
					buffer.longestLineIdx++;
				if (parser == null)
					continue;
				parser.TryParseBufferLine (e.LineStart + i);
			}

			if (parser != null)
				parser.reparseSource ();

			measureLeftMargin ();

			updatePrintedLines ();
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_LineRemoveEvent (object sender, CodeBufferEventArgs e)
		{
			bool trigFindLongestLine = false;
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				if (lptr <= buffer.longestLineIdx)
					trigFindLongestLine = true;
			}
			if (trigFindLongestLine)
				findLongestLineAndUpdateMaxScrollX ();

			measureLeftMargin ();
			updatePrintedLines ();
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_LineUpadateEvent (object sender, CodeBufferEventArgs e)
		{
			bool trigFindLongestLine = false;
			for (int i = 0; i < e.LineCount; i++) {

				int lptr = e.LineStart + i;
				if (lptr == buffer.longestLineIdx)
					trigFindLongestLine = true;
				else if (buffer[lptr].PrintableLength > buffer.longestLineCharCount) {
					buffer.longestLineCharCount = buffer[lptr].PrintableLength;
					buffer.longestLineIdx = lptr;
				}
			}
			if (trigFindLongestLine)
				findLongestLineAndUpdateMaxScrollX ();
			
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_PositionChanged (object sender, EventArgs e)
		{
			//Console.WriteLine ("Position changes: ({0},{1})", buffer.CurrentLine, buffer.CurrentColumn);
			int cc = buffer.CurrentTabulatedColumn;

			if (cc > visibleColumns + ScrollX) {
				ScrollX = cc - visibleColumns;
			} else if (cc < ScrollX)
				ScrollX = cc;
			
			RegisterForGraphicUpdate ();
			updateOnScreenCurLineFromBuffCurLine ();
			notifyPositionChanged ();
		}

		void Buffer_SelectionChanged (object sender, EventArgs e)
		{
			RegisterForGraphicUpdate ();
		}
		void Buffer_FoldingEvent (object sender, CodeBufferEventArgs e)
		{
			updatePrintedLines ();
			updateOnScreenCurLineFromBuffCurLine ();
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
		}
		#endregion

		void notifyPositionChanged (){
			try {				
				NotifyValueChanged ("CurrentLine", buffer.CurrentLine+1);
				NotifyValueChanged ("CurrentColumn", buffer.CurrentColumn+1);
				NotifyValueChanged ("CurrentLineHasError", CurrentLineHasError);
				NotifyValueChanged ("CurrentLineError", CurrentLineError);
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
					l = buffer.CurrentLine; //reaffect from buffer where bound check is made
					if ((bool)buffer [l]?.IsFolded)
						buffer.ToogleFolding (l);					
				} catch (Exception ex) {
					requestedLine = value - 1;
					Console.WriteLine ("Error cur column: " + ex);
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
					requestedCol = value - 1;
					Console.WriteLine ("Error cur column: " + ex.ToString ());
				}
			}
		}
		public bool PrintLineNumbers
		{
			get { return Configuration.Global.Get<bool> ("PrintLineNumbers"); }
			set	{
				if (PrintLineNumbers == value)
					return;
				Configuration.Global.Set ("PrintLineNumbers", value);
				NotifyValueChanged ("PrintLineNumbers", PrintLineNumbers);
				measureLeftMargin ();
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue("SteelBlue")]
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
				base.ScrollY = value;
				updatePrintedLines ();
				updateOnScreenCurLineFromBuffCurLine ();
				RegisterForGraphicUpdate ();
			}
		}
		public ParserException CurrentLineError {
			get { return buffer?.CurrentCodeLine?.exception; }
		}
		public bool CurrentLineHasError {
			get { return buffer == null ? false : buffer.CurrentCodeLine == null ? false :
				buffer.CurrentCodeLine.exception != null; }
		}
		public override ProjectFile ProjectNode {
			get {
				return base.ProjectNode;
			}
			set {
				base.ProjectNode = value;
				if (projFile != null)
					parser = getParserFromExt (System.IO.Path.GetExtension (projFile.Extension));
			}
		}
		#endregion

		BufferParser getParserFromExt (string extension) {
			if (string.IsNullOrEmpty(extension))
				return null;
			if (!parsing.ContainsKey(extension))
				return null;
			Type parserType = Type.GetType (parsing [extension]);
			if (parserType == null)
				return null;
			return (BufferParser)Activator.CreateInstance (parserType, buffer );
		}
		void loadSource () {

			try {

				if (parser == null)
					buffer.Load (projFile.Source);
				else//parser may have special linebrk rules
					buffer.Load (projFile.Source, parser.LineBrkRegex);

			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
			}

			projFile.RegisteredEditors [this] = true;

			updateMaxScrollY ();
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);
			updatePrintedLines ();

			RegisterForGraphicUpdate ();
		}

		/// <summary>
		/// Current editor line, when set, update buffer.CurrentLine
		/// </summary>
		int PrintedCurrentLine {
			get { return printedCurrentLine;}
			set {
				if (value < 0) {
					ScrollY += value;
					printedCurrentLine = 0;
				} else if (PrintedLines.Count < visibleLines && value >= PrintedLines.Count) {
					printedCurrentLine = PrintedLines.Count - 1;
				}else if (value >= visibleLines) {
					ScrollY += value - visibleLines + 1;
					printedCurrentLine = visibleLines - 1;
				}else
					printedCurrentLine = value;
				//Debug.WriteLine ("printed current line:" + printedCurrentLine.ToString ());
				//update position in buffer
				buffer.CurrentLine = buffer.IndexOf (PrintedLines[printedCurrentLine]);
			}
		}
		int getTabulatedColumn (int col, int line) {
			return buffer [line].Content.Substring (0, col).Replace ("\t", new String (' ', Interface.TabSize)).Length;
		}
		int getTabulatedColumn (Point pos) {
			return getTabulatedColumn (pos.X,pos.Y);
		}
		/// <summary>
		/// Moves cursor one char to the left, move up if cursor reaches start of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			if (buffer.CurrentColumn == 0) {
				if (printedCurrentLine == 0)
					return false;
				PrintedCurrentLine--;
				buffer.CurrentColumn = int.MaxValue;
			} else
				buffer.CurrentColumn--;
			return true;
		}
		/// <summary>
		/// Moves cursor one char to the right, move down if cursor reaches end of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			if (buffer.CurrentColumn >= buffer.CurrentCodeLine.Length) {
				if (PrintedCurrentLine == buffer.UnfoldedLines - 1)
					return false;
				buffer.CurrentColumn = 0;
				PrintedCurrentLine++;
			} else
				buffer.CurrentColumn++;
			return true;
		}

		#region Drawing
		void drawLine(Context gr, Rectangle cb, int i) {
			CodeLine cl = PrintedLines[i];
			int lineIndex = buffer.IndexOf(cl);

			double y = cb.Y + (fe.Ascent+fe.Descent) * i, x = cb.X;

			//Draw line numbering
			Color mgFg = Color.Grey;
			Color mgBg = Color.White;
			if (PrintLineNumbers){
				Rectangle mgR = new Rectangle ((int)x, (int)y, leftMargin - leftMarginGap, (int)Math.Ceiling((fe.Ascent+fe.Descent)));
				if (cl.exception != null) {
					mgBg = Color.Red;
					if (buffer.CurrentLine == lineIndex)
						mgFg = Color.White;
					else
						mgFg = Color.LightGrey;
				}else if (buffer.CurrentLine == lineIndex) {
					mgFg = Color.Black;
					mgBg = Color.DarkGrey;
				}
				string strLN = (lineIndex+1).ToString ();
				gr.SetSourceColor (mgBg);
				gr.Rectangle (mgR);
				gr.Fill();
				gr.SetSourceColor (mgFg);

				gr.MoveTo (cb.X + (int)(gr.TextExtents (buffer.LineCount.ToString()).Width - gr.TextExtents (strLN).Width), y + fe.Ascent);
				gr.ShowText (strLN);
				gr.Fill ();
			}



			//draw folding
			if (foldingEnabled){

				Rectangle rFld = new Rectangle (cb.X + leftMargin - leftMarginGap - foldMargin,
					(int)(y + (fe.Ascent + fe.Descent) / 2.0 - foldSize / 2.0), foldSize, foldSize);

				gr.SetSourceColor (Color.Black);
				gr.LineWidth = 1.0;

				int level = 0;
				bool closingNode = false;

				if (currentNode != null) {
					if (cl == currentNode.EndLine) {
						currentNode = currentNode.Parent;
						closingNode = true;
					}
					if (currentNode != null)
						level = currentNode.Level - 1;
				}

				for (int l = 0; l < level; l++) {					
					gr.MoveTo (rFld.Center.X + 0.5, y);
					gr.LineTo (rFld.Center.X + 0.5, y + fe.Ascent + fe.Descent);
					rFld.Left += foldHSpace;
				}
				if (closingNode) {
					gr.MoveTo (rFld.Center.X + 0.5, y);
					gr.LineTo (rFld.Center.X + 0.5, y + fe.Ascent / 2 + 0.5);
					gr.LineTo (rFld.Center.X + 0.5 + foldSize / 2, y + fe.Ascent / 2 + 0.5);
					closingNode = false;
				}
				gr.SetDash (new double[]{ 1.5 },0.0);
				gr.SetSourceColor (Color.Grey);
				gr.Stroke ();
				gr.SetDash (new double[]{}, 0.0);

				if (cl.IsFoldable) {
					gr.Rectangle (rFld);
					gr.SetSourceColor (Color.White);
					gr.Fill();
					gr.SetSourceColor (Color.Black);
					gr.Rectangle (rFld, 1.0);
					if (cl.IsFolded) {
						gr.MoveTo (rFld.Center.X + 0.5, rFld.Y + 2);
						gr.LineTo (rFld.Center.X + 0.5, rFld.Bottom - 2);
					}else
						currentNode = cl.SyntacticNode;
					
					gr.MoveTo (rFld.Left + 2, rFld.Center.Y + 0.5);
					gr.LineTo (rFld.Right - 2, rFld.Center.Y + 0.5);
					gr.Stroke ();
				} 
			}

			gr.SetSourceColor (Foreground);
			x += leftMargin;

			if (cl.Tokens == null)
				drawRawCodeLine (gr, x, y, i, lineIndex);
			else
				drawParsedCodeLine (gr, x, y, i, lineIndex);
		}
		Node currentNode = null;
//		void drawParsed(Context gr){
//			if (PrintedLines == null)
//				return;
//
//			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
//			gr.SetFontSize (Font.Size);
//			gr.FontOptions = Interface.FontRenderingOptions;
//			gr.Antialias = Interface.Antialias;
//
//			Rectangle cb = ClientRectangle;
//			gr.Save ();
//			CairoHelpers.CairoRectangle (gr, cb, CornerRadius);
//			gr.Clip ();
//
//			bool selectionInProgress = false;
//
//			Foreground.SetAsSource (gr);
//
//			#region draw text cursor
//			if (SelBegin != SelRelease)
//				selectionInProgress = true;
//			else if (HasFocus){
//				gr.LineWidth = 1.0;
//				double cursorX = + leftMargin + cb.X + (CurrentColumn - ScrollX) * fe.MaxXAdvance;
//				gr.MoveTo (0.5 + cursorX, cb.Y + printedCurrentLine * (fe.Ascent+fe.Descent));
//				gr.LineTo (0.5 + cursorX, cb.Y + (printedCurrentLine + 1) * (fe.Ascent+fe.Descent));
//				gr.Stroke();
//			}
//			#endregion
//
//			for (int i = 0; i < PrintedLines.Count; i++)
//				drawTokenLine (gr, i, selectionInProgress, cb);
//
//			gr.Restore ();
//		}
		void drawRawCodeLine(Context gr, double x, double y, int i, int lineIndex) {
			string lstr = buffer[lineIndex].PrintableContent;
			if (ScrollX < lstr.Length)
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

				//System.Diagnostics.Debug.WriteLine ("sel start: " + buffer.SelectionStart + " sel end: " + buffer.SelectionEnd);
				if (lineIndex == buffer.SelectionStart.Y) {
					rLineX += (selStartCol - ScrollX) * fe.MaxXAdvance;
					rLineW -= selStartCol * fe.MaxXAdvance;
				}
				if (lineIndex == buffer.SelectionEnd.Y)
					rLineW -= (lstr.Length - selEndCol) * fe.MaxXAdvance;

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
		void drawParsedCodeLine (Context gr, double x, double y, int i, int lineIndex) {
			int lPtr = 0;
			CodeLine cl = PrintedLines[i];

			for (int t = 0; t < cl.Tokens.Count; t++) {
				string lstr = cl.Tokens [t].PrintableContent;
				if (lPtr < ScrollX) {
					if (lPtr - ScrollX + lstr.Length <= 0) {
						lPtr += lstr.Length;
						continue;
					}
					lstr = lstr.Substring (ScrollX - lPtr);
					lPtr += ScrollX - lPtr;
				}
				Color bg = this.Background;
				Color fg = this.Foreground;
				Color selbg = this.SelectionBackground;
				Color selfg = this.SelectionForeground;
				FontSlant fts = FontSlant.Normal;
				FontWeight ftw = FontWeight.Normal;

				if (formatting.ContainsKey ((int)cl.Tokens [t].Type)) {
					TextFormatting tf = formatting [(int)cl.Tokens [t].Type];
					bg = tf.Background;
					fg = tf.Foreground;
					if (tf.Bold)
						ftw = FontWeight.Bold;
					if (tf.Italic)
						fts = FontSlant.Italic;
				}

				gr.SelectFontFace (Font.Name, fts, ftw);
				gr.SetSourceColor (fg);

				gr.MoveTo (x, y + fe.Ascent);
				gr.ShowText (lstr);
				gr.Fill ();

				if (buffer.SelectionInProgress && lineIndex >= buffer.SelectionStart.Y && lineIndex <= buffer.SelectionEnd.Y &&
					!(lineIndex == buffer.SelectionStart.Y && lPtr + lstr.Length <= selStartCol) &&
					!(lineIndex == buffer.SelectionEnd.Y && selEndCol <= lPtr)) {

					double rLineX = x,
					rLineY = y,
					rLineW = lstr.Length * fe.MaxXAdvance;
					double startAdjust = 0.0;

					if ((lineIndex == buffer.SelectionStart.Y) && (selStartCol < lPtr + lstr.Length) && (selStartCol > lPtr))
						startAdjust = (selStartCol - lPtr) * fe.MaxXAdvance;
					rLineX += startAdjust;
					if ((lineIndex == buffer.SelectionEnd.Y) && (selEndCol < lPtr + lstr.Length))
						rLineW = (selEndCol - lPtr) * fe.MaxXAdvance;
					rLineW -= startAdjust;

					gr.Save ();
					gr.Operator = Operator.Source;
					gr.Rectangle (rLineX, rLineY, rLineW, (fe.Ascent+fe.Descent));
					gr.SetSourceColor (selbg);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (selfg);
					gr.MoveTo (x, y + fe.Ascent);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}
				x += (int)lstr.Length * fe.MaxXAdvance;
				lPtr += lstr.Length;
			}
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

			return (int)(fe.MaxXAdvance * buffer.longestLineCharCount) + Margin * 2 + leftMargin;
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
			}else if (HasFocus && printedCurrentLine >= 0){
				gr.LineWidth = 1.0;
				double cursorX = cb.X + (getTabulatedColumn(buffer.CurrentPosition) - ScrollX) * fe.MaxXAdvance + leftMargin;
				gr.MoveTo (0.5 + cursorX, cb.Y + (printedCurrentLine) * (fe.Ascent+fe.Descent));
				gr.LineTo (0.5 + cursorX, cb.Y + (printedCurrentLine + 1) * (fe.Ascent+fe.Descent));
				gr.Stroke();
			}
			#endregion

			if (PrintedLines?.Count > 0) {				
				int unfoldedLines = buffer.UnfoldedLines;
				currentNode = null;
				CodeLine cl = PrintedLines[0];
				int idx0 = buffer.IndexOf(cl);
				int li = idx0-1;
				while (li >= 0) {
					if (buffer [li].IsFoldable && !buffer [li].IsFolded) {
						if (buffer.IndexOf(buffer [li].SyntacticNode.EndLine) > idx0){
							currentNode = buffer [li].SyntacticNode;
							break;
						}
					}
					li--;
				}

				for (int i = 0; i < visibleLines; i++) {
					if (i + ScrollY >= unfoldedLines)//TODO:need optimize
						break;
					drawLine (gr, cb, i);
				}
			}

			editorMutex.ExitReadLock ();

			buffer.editMutex.ExitReadLock ();

		}
		#endregion

		#region Mouse handling

		int hoverLine = -1;
		public int HoverLine {
			get { return hoverLine; }
			set { 
				if (hoverLine == value)
					return;
				hoverLine = value;
				NotifyValueChanged ("HoverLine", hoverLine);
				NotifyValueChanged ("HoverError", buffer [hoverLine].exception);
			}
		}
		void updateHoverLine () {
			int hvl = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)));
			hvl = Math.Min (PrintedLines.Count-1, hvl);
			HoverLine = buffer.IndexOf (PrintedLines[hvl]);
		}
		void updateCurrentPosFromMouseLocalPos(){			
			PrintedCurrentLine = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)));
			int curVisualCol = ScrollX +  (int)Math.Round ((mouseLocalPos.X - leftMargin) / fe.MaxXAdvance);

			int i = 0;
			int buffCol = 0;
			while (i < curVisualCol && buffCol < buffer.CurrentCodeLine.Length) {
				if (buffer.CurrentCodeLine[buffCol] == '\t')
					i += Interface.TabSize;
				else
					i++;
				buffCol++;
			}
			buffer.CurrentColumn = buffCol;
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if (e.X - ScreenCoordinates(Slot).X < leftMargin + ClientRectangle.X)
				IFace.MouseCursor = XCursor.Default;
			else
				IFace.MouseCursor = XCursor.Text;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			IFace.MouseCursor = XCursor.Default;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			mouseLocalPos = e.Position - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;

			updateHoverLine ();

			if (!e.Mouse.IsButtonDown (MouseButton.Left)) {
				if (mouseLocalPos.X < leftMargin)
					IFace.MouseCursor = XCursor.Default;
				else
					IFace.MouseCursor = XCursor.Text;
				return;
			}

			if (!HasFocus || !buffer.SelectionInProgress)
				return;

			//mouse is down
			updateCurrentPosFromMouseLocalPos();
			buffer.SetSelEndPos ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (!this.Focusable)
				return;

			if (mouseLocalPos.X >= leftMargin)
				base.onMouseDown (sender, e);

			if (doubleClicked) {
				doubleClicked = false;
				return;
			}

			if (mouseLocalPos.X < leftMargin) {
				toogleFolding (buffer.IndexOf (PrintedLines [(int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)))]));
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
			doubleClicked = true;
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

			if (e.Control) {
				switch (key) {
				case Key.S:
					projFile.Save ();
					break;
				case Key.W:
					editorMutex.EnterWriteLock ();
					if (e.Shift)
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
			case Key.Back:
				buffer.DeleteChar ();
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (buffer.SelectionIsEmpty)
					MoveRight ();
				else if (e.Shift)
					IFace.Clipboard = buffer.SelectedText;
				buffer.DeleteChar ();
				break;
			case Key.Enter:
			case Key.KeypadEnter:
				if (!buffer.SelectionIsEmpty)
					buffer.DeleteChar ();
				buffer.InsertLineBreak ();
				break;
			case Key.Escape:
				buffer.ResetSelection ();
				break;
			case Key.Home:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (e.Control)
						buffer.CurrentLine = 0;
					buffer.CurrentColumn = 0;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (e.Control)
					buffer.CurrentLine = 0;
				buffer.CurrentColumn = 0;
				break;
			case Key.End:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (e.Control)
						buffer.CurrentLine = int.MaxValue;
					buffer.CurrentColumn = int.MaxValue;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (e.Control)
					buffer.CurrentLine = int.MaxValue;
				buffer.CurrentColumn = int.MaxValue;
				break;
			case Key.Insert:
				if (e.Shift)
					buffer.Insert (IFace.Clipboard);
				else if (e.Control && !buffer.SelectionIsEmpty)
					IFace.Clipboard = buffer.SelectedText;
				break;
			case Key.Left:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (e.Control)
						buffer.GotoWordStart ();
					else
						MoveLeft ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (e.Control)
					buffer.GotoWordStart ();
				else
					MoveLeft();
				break;
			case Key.Right:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (e.Control)
						buffer.GotoWordEnd ();
					else
						MoveRight ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (e.Control)
					buffer.GotoWordEnd ();
				else
					MoveRight ();
				break;
			case Key.Up:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine--;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine--;
				break;
			case Key.Down:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine++;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine++;
				break;
			case Key.Menu:
				break;
			case Key.NumLock:
				break;
			case Key.PageDown:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine += visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine += visibleLines;
				break;
			case Key.PageUp:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine -= visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine -= visibleLines;
				break;
			case Key.RWin:
				break;
			case Key.Tab:
				if (e.Shift) {
					if (buffer.SelectionIsEmpty ||
						(buffer.SelectionStart.Y == buffer.SelectionEnd.Y)) {
						//TODO
						break;
					}
					for (int i = buffer.SelectionStart.Y; i <= buffer.SelectionEnd.Y; i++)
						buffer.RemoveLeadingTab (i);
					buffer.SetSelectionOnFullLines ();
				} else {
					if (buffer.SelectionIsEmpty ||
						(buffer.SelectionStart.Y == buffer.SelectionEnd.Y)) {
						buffer.Insert ("\t");
						break;
					}
					for (int i = buffer.SelectionStart.Y; i <= buffer.SelectionEnd.Y; i++) {
						buffer.UpdateLine (i, "\t" + buffer [i].Content);
					}
				}

				break;
			case Key.F8:
				toogleFolding (buffer.CurrentLine);
				break;
			default:
				break;
			}
			RegisterForGraphicUpdate();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			buffer.Insert (e.KeyChar.ToString());
			buffer.ResetSelection ();
		}
		#endregion
	}
}