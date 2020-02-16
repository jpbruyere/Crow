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

		int tabSize = 4;
		string oldSource = "";
		//save requested position on error, and try it on next move
		int requestedLine = 0, requestedCol = 0;
		volatile bool isDirty = false;

		internal const int leftMarginGap = 3;//gap between items in margin and text
		const int foldSize = 9;//folding rectangles size
		int foldMargin = 9;// { get { return parser == null ? 0 : parser.SyntacticTreeMaxDepth * foldHSpace; }}//folding margin size

		#region private and protected fields
		bool foldingEnabled = true;
		[XmlIgnore]
		public int leftMargin { get; private set; } = 0;	//margin used to display line numbers, folding errors,etc...
		int visibleLines = 1;
		int visibleColumns = 1;
		int printedCurrentLine = 0;//Index of the currentline in the PrintedLines array
		int [] printedLines; //printed line indices in source

		SourceText buffer => syntaxTree == null ? SourceText.From ("") : syntaxTree.TryGetText(out SourceText src) ? src : SourceText.From("");

		SyntaxTree syntaxTree;
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

		void measureLeftMargin () {
			leftMargin = 0;
			if (PrintLineNumbers)
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
				//if (tl.TabulatedText(tabSize).Length <= longestLineCharCount)
				//	continue;
				longestLineCharCount = tl.Span.Length;
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
		void updateOnScreenCurLineFromBuffCurLine(){
			//printedCurrentLine = PrintedLines.IndexOf (buffer.CurrentCodeLine);
		}
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

			updateOnScreenCurLineFromBuffCurLine ();
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

				updateOnScreenCurLineFromBuffCurLine ();
				RegisterForGraphicUpdate ();
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
				//buffer = SourceText.From (projFile.Source);
				SyntaxTree = CSharpSyntaxTree.ParseText (projFile.Source);
			} catch (Exception ex) {
				Debug.WriteLine (ex.ToString ());
			}

			//projFile.RegisteredEditors [this] = true;

			updateMaxScrollY ();

			measureLeftMargin ();
			findLongestLineAndUpdateMaxScrollX ();


			RegisterForGraphicUpdate ();
		}

		/// <summary>
		/// Current editor line, when set, update buffer.CurrentLine
		/// </summary>
		int PrintedCurrentLine {
			get { return printedCurrentLine;}
			set {
				/*if (value < 0) {
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
				buffer.CurrentLine = buffer.IndexOf (PrintedLines[printedCurrentLine]);*/
			}
		}
		int getTabulatedColumn (int col, int line) {
			return 0; //buffer [line].Content.Substring (0, col).Replace ("\t", new String (' ', Interface.TAB_SIZE)).Length;
		}
		int getTabulatedColumn (Point pos) {
			return getTabulatedColumn (pos.X,pos.Y);
		}
		/// <summary>
		/// Moves cursor one char to the left, move up if cursor reaches start of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			/*if (buffer.CurrentColumn == 0) {
				if (printedCurrentLine == 0)
					return false;
				PrintedCurrentLine--;
				buffer.CurrentColumn = int.MaxValue;
			} else
				buffer.CurrentColumn--;*/
			return true;
		}
		/// <summary>
		/// Moves cursor one char to the right, move down if cursor reaches end of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			/*if (buffer.CurrentColumn >= buffer.CurrentCodeLine.Length) {
				if (PrintedCurrentLine == buffer.UnfoldedLines - 1)
					return false;
				buffer.CurrentColumn = 0;
				PrintedCurrentLine++;
			} else
				buffer.CurrentColumn++;*/
			return true;
		}

		#region Drawing
		void drawLine(Context gr, Rectangle cb, int i) {
			TextLine tl = buffer.Lines[i];
			int lineIndex = tl.LineNumber;// buffer.Lines.(cl);
			
			double y = cb.Y + (fe.Ascent+fe.Descent) * i, x = cb.X;

			//Draw line numbering
			Color mgFg = Color.Jet;
			Color mgBg = Color.Grey;
			if (PrintLineNumbers){
				Rectangle mgR = new Rectangle ((int)x, (int)y, leftMargin - leftMarginGap, (int)Math.Ceiling((fe.Ascent+fe.Descent)));
				/*if (cl.exception != null) {
					mgBg = Color.Red;
					if (CurrentLine == lineIndex)
						mgFg = Color.White;
					else
						mgFg = Color.LightGrey;
				}else */
				if (CurrentLine == lineIndex && HasFocus) {
					mgFg = Color.Black;
					mgBg = Color.DarkGrey;
				}
				string strLN = (lineIndex+1).ToString ();
				gr.SetSourceColor (mgBg);
				gr.Rectangle (mgR);
				gr.Fill();
				gr.SetSourceColor (mgFg);

				gr.MoveTo (cb.X + (int)(gr.TextExtents (buffer.Lines.Count.ToString()).Width - gr.TextExtents (strLN).Width), y + fe.Ascent);
				gr.ShowText (strLN);
				gr.Fill ();
			}


			//draw folding
			/*if (foldingEnabled){

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


				if (level > 0) {
					gr.MoveTo (rFld.Center.X + 0.5, y);
					gr.LineTo (rFld.Center.X + 0.5, y + fe.Ascent + fe.Descent);
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
			}*/

			gr.SetSourceColor (Foreground);
			x += leftMargin;

			if (syntaxTree == null)
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
			//string lstr = buffer.Lines [lineIndex].TabulatedText (tabSize);
			//if (ScrollX < lstr.Length)
			//	lstr = lstr.Substring (ScrollX);
			//else
			//	lstr = "";

			//gr.MoveTo (x, y + fe.Ascent);
			//gr.ShowText (lstr);
			//gr.Fill ();

			/*if (!buffer.SelectionIsEmpty && lineIndex >= buffer.SelectionStart.Y && lineIndex <= buffer.SelectionEnd.Y) {
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
			}*/
		}
		void drawParsedCodeLine (Context gr, double x, double y, int i, int lineIndex) {
			int lPtr = 0;
			TextLine tl = buffer.Lines[i];
			SyntaxNode node = syntaxTree.GetRoot ();

			//node.ChildNodes
			node = node.FindNode (tl.SpanIncludingLineBreak);

			SyntaxToken tok = node.GetFirstToken (true, true, true, true);
			//tok.GetLocation ().GetLineSpan ().EndLinePosition;

			while (tok != default) {
				string lstr = tok.NormalizeWhitespace(new string(' ', tabSize)).ToFullString();
				if (lPtr < ScrollX) {
					if (lPtr - ScrollX + lstr.Length <= 0) {
						lPtr += lstr.Length;
						continue;
					}
					lstr = lstr.Substring (ScrollX - lPtr);
					lPtr += ScrollX - lPtr;
				}
				//Color bg = this.Background;
				//Color fg = this.Foreground;
				//Color selbg = this.SelectionBackground;
				//Color selfg = this.SelectionForeground;
				//FontSlant fts = FontSlant.Normal;
				//FontWeight ftw = FontWeight.Normal;

				//int tk = tok.RawKind & ~0xFF;
				//if (formatting.ContainsKey (tk)) {
				//	TextFormatting tf = formatting [tk];
				//	bg = tf.Background;
				//	fg = tf.Foreground;
				//	if (tf.Bold)
				//		ftw = FontWeight.Bold;
				//	if (tf.Italic)
				//		fts = FontSlant.Italic;
				//}

				//gr.SelectFontFace (Font.Name, fts, ftw);
				//gr.SetSourceColor (fg);

				gr.MoveTo (x, y + fe.Ascent);
				gr.ShowText (lstr);
				gr.Fill ();

				tok = tok.GetNextToken (false, true, true, true);

				/*if (buffer.SelectionInProgress && lineIndex >= buffer.SelectionStart.Y && lineIndex <= buffer.SelectionEnd.Y &&
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
					gr.Rectangle (rLineX, rLineY, rLineW, (fe.Ascent + fe.Descent));
					gr.SetSourceColor (selbg);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (selfg);
					gr.MoveTo (x, y + fe.Ascent);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}*/
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
			base.onDraw (gr);

			if (syntaxTree == null)
				return;

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Foreground.SetAsSource (gr);

			//buffer.editMutex.EnterReadLock ();
			editorMutex.EnterReadLock ();

			#region draw text cursor
			/*if (buffer.SelectionInProgress){
				selStartCol = getTabulatedColumn (buffer.SelectionStart);
				selEndCol = getTabulatedColumn (buffer.SelectionEnd);
			}else*/
			if (HasFocus && printedCurrentLine >= 0){
				/*gr.LineWidth = 1.0;
				double cursorX = cb.X + (getTabulatedColumn(buffer.CurrentPosition) - ScrollX) * fe.MaxXAdvance + leftMargin;
				gr.MoveTo (0.5 + cursorX, cb.Y + (printedCurrentLine) * (fe.Ascent+fe.Descent));
				gr.LineTo (0.5 + cursorX, cb.Y + (printedCurrentLine + 1) * (fe.Ascent+fe.Descent));
				gr.Stroke();*/
			}
			#endregion

			//if (PrintedLines?.Count > 0) {				

			/*currentNode = null;
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
			}*/

			SyntaxNodePrinter printer = new SyntaxNodePrinter (this, gr, syntaxTree.GetText ().Lines.Count, ScrollY, visibleLines);
			printer.Visit (syntaxTree.GetRoot ());
			printedLines = printer.printedLinesNumbers;
			//for (int i = 0; i < visibleLines; i++) {
			//	if (i + ScrollY >= buffer.Lines.Count)//TODO:need optimize
			//		break;
			//	drawLine (gr, cb, i);
			//}
			//}

			editorMutex.ExitReadLock ();

			//buffer.editMutex.ExitReadLock ();

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
				//NotifyValueChanged ("HoverError", buffer [hoverLine].exception);
			}
		}
		void updateHoverLine () {
			if (printedLines != null) { 
				int hvl = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent + fe.Descent)));
				hvl = Math.Min (printedLines.Length - 1, hvl);
				HoverLine = printedLines [hvl];
			} else
				HoverLine = 0;
		}
		void updateCurrentPosFromMouseLocalPos(){			
			PrintedCurrentLine = (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent)));
			int curVisualCol = ScrollX +  (int)Math.Round ((mouseLocalPos.X - leftMargin) / fe.MaxXAdvance);

			int i = 0;
			int buffCol = 0;
			/*while (i < curVisualCol && buffCol < buffer.CurrentCodeLine.Length) {
				if (buffer.CurrentCodeLine[buffCol] == '\t')
					i += Interface.TAB_SIZE;
				else
					i++;
				buffCol++;
			}
			buffer.CurrentColumn = buffCol;*/
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

			updateHoverLine ();

			if (!e.Mouse.IsButtonDown (MouseButton.Left)) {
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
			if (!this.Focusable)
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

			CurrentLine = HoverLine;
			updateCurrentPosFromMouseLocalPos ();
			//buffer.SetSelStartPos ();
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

			/*switch (key)
			{
			case Key.BackSpace:
				buffer.DeleteChar ();
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (buffer.SelectionIsEmpty)
					MoveRight ();
				else if (IFace.Shift)
					IFace.Clipboard = buffer.SelectedText;
				buffer.DeleteChar ();
				break;
			case Key.Return:
			case Key.KP_Enter:
				if (!buffer.SelectionIsEmpty)
					buffer.DeleteChar ();
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
					buffer.Insert (IFace.Clipboard);
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
						MoveLeft ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.GotoWordStart ();
				else
					MoveLeft();
				break;
			case Key.Right:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					if (IFace.Ctrl)
						buffer.GotoWordEnd ();
					else
						MoveRight ();
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				if (IFace.Ctrl)
					buffer.GotoWordEnd ();
				else
					MoveRight ();
				break;
			case Key.Up:
				if (IFace.Shift) {
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
				if (IFace.Shift) {
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
			case Key.Num_Lock:
				break;
			case Key.Page_Down:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine += visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine += visibleLines;
				break;
			case Key.Page_Up:
				if (IFace.Shift) {
					if (buffer.SelectionIsEmpty)
						buffer.SetSelStartPos ();
					PrintedCurrentLine -= visibleLines;
					buffer.SetSelEndPos ();
					break;
				}
				buffer.ResetSelection ();
				PrintedCurrentLine -= visibleLines;
				break;
			case Key.Tab:
				if (IFace.Shift) {
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
			}*/
			RegisterForGraphicUpdate ();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			//buffer.Insert (e.KeyChar.ToString());
			//buffer.ResetSelection ();
		}
		#endregion
	}
}