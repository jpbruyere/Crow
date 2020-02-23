// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using Crow.Cairo;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Crow.Coding
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class RoslynEditor : Editor
	{
		internal Dictionary<string, TextFormatting> formatting = new Dictionary<string, TextFormatting> ();

		#region CTOR
		public RoslynEditor (): base()
		{
			formatting ["default"] = new TextFormatting (Color.Jet, Color.Transparent);
			formatting ["TypeSyntax"] = new TextFormatting (Color.DarkCyan, Color.Transparent);
			formatting ["DocumentationCommentTrivia"] = new TextFormatting (Color.GreenYellow, Color.Transparent);
			formatting ["DisabledTextTrivia"] = new TextFormatting (Color.Grey, Color.Transparent);
			formatting ["Trivia"] = new TextFormatting (Color.Green, Color.Transparent);
			formatting ["Punctuation"] = new TextFormatting (Color.Black, Color.Transparent, false);
			formatting ["Name"] = new TextFormatting (Color.Jet, Color.Transparent);
			formatting ["LiteralExpression"] = new TextFormatting (Color.FireBrick, Color.Transparent, false, true);
			formatting ["PredefinedType"] = new TextFormatting (Color.DarkCyan, Color.Transparent, false);
			formatting ["PrimaryFunction"] = new TextFormatting (Color.SteelBlue, Color.Transparent, true);
			formatting ["ContextualKeyword"] = new TextFormatting (Color.DarkBlue, Color.Transparent, true);
			formatting ["keyword"] = new TextFormatting (Color.Blue, Color.Transparent);
			formatting ["GlobalMemberDeclaration"] = new TextFormatting (Color.Red, Color.Transparent);
			formatting ["InstanceExpression"] = new TextFormatting (Color.Jet, Color.Transparent);
			formatting ["InstanceExpression"] = new TextFormatting (Color.Jet, Color.Transparent);
			formatting ["NamespaceMemberDeclaration"] = new TextFormatting (Color.Jet, Color.Transparent);
			formatting ["PreprocessorDirective"] = new TextFormatting (Color.DeepPink, Color.Transparent, true);
			formatting ["TypeDeclaration"] = new TextFormatting (Color.Lavender, Color.Transparent);

			/*formatting ["constant"] = new TextFormatting (Color.Blue, Color.Transparent, true);
			formatting ["primitive"] = new TextFormatting (Color.DarkCyan, Color.Transparent);
			formatting ["operator"] = new TextFormatting (Color.DarkRed, Color.Transparent, true);
			formatting ["modifier"] = new TextFormatting (Color.RoyalBlue, Color.Transparent);
			formatting ["typekind"] = new TextFormatting (Color.OliveDrab, Color.Transparent);
			formatting ["async"] = new TextFormatting (Color.YellowGreen, Color.Transparent);
			formatting ["linq"] = new TextFormatting (Color.Yellow, Color.Transparent);
			formatting ["preproc"] = new TextFormatting (Color.DarkOrange, Color.Transparent, true);
			formatting ["comment"] = new TextFormatting (Color.Green, Color.Transparent, false, true);

			formatting ["PredefinedType"] = new TextFormatting (Color.Red, Color.Transparent, true);
			formatting ["identifier"] = new TextFormatting (Color.Jet, Color.Transparent, true);
			formatting ["litteral"] = new TextFormatting (Color.Crimson, Color.Transparent, false, true);*/

			/*formatting.Add ((int)XMLParser.TokenType.AttributeName, new TextFormatting (Color.DarkSlateGrey, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.ElementName, new TextFormatting (Color.DarkBlue, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.ElementStart, new TextFormatting (Color.Black, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.ElementEnd, new TextFormatting (Color.Black, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.ElementClosing, new TextFormatting (Color.Black, Color.Transparent));

			formatting.Add ((int)XMLParser.TokenType.AttributeValueOpening, new TextFormatting (Color.Crimson, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.AttributeValueClosing, new TextFormatting (Color.Crimson, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.AttributeValue, new TextFormatting (Color.FireBrick, Color.Transparent, false, true));
			formatting.Add ((int)XMLParser.TokenType.XMLDecl, new TextFormatting (Color.ForestGreen, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.Content, new TextFormatting (Color.DimGrey, Color.Transparent, false, true));

			formatting.Add ((int)BufferParser.TokenType.BlockComment, new TextFormatting (Color.Grey, Color.Transparent, false, true));
			formatting.Add ((int)BufferParser.TokenType.LineComment, new TextFormatting (Color.Grey, Color.Transparent, false, true));
			formatting.Add ((int)BufferParser.TokenType.OperatorOrPunctuation, new TextFormatting (Color.Black, Color.Transparent));
			formatting.Add ((int)8300, new TextFormatting (Color.Teal, Color.Transparent));*/
		}
		#endregion

		#region private and protected fields

		int tabSize = 4;
		string oldSource = "";
		//save requested position on error, and try it on next move
		int requestedLine = 0, requestedCol = 0;
		volatile bool isDirty = false;

		internal const int leftMarginGap = 3;	//gap between items in margin and text
		const int foldSize = 9;					//folding rectangles size
		int foldMargin = 9;						// { get { return parser == null ? 0 : parser.SyntacticTreeMaxDepth * foldHSpace; }}//folding margin size

		bool foldingEnabled = true;
		[XmlIgnore]
		public int leftMargin { get; private set; } = 0;	//margin used to display line numbers, folding errors,etc...
		int visibleLines = 1;
		int visibleColumns = 1;
		int printedCurrentLine = 0;				//Index of the currentline in the PrintedLines array
		int [] printedLines; 					//printed line indices in source


		internal int hoverPos, currentPos, selStartPos;//absolute char index in buffer source
		TextSpan selection = default;
		SourceText buffer = SourceText.From ("");
		SyntaxTree syntaxTree;

		//SourceText buffer => syntaxTree == null ?  : syntaxTree.TryGetText (out SourceText src) ? src : SourceText.From ("");
		public SyntaxTree SyntaxTree {
			get => syntaxTree;
			private set {
				if (syntaxTree == value)
					return;
				syntaxTree = value;
				CSProjectItem cspi = ProjectNode as CSProjectItem;
				if (cspi != null)
					cspi.SyntaxTree = syntaxTree;
				NotifyValueChanged ("SyntaxTree", syntaxTree);
			}
		}

		//absolute char pos in text of start of folds
		List<int> folds = new List<int> ();


		//Dictionary<int, TextFormatting> formatting = new Dictionary<int, TextFormatting>();

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

		internal void measureLeftMargin () {
			leftMargin = 0;
			if (printLineNumbers)
				leftMargin += (int)Math.Ceiling((double)buffer?.Lines.Count.ToString().Length * fe.MaxXAdvance) + 6;
			if (foldingEnabled)
				leftMargin += foldMargin;
			if (leftMargin > 0)
				leftMargin += leftMarginGap;
		}

		int longestLineCharCount = 0, longestLineIdx = 0;

		void findLongestLineAndUpdateMaxScrollX() {
			longestLineCharCount = 0;
			longestLineIdx = 0;
			for (int i = 0; i < buffer.Lines.Count; i++) {
				TextLine tl = buffer.Lines [i];
				int length = tl.ToString ().TabulatedText (tabSize).Length;
				if (length <= longestLineCharCount)
					continue;
				longestLineCharCount = length;
				longestLineIdx = i;
			}
			updateMaxScrollX ();

//			Debug.WriteLine ("SourceEditor: Find Longest line and update maxscrollx: {0} visible cols:{1}", MaxScrollX, visibleColumns);
		}
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
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width - leftMargin)/ fe.MaxXAdvance);
			NotifyValueChanged ("VisibleColumns", visibleColumns);
			updateMaxScrollX ();
//			System.Diagnostics.Debug.WriteLine ("update visible columns: {0} leftMargin:{1}",visibleColumns, leftMargin);
//			System.Diagnostics.Debug.WriteLine ("update MaxScrollX: " + MaxScrollX);
		}
		void updateMaxScrollX () {
			MaxScrollX = Math.Max (0, longestLineCharCount - visibleColumns);
			if (longestLineCharCount > 0)
				NotifyValueChanged ("ChildWidthRatio", Slot.Width * visibleColumns / longestLineCharCount);			
		}
		void updateMaxScrollY () {
			/*if (parser == null || !foldingEnabled) {
				MaxScrollY = Math.Max (0, buffer.Lines.Count - visibleLines);
				if (buffer.UnfoldedLines > 0)
					NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / buffer.UnfoldedLines);							
			} else {
				MaxScrollY = Math.Max (0, buffer.UnfoldedLines - visibleLines);
				if (buffer.UnfoldedLines > 0)
					NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / buffer.UnfoldedLines);							
			}*/

			MaxScrollY = Math.Max (0, buffer.Lines.Count - visibleLines);
		}			
		//void updatePrintedLines () {
			//PrintedLines = buffer.Lines;
			/*buffer.editMutex.EnterReadLock ();
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
			editorMutex.ExitWriteLock ();*/
		//}
		void toogleFolding (int line) {
			/*if (parser == null || !foldingEnabled)
				return;
			buffer.ToogleFolding (line);*/
		}

		#region Editor overrides
		protected override void updateEditorFromProjFile ()
		{
			Debug.WriteLine("\t\tSourceEditor updateEditorFromProjFile");

			//buffer.editMutex.EnterWriteLock ();
			loadSource ();
			//buffer.editMutex.ExitWriteLock ();

			isDirty = false;
			oldSource = projFile.Source;
			CurrentLine = requestedLine;
			CurrentColumn = requestedCol;
			//projFile.RegisteredEditors [this] = true;
		}
		protected override void updateProjFileFromEditor ()
		{
			Debug.WriteLine("\t\tSourceEditor updateProjFileFromEditor");

			char[] chars = new char [buffer.Length];
			buffer.CopyTo (0, chars, 0, buffer.Length);
			projFile.UpdateSource (this, new string(chars));
		}
		protected override bool EditorIsDirty {
			get { return isDirty; }
			set { isDirty = value; }
		}
		protected override bool IsReady {
			get { return projFile != null; }
		}
		#endregion

		#region Buffer events handlers
		void Buffer_BufferCleared (object sender, EventArgs e)
		{
			editorMutex.EnterWriteLock ();

			longestLineCharCount = 0;
			longestLineIdx = 0;
			measureLeftMargin ();
			MaxScrollX = MaxScrollY = 0;

			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;

			editorMutex.ExitWriteLock ();
		}
		void Buffer_LineAdditionEvent (object sender, CodeBufferEventArgs e)
		{
			/*for (int i = 0; i < e.LineCount; i++) {
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
				parser.reparseSource ();*/

			measureLeftMargin ();


			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_LineRemoveEvent (object sender, CodeBufferEventArgs e)
		{
			/*bool trigFindLongestLine = false;
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				if (lptr <= buffer.longestLineIdx)
					trigFindLongestLine = true;
			}
			if (trigFindLongestLine)
				findLongestLineAndUpdateMaxScrollX ();*/

			measureLeftMargin ();

			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_LineUpadateEvent (object sender, CodeBufferEventArgs e)
		{
			/*bool trigFindLongestLine = false;
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
				findLongestLineAndUpdateMaxScrollX ();*/
			
			RegisterForGraphicUpdate ();
			notifyPositionChanged ();
			isDirty = true;
		}
		void Buffer_PositionChanged (object sender, EventArgs e)
		{
			//Console.WriteLine ("Position changes: ({0},{1})", buffer.CurrentLine, buffer.CurrentColumn);
			/*int cc = buffer.CurrentTabulatedColumn;

			if (cc > visibleColumns + ScrollX) {
				ScrollX = cc - visibleColumns;
			} else if (cc < ScrollX)
				ScrollX = cc;
			
			RegisterForGraphicUpdate ();
			updateOnScreenCurLineFromBuffCurLine ();
			notifyPositionChanged ();*/
		}

		void Buffer_SelectionChanged (object sender, EventArgs e)
		{
			RegisterForGraphicUpdate ();
		}
		void Buffer_FoldingEvent (object sender, CodeBufferEventArgs e)
		{

			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
		}
		#endregion

		void notifyPositionChanged (){
			/*try {				
				NotifyValueChanged ("CurrentLine", CurrentLine+1);
				NotifyValueChanged ("CurrentColumn", buffer.CurrentColumn+1);
				NotifyValueChanged ("CurrentLineHasError", CurrentLineHasError);
				NotifyValueChanged ("CurrentLineError", CurrentLineError);
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}*/
		}

		int currentLine, currentColumn;	
		#region Public Crow Properties
		public int CurrentLine{
			get { return currentLine; }
			set {
				if (currentLine == value)
					return;
				currentLine = value;
				NotifyValueChanged ("CurrentLine", currentLine);
				RegisterForRedraw ();
			}
		}
		public int CurrentColumn{
			get { return currentColumn; }
			set {
				if (currentColumn == value)
					return;
				currentColumn = value;
				NotifyValueChanged ("CurrentColumn", currentColumn);
				RegisterForRedraw ();
			}
		}
		internal bool printLineNumbers => (this.IFace as CrowIDE).PrintLineNumbers;

		[DefaultValue("CornflowerBlue")]
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
		public ParserException CurrentLineError {
			get { return null; }// buffer?.CurrentCodeLine?.exception; }
		}
		public bool CurrentLineHasError {
			get { return false; }
		}
		#endregion


		void loadSource () {

			try {
				buffer = SourceText.From (projFile.Source);
				SyntaxTree = CSharpSyntaxTree.ParseText (buffer);
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
			}

			//projFile.RegisteredEditors [this] = true;

			updateMaxScrollY ();
			measureLeftMargin ();
			findLongestLineAndUpdateMaxScrollX ();


			RegisterForGraphicUpdate ();
		}

		int getTabulatedColumn (int col, int line) {
			return 0; //buffer [line].Content.Substring (0, col).Replace ("\t", new String (' ', Interface.TAB_SIZE)).Length;
		}
		int getTabulatedColumn (Point pos) {
			return getTabulatedColumn (pos.X,pos.Y);
		}

		void move (bool shiftIsDown, int hDelta, int vDelta = 0)
		{
			if (shiftIsDown) {
				if (selection.IsEmpty)
					selStartPos = currentPos;
				/*if (IFace.Ctrl)
					buffer.GotoWordStart ();
				else*/
				move (hDelta, vDelta);
				selection = (selStartPos < currentPos) ?
					TextSpan.FromBounds (selStartPos, currentPos) :
					TextSpan.FromBounds (currentPos, selStartPos);
				return;
			}
			selection = default;
			move (hDelta, vDelta);
		}
		void move (int hDelta, int vDelta)
		{
			if (buffer == null)
				return;

			if (hDelta != 0) {
				currentPos += hDelta;
				if (currentPos < 0)
					currentPos = 0;
				else if (currentPos >= buffer.Length)
					currentPos = buffer.Length - 1;
			}

			if (vDelta != 0) {
				LinePosition lp = buffer.Lines.GetLinePosition (currentPos);
				int nextL = lp.Line + vDelta;
				if (nextL < 0)
					nextL = 0;
				else if (nextL >= buffer.Lines.Count)
					nextL = buffer.Lines.Count - 1;

				if (nextL == lp.Line)
					return;

				string str = buffer.Lines [lp.Line].ToString ();
				int tabulatedColumn = str.Substring (0, lp.Character).TabulatedText (tabSize).Length;
				lp = new LinePosition (nextL, buffer.Lines [nextL].GetCharPosFromVisualColumn (tabulatedColumn, tabSize));
				currentPos = buffer.Lines.GetPosition (lp);
			}
		}

		#region GraphicObject overrides
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;
				using (Context gr = new Context (IFace.surf)) {
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);

					fe = gr.FontExtents;
				}				
				MaxScrollY = 0;
				RegisterForGraphicUpdate ();
			}
		}
		protected override int measureRawSize(LayoutingType lt)
		{
			if (lt == LayoutingType.Height)
				return (int)Math.Ceiling((fe.Ascent+fe.Descent) * buffer.Lines.Count) + Margin * 2;

			return (int)(fe.MaxXAdvance * longestLineCharCount) + Margin * 2 + leftMargin;
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
			//base.onDraw (gr);

			if (syntaxTree == null)
				return;

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Foreground.SetAsSource (gr);

			editorMutex.EnterReadLock ();

			syntaxTree = syntaxTree.WithChangedText (buffer);
			SyntaxNodePrinter printer = new SyntaxNodePrinter (this, gr, buffer.Lines.Count, ScrollY, visibleLines);
			printer.Visit (syntaxTree.GetRoot ());
			printedLines = printer.printedLinesNumbers;

			#region draw text cursor
			/*if (buffer.SelectionInProgress){
				selStartCol = getTabulatedColumn (buffer.SelectionStart);
				selEndCol = getTabulatedColumn (buffer.SelectionEnd);
			}else*/
			Rectangle cb = ClientRectangle;

			if (!selection.IsEmpty) {
				Color selbg = this.SelectionBackground;

				TextLine startTl = buffer.Lines.GetLineFromPosition (selection.Start);
				TextLine endTl = buffer.Lines.GetLineFromPosition (selection.End);

				if (endTl.LineNumber < ScrollY || startTl.LineNumber >= ScrollY + visibleLines) {
					editorMutex.ExitReadLock ();
					return;
				}
				int visualColStart = startTl.ToString ().Substring (0, selection.Start - startTl.Start).TabulatedText (tabSize).Length - ScrollX;
				int visualColEnd = endTl.ToString ().Substring (0, selection.End - endTl.Start).TabulatedText (tabSize).Length - ScrollX;

				int visualLineStart = Array.IndexOf (printedLines, startTl.LineNumber);
				double xStart = cb.X + visualColStart * fe.MaxXAdvance + leftMargin;
				double yStart = cb.Y + visualLineStart * (fe.Ascent + fe.Descent);
				RectangleD r = new RectangleD (xStart,
					yStart, (visualColEnd - visualColStart) * fe.MaxXAdvance, (fe.Ascent + fe.Descent));

				gr.Operator = Operator.DestOver;
				gr.SetSourceColor (selbg);

				if (startTl == endTl) {
					gr.Rectangle (r);
					gr.Fill ();
				}else {
					r.Width = Math.Min (cb.Width - xStart, (startTl.ToString ().TabulatedText (tabSize).Length - ScrollX - visualColStart) * fe.MaxXAdvance);
					gr.Rectangle (r);
					gr.Fill ();
					int visualLineEnd = Array.IndexOf (printedLines, endTl.LineNumber);
					r.Left = cb.X + leftMargin;
					for (int l = visualLineStart + 1; l < (visualLineEnd < 0 ? printedLines.Length : visualLineEnd); l++) {
						r.Top += (fe.Ascent + fe.Descent);
						TextLine tl = buffer.Lines [printedLines [l]];
						r.Width = Math.Min(cb.Width - leftMargin, (tl.ToString ().TabulatedText (tabSize).Length - ScrollX) * fe.MaxXAdvance);
						gr.Rectangle (r);
						gr.Fill ();
					}
					if (visualLineEnd >= 0) {
						r.Top += (fe.Ascent + fe.Descent);
						r.Width = Math.Min (cb.Width - leftMargin, visualColEnd * fe.MaxXAdvance);
						gr.Rectangle (r);
						gr.Fill ();
					}
				}

				gr.Operator = Operator.Over;

			} else if (HasFocus && printedLines != null && currentPos >= 0) {
				gr.LineWidth = 1.0;

				TextLine tl = buffer.Lines.GetLineFromPosition (currentPos);
				int visualCol = tl.ToString ().Substring (0, currentPos - tl.Start).TabulatedText (tabSize).Length - ScrollX;
				int visualLine = Array.IndexOf (printedLines, tl.LineNumber);
				double cursorX = cb.X + visualCol * fe.MaxXAdvance + leftMargin;
				gr.MoveTo (0.5 + cursorX, cb.Y + visualLine * (fe.Ascent + fe.Descent));
				gr.LineTo (0.5 + cursorX, cb.Y + (visualLine + 1) * (fe.Ascent + fe.Descent));
				gr.Stroke ();
			}
			#endregion
			editorMutex.ExitReadLock ();

		}
		#endregion

		#region Mouse handling

		int hoverLine = -1, hoverColumn = -1;
		public int HoverLine {
			get { return hoverLine; }
			set {
				if (hoverLine == value)
					return;
				hoverLine = value;
				NotifyValueChanged ("HoverLine", hoverLine);
				//NotifyValueChanged ("HoverError", buffer [hoverLine].exception);
			}
		}
		public int HoverColumn {
			get { return hoverColumn; }
			set {
				if (hoverColumn == value)
					return;
				hoverColumn = value;
				NotifyValueChanged ("HoverColumn", hoverColumn);
				//NotifyValueChanged ("HoverError", buffer [hoverLine].exception);
			}
		}

		void updateHoverPos ()
		{
			if (buffer == null || printedLines == null) {
				HoverLine = 0;
				HoverColumn = 0;
				return;
			}

			int hvl = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent + fe.Descent)));
			hvl = Math.Min (printedLines.Length - 1, hvl);

			HoverLine = printedLines [hvl];

			int curVisualCol = ScrollX + (int)Math.Round ((mouseLocalPos.X - leftMargin) / fe.MaxXAdvance);
			HoverColumn = buffer.Lines [hoverLine].GetCharPosFromVisualColumn (curVisualCol, tabSize);
		}

		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if (e.X - ScreenCoordinates(Slot).X < leftMargin + ClientRectangle.X)
				IFace.MouseCursor = MouseCursor.Arrow;
			else
				IFace.MouseCursor = MouseCursor.IBeam;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			IFace.MouseCursor = MouseCursor.Arrow;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			mouseLocalPos = e.Position - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;

			updateHoverPos ();

			if (hoverLine < 0 || hoverColumn < 0)
				hoverPos = -1;
			else
				hoverPos = buffer.Lines.GetPosition (new LinePosition (hoverLine, hoverColumn));

			if (e.Mouse.IsButtonDown (MouseButton.Left)) {
				if (hoverPos != selStartPos)
					selection = (selStartPos < hoverPos) ?
						TextSpan.FromBounds (selStartPos, hoverPos) :
						TextSpan.FromBounds (hoverPos, selStartPos);
				RegisterForRedraw ();
			} else {
				if (mouseLocalPos.X < leftMargin)
					IFace.MouseCursor = MouseCursor.Arrow;
				else
					IFace.MouseCursor = MouseCursor.IBeam;
				return;
			}

			/*if (!HasFocus || !buffer.SelectionInProgress)
				return;

			//mouse is down
			updateCurrentPosFromMouseLocalPos();
			buffer.SetSelEndPos ();*/
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (!Focusable)
				return;

			if (mouseLocalPos.X >= leftMargin)
				base.onMouseDown (sender, e);

			if (e.Handled)
				return;

			if (doubleClicked) {
				doubleClicked = false;
				return;
			}

			if (mouseLocalPos.X < leftMargin) {
				toogleFolding (hoverLine);
				//toogleFolding (buffer.IndexOf (PrintedLines [(int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)))]));
				return;
			}

			currentPos = selStartPos = hoverPos;
			RegisterForRedraw ();
			selection = default;
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			/*if (buffer.SelectionIsEmpty)
				buffer.ResetSelection ();*/
		}

		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			doubleClicked = true;
			base.onMouseDoubleClick (sender, e);

			/*buffer.GotoWordStart ();
			buffer.SetSelStartPos ();
			buffer.GotoWordEnd ();
			buffer.SetSelEndPos ();*/
		}
		public void MakeSelection (int lineStart, int colStart, int lineEnd, int colEnd) {
			/*buffer.CurrentLine = lineStart;
			buffer.CurrentColumn = colStart;
			buffer.SetSelStartPos ();
			buffer.CurrentLine = lineEnd;
			buffer.CurrentColumn = colEnd;
			buffer.SetSelEndPos ();*/
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
				case Key.z:
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

			switch (key) {
			case Key.BackSpace:
				if (selection.IsEmpty) {
					if (currentPos < 1)
						return;
					selection = TextSpan.FromBounds (currentPos - 1, currentPos);
				}
				replaceSelection ("");
				break;			
			case Key.Delete:
				if (selection.IsEmpty) {
					if (currentPos >= buffer.Length)
						return;
					selection = TextSpan.FromBounds (currentPos, currentPos + 1);
				} else if (IFace.Shift)
					IFace.Clipboard = buffer.GetSubText(selection).ToString().TabulatedText(tabSize);
				replaceSelection ("");
				break;
			case Key.Insert:
				if (selection.IsEmpty)
					selection = TextSpan.FromBounds (currentPos, currentPos);
				else if (IFace.Ctrl) {
					IFace.Clipboard = buffer.GetSubText (selection).ToString ().TabulatedText (tabSize);
					break;
				}				
				if (IFace.Shift)
					replaceSelection (IFace.Clipboard);
				break;
			case Key.Return:
			case Key.KP_Enter:
				if (!selection.IsEmpty)
					replaceSelection ("");
				selection = TextSpan.FromBounds (currentPos, currentPos);
				replaceSelection ("\n");
				break;
			case Key.Escape:
				selection = default;
				break;
			case Key.Home:
				if (IFace.Ctrl)
					move (IFace.Shift, -currentPos);
				else
					move (IFace.Shift, buffer.Lines.GetLineFromPosition (currentPos).Start - currentPos);
				break;
			case Key.End:
				if (IFace.Ctrl)
					move (IFace.Shift, buffer.Length - currentPos);
				else
					move (IFace.Shift, buffer.Lines.GetLineFromPosition (currentPos).End - currentPos);
				break;
			case Key.Left:
				move (IFace.Shift, -1);
				break;
			case Key.Right:
				move (IFace.Shift, 1);
				break;
			case Key.Up:
				move (IFace.Shift, 0, -1);
				break;
			case Key.Down:
				move (IFace.Shift, 0, 1);
				break;
			case Key.Page_Up:
				move (IFace.Shift, 0, -visibleLines);
				break;
			case Key.Page_Down:
				move (IFace.Shift, 0, visibleLines);
				break;
			case Key.Tab:
			case Key.ISO_Left_Tab:
				if (selection.IsEmpty)
					selection = TextSpan.FromBounds (currentPos, currentPos);
				LinePositionSpan lps = buffer.Lines.GetLinePositionSpan (selection);
				if (IFace.Shift) {
					for (int i = lps.Start.Line; i <= lps.End.Line; i++) {
						int pos = buffer.Lines [i].Start;
						int delta = 0;
						if (buffer [pos] == '\t')
							delta = 1;
						else {
							while (delta <= tabSize && buffer [pos + delta] == ' ')
								delta++;
						}
						if (delta > 0)
							buffer = buffer.Replace (TextSpan.FromBounds (pos, pos + delta), "");
					}
					selection = TextSpan.FromBounds (buffer.Lines [lps.Start.Line].Start, buffer.Lines [lps.End.Line].End);
					RegisterForRedraw ();
				} else {
					if (lps.Start.Line == lps.End.Line)
						replaceSelection ("\t");
					else {
						for (int i = lps.Start.Line; i <= lps.End.Line; i++) {
							int pos = buffer.Lines [i].Start;
							buffer = buffer.Replace (TextSpan.FromBounds (pos, pos), "\t");
						}
						selection = TextSpan.FromBounds (buffer.Lines [lps.Start.Line].Start, buffer.Lines [lps.End.Line].End);
						RegisterForRedraw ();
					}
				}
				break;
			//case Key.F8:
			//	toogleFolding (buffer.CurrentLine);
			//	break;
			//default:
				//break;
			}
			RegisterForGraphicUpdate ();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);
			if (selection.IsEmpty)
				selection = TextSpan.FromBounds (currentPos, currentPos);
			string str = e.KeyChar.ToString ();
			replaceSelection (str);
		}
		void replaceSelection (string newText)
		{
			buffer = buffer.WithChanges (new TextChange (selection, newText));
			if (string.IsNullOrEmpty (newText))
				currentPos = selection.Start;
			else
				currentPos = selection.Start + newText.Length;
			selection = default;
			RegisterForRedraw ();
		}
		#endregion
	}
}